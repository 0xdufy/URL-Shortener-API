using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
using UrlShortener.Api.Configuration;
using UrlShortener.Api.Models;
using UrlShortener.Api.Security;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.Services;
using UrlShortener.Application.Validators;
using UrlShortener.Infrastructure.Caching;
using UrlShortener.Infrastructure.Configuration;
using UrlShortener.Infrastructure.Identity;
using UrlShortener.Infrastructure.Messaging;
using UrlShortener.Infrastructure.Persistence;
using UrlShortener.Infrastructure.Persistence.Repositories;
using UrlShortener.Infrastructure.RateLimiting;
using UrlShortener.Infrastructure.Services;

namespace UrlShortener.Api.Extensions;

public static class ServiceCollectionExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var storageSection = configuration.GetRequiredSection(StorageOptions.SectionName);
        var proxyTrustSection = configuration.GetRequiredSection(ProxyTrustOptions.SectionName);
        var persistenceSection = configuration.GetRequiredSection(PersistenceOptions.SectionName);
        var rateLimitingSection = configuration.GetRequiredSection(DistributedRateLimitingOptions.SectionName);
        var identitySection = configuration.GetRequiredSection(IdentitySecurityOptions.SectionName);
        var shortUrlLifecycleSection = configuration.GetRequiredSection(ShortUrlLifecycleOptions.SectionName);
        var publicUrlSection = configuration.GetRequiredSection(PublicUrlOptions.SectionName);
        var redisSection = configuration.GetRequiredSection(RedisOptions.SectionName);
        var idempotencySection = configuration.GetRequiredSection(IdempotencyOptions.SectionName);
        var requestLimitsSection = configuration.GetRequiredSection(RequestLimitsOptions.SectionName);
        var clickEventSection = configuration.GetRequiredSection(ClickEventPrivacyOptions.SectionName);

        services.AddRabbitMqTransport(configuration, environment);

        services.AddOptions<ClickEventPrivacyOptions>()
            .Bind(clickEventSection)
            .Validate(
                options => IsValidSecretKey(options.VisitorIdentityHmacKeyBase64),
                "ClickEvents:VisitorIdentityHmacKeyBase64 must contain at least 32 bytes encoded as Base64.")
            .ValidateOnStart();

        var storageOptions = storageSection.Get<StorageOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{StorageOptions.SectionName}' is invalid.");

        var proxyTrustOptions = proxyTrustSection.Get<ProxyTrustOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{ProxyTrustOptions.SectionName}' is invalid.");
        services.AddOptions<ProxyTrustOptions>()
            .Bind(proxyTrustSection)
            .Validate(
                options => options.ForwardLimit is >= 1 and <= 10,
                "ProxyTrust:ForwardLimit must be between 1 and 10.")
            .Validate(
                options => options.KnownProxies is not null && options.KnownNetworks is not null,
                "ProxyTrust proxy and network lists cannot be null.")
            .Validate(
                options => !options.Enabled ||
                    (options.KnownProxies?.Length ?? 0) + (options.KnownNetworks?.Length ?? 0) > 0,
                "ProxyTrust must include at least one known proxy or network when enabled.")
            .Validate(
                options => options.KnownProxies is not null && options.KnownProxies.All(IsValidKnownProxy),
                "Every ProxyTrust:KnownProxies value must be a specific IPv4 or IPv6 address.")
            .Validate(
                options => options.KnownNetworks is not null && options.KnownNetworks.All(IsValidKnownNetwork),
                "Every ProxyTrust:KnownNetworks value must be a bounded IPv4 or IPv6 CIDR network.")
            .ValidateOnStart();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = proxyTrustOptions.Enabled
                ? ForwardedHeaders.XForwardedFor
                : ForwardedHeaders.None;
            options.ForwardLimit = proxyTrustOptions.ForwardLimit;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();

            foreach (var value in proxyTrustOptions.KnownProxies)
            {
                var address = IPAddress.Parse(value);
                options.KnownProxies.Add(address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address);
            }

            foreach (var value in proxyTrustOptions.KnownNetworks)
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(value));
            }
        });

        services.AddOptions<StorageOptions>()
            .Bind(storageSection)
            .Validate(
                options => environment.IsDevelopment() || !options.UseInMemory,
                $"{StorageOptions.SectionName}:UseInMemory must be false outside the Development environment.")
            .ValidateOnStart();

        services.AddOptions<PersistenceOptions>()
            .Bind(persistenceSection)
            .Validate(options => options.CommandTimeoutSeconds is >= 1 and <= 300, "Persistence:CommandTimeoutSeconds must be between 1 and 300.")
            .ValidateOnStart();

        var idempotencyOptions = idempotencySection.Get<IdempotencyOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{IdempotencyOptions.SectionName}' is invalid.");
        services.AddOptions<IdempotencyOptions>()
            .Bind(idempotencySection)
            .Validate(
                options => options.RetentionHours is >= 1 and <= 168,
                "Idempotency:RetentionHours must be between 1 and 168.")
            .ValidateOnStart();

        services.AddOptions<RequestLimitsOptions>()
            .Bind(requestLimitsSection)
            .Validate(
                options => options.MaxRequestBodyBytes is >= 8_192 and <= 1_048_576,
                "RequestLimits:MaxRequestBodyBytes must be between 8192 and 1048576.")
            .Validate(
                options => options.MaxRequestLineBytes is >= 2_048 and <= 32_768,
                "RequestLimits:MaxRequestLineBytes must be between 2048 and 32768.")
            .Validate(
                options => options.MaxRequestHeadersTotalBytes is >= 8_192 and <= 65_536,
                "RequestLimits:MaxRequestHeadersTotalBytes must be between 8192 and 65536.")
            .Validate(
                options => options.MaxRequestHeaderCount is >= 16 and <= 128,
                "RequestLimits:MaxRequestHeaderCount must be between 16 and 128.")
            .Validate(
                options => options.RequestHeadersTimeoutSeconds is >= 5 and <= 60,
                "RequestLimits:RequestHeadersTimeoutSeconds must be between 5 and 60.")
            .Validate(
                options => options.RequestTimeoutSeconds is >= 5 and <= 120,
                "RequestLimits:RequestTimeoutSeconds must be between 5 and 120.")
            .ValidateOnStart();

        services.AddOptions<DistributedRateLimitingOptions>()
            .Bind(rateLimitingSection)
            .Validate(
                options => options.GetPolicies().All(policy => IsValidRateLimitPolicy(policy.Options)),
                "Every RateLimiting policy must use a supported algorithm and safe permit/window/refill bounds.")
            .ValidateOnStart();

        var redisOptions = redisSection.Get<RedisOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{RedisOptions.SectionName}' is invalid.");
        services.AddOptions<RedisOptions>()
            .Bind(redisSection)
            .Validate(
                options => IsValidRedisConnectionString(options.ConnectionString),
                "Redis:ConnectionString must contain a valid StackExchange.Redis endpoint configuration.")
            .Validate(
                options => IsValidRedisKeyPrefix(options.KeyPrefix),
                "Redis:KeyPrefix must use the lowercase 'application:environment:vN:' format.")
            .Validate(
                options => options.ConnectTimeoutMilliseconds is >= 100 and <= 10_000,
                "Redis:ConnectTimeoutMilliseconds must be between 100 and 10000.")
            .Validate(
                options => options.OperationTimeoutMilliseconds is >= 50 and <= 5_000,
                "Redis:OperationTimeoutMilliseconds must be between 50 and 5000.")
            .Validate(
                options => options.ConnectRetryCount is >= 0 and <= 5,
                "Redis:ConnectRetryCount must be between 0 and 5.")
            .Validate(
                options => options.ReconnectBaseDelayMilliseconds is >= 100 and <= 60_000,
                "Redis:ReconnectBaseDelayMilliseconds must be between 100 and 60000.")
            .Validate(
                options => options.ReconnectMaxDelayMilliseconds >= options.ReconnectBaseDelayMilliseconds &&
                    options.ReconnectMaxDelayMilliseconds <= 300_000,
                "Redis:ReconnectMaxDelayMilliseconds must be at least ReconnectBaseDelayMilliseconds and no more than 300000.")
            .ValidateOnStart();

        services.AddRedisInfrastructure(redisOptions);

        services.AddOptions<IdentitySecurityOptions>()
            .Bind(identitySection)
            .Validate(options => !string.IsNullOrWhiteSpace(options.JwtIssuer), "Identity:JwtIssuer is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.JwtAudience), "Identity:JwtAudience is required.")
            .Validate(options => options.JwtClockSkewSeconds is >= 0 and <= 120, "Identity:JwtClockSkewSeconds must be between 0 and 120.")
            .Validate(
                options => options.AllowedOrigins.Length > 0 && options.AllowedOrigins.All(IsValidOrigin),
                "Identity:AllowedOrigins must contain explicit absolute HTTP or HTTPS origins without paths.")
            .Validate(
                options => environment.IsDevelopment() || options.RequireSecureCookies,
                "Identity:RequireSecureCookies must be true outside Development.")
            .Validate(
                options => storageOptions.UseInMemory || IsValidSigningKey(options.JwtSigningKeyBase64),
                "Identity:JwtSigningKeyBase64 must be valid base64 containing at least 32 random bytes when SQL storage is enabled.")
            .Validate(options => options.PasswordRequiredLength is >= 12 and <= 128, "Identity:PasswordRequiredLength must be between 12 and 128.")
            .Validate(options => options.PasswordRequiredUniqueChars is >= 1 and <= 16, "Identity:PasswordRequiredUniqueChars must be between 1 and 16.")
            .Validate(options => options.MaxFailedAccessAttempts is >= 3 and <= 20, "Identity:MaxFailedAccessAttempts must be between 3 and 20.")
            .Validate(options => options.LockoutMinutes is >= 5 and <= 1440, "Identity:LockoutMinutes must be between 5 and 1440.")
            .Validate(options => options.AccessTokenLifetimeMinutes is >= 5 and <= 30, "Identity:AccessTokenLifetimeMinutes must be between 5 and 30.")
            .Validate(options => options.RefreshTokenLifetimeDays is >= 1 and <= 60, "Identity:RefreshTokenLifetimeDays must be between 1 and 60.")
            .Validate(
                options => options.RefreshTokenAbsoluteLifetimeDays >= options.RefreshTokenLifetimeDays &&
                    options.RefreshTokenAbsoluteLifetimeDays <= 180,
                "Identity:RefreshTokenAbsoluteLifetimeDays must be between RefreshTokenLifetimeDays and 180.")
            .ValidateOnStart();

        var shortUrlLifecycleOptions = shortUrlLifecycleSection.Get<ShortUrlLifecycleOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{ShortUrlLifecycleOptions.SectionName}' is invalid.");
        services.AddOptions<ShortUrlLifecycleOptions>()
            .Bind(shortUrlLifecycleSection)
            .Validate(
                options => options.SoftDeleteRetentionDays is >= 1 and <= 3650,
                "ShortUrlLifecycle:SoftDeleteRetentionDays must be between 1 and 3650.")
            .ValidateOnStart();

        var publicUrlOptions = publicUrlSection.Get<PublicUrlOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{PublicUrlOptions.SectionName}' is invalid.");
        services.AddOptions<PublicUrlOptions>()
            .Bind(publicUrlSection)
            .Validate(options => IsValidPublicBaseUrl(options.BaseUrl),
                "PublicUrls:BaseUrl must be an absolute HTTP or HTTPS origin without a path, query, fragment, or trailing slash.")
            .ValidateOnStart();

        var identityOptions = identitySection.Get<IdentitySecurityOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{IdentitySecurityOptions.SectionName}' is invalid.");

        if (!storageOptions.UseInMemory)
        {
            var connectionString = configuration.GetConnectionString("SqlServer");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "SQL Server storage is enabled, but ConnectionStrings:SqlServer is missing. " +
                    "Provide it through environment-specific configuration or the ConnectionStrings__SqlServer environment variable.");
            }

            services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            {
                var persistenceOptions = serviceProvider
                    .GetRequiredService<IOptions<PersistenceOptions>>()
                    .Value;

                options.UseSqlServer(
                    connectionString,
                    sqlOptions =>
                    {
                        sqlOptions.CommandTimeout(persistenceOptions.CommandTimeoutSeconds);
                    });
            });

            services.AddIdentityCore<ApplicationUser>(options =>
                {
                    options.User.RequireUniqueEmail = true;
                    options.Password.RequiredLength = identityOptions.PasswordRequiredLength;
                    options.Password.RequiredUniqueChars = identityOptions.PasswordRequiredUniqueChars;
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Lockout.AllowedForNewUsers = true;
                    options.Lockout.MaxFailedAccessAttempts = identityOptions.MaxFailedAccessAttempts;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(identityOptions.LockoutMinutes);
                    options.SignIn.RequireConfirmedEmail = false;
                })
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<AppDbContext>();

            services.AddScoped<IAccessTokenIssuer, JwtAccessTokenIssuer>();
            services.AddScoped<IAuthenticationService, IdentityAuthenticationService>();
        }
        else
        {
            services.AddSingleton<IAuthenticationService, UnavailableAuthenticationService>();
        }

        var signingKey = ResolveSigningKey(storageOptions, identityOptions);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = identityOptions.JwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = identityOptions.JwtAudience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(signingKey),
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ClockSkew = TimeSpan.FromSeconds(identityOptions.JwtClockSkewSeconds),
                    NameClaimType = JwtRegisteredClaimNames.Sub
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var userId = context.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                        var sessionId = context.Principal?.FindFirst(JwtAccessTokenIssuer.SessionIdClaim)?.Value;
                        if (!Guid.TryParse(userId, out _) || !Guid.TryParse(sessionId, out _))
                        {
                            context.Fail("Required authentication claims are missing.");
                        }

                        return Task.CompletedTask;
                    },
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        await WriteAuthenticationErrorAsync(
                            context.HttpContext,
                            StatusCodes.Status401Unauthorized,
                            "AUTHENTICATION_REQUIRED",
                            "A valid access token is required.");
                    },
                    OnForbidden = context => WriteAuthenticationErrorAsync(
                        context.HttpContext,
                        StatusCodes.Status403Forbidden,
                        "FORBIDDEN",
                        "The authenticated identity is not permitted to perform this operation.")
                };
            });
        services.AddAuthorization();

        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-XSRF-TOKEN";
            options.Cookie.Name = "urlshortener.csrf";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = identityOptions.RequireSecureCookies
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.None;
        });

        services.AddCors(options =>
        {
            options.AddPolicy("TrustedWebClient", policy =>
            {
                policy
                    .WithOrigins(identityOptions.AllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        services.AddMemoryCache();
        services.AddHttpContextAccessor();

        var mvcBuilder = services.AddControllers();
        mvcBuilder.ConfigureApiBehaviorOptions(options =>
        {
            options.SuppressMapClientErrors = true;
            options.InvalidModelStateResponseFactory = context =>
            {
                var failures = context.ModelState
                    .Where(entry => entry.Value?.Errors.Count > 0)
                    .SelectMany(entry => entry.Value!.Errors.Select(error =>
                        new FluentValidation.Results.ValidationFailure(
                            string.IsNullOrWhiteSpace(entry.Key) ? "request" : entry.Key,
                            string.IsNullOrWhiteSpace(error.ErrorMessage)
                                ? "The request value is invalid."
                                : error.ErrorMessage)));

                return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(
                    ApiErrorFactory.Validation(context.HttpContext, failures));
            };
        });

        services.AddValidatorsFromAssemblyContaining<CreateShortUrlRequestValidator>();

        services.AddScoped<IShortUrlService, ShortUrlService>();
        services.AddScoped<IRedirectResolver, RedirectResolver>();
        services.AddSingleton<IRedirectClickEventPublisher, PrivacyAwareRedirectClickEventPublisher>();
        services.AddSingleton(new ShortUrlLifecycleSettings(shortUrlLifecycleOptions.SoftDeleteRetentionDays));
        services.AddSingleton(new ShortUrlContractSettings(publicUrlOptions.BaseUrl));
        services.AddSingleton(new IdempotencySettings(idempotencyOptions.RetentionHours));
        services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
        if (storageOptions.UseInMemory)
        {
            services.AddSingleton<IShortUrlRepository, InMemoryShortUrlRepository>();
        }
        else
        {
            services.AddScoped<IShortUrlRepository, ShortUrlRepository>();
        }
        services.AddSingleton<IShortCodeGenerator, ShortCodeGenerator>();
        services.AddSingleton<IShortUrlCache, ShortUrlCache>();
        services.AddSingleton<IDistributedRateLimiter, RedisDistributedRateLimiter>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        return services;
    }

    private static bool IsValidSigningKey(string value)
    {
        return IsValidSecretKey(value);
    }

    private static bool IsValidSecretKey(string value)
    {
        try
        {
            return Convert.FromBase64String(value).Length >= 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool IsValidKnownProxy(string value)
    {
        if (!IPAddress.TryParse(value, out var address))
        {
            return false;
        }

        var normalized = address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
        return !normalized.Equals(IPAddress.Any) && !normalized.Equals(IPAddress.IPv6Any);
    }

    private static bool IsValidKnownNetwork(string value)
    {
        if (!System.Net.IPNetwork.TryParse(value, out var network) || network.PrefixLength == 0)
        {
            return false;
        }

        return !network.BaseAddress.IsIPv4MappedToIPv6;
    }

    private static bool IsValidRateLimitPolicy(RateLimitPolicyOptions options)
    {
        if (!Enum.IsDefined(options.Algorithm) || options.PermitLimit is < 1 or > 100_000)
        {
            return false;
        }

        return options.Algorithm switch
        {
            RateLimitAlgorithm.FixedWindow or RateLimitAlgorithm.SlidingWindow =>
                options.WindowSeconds is >= 1 and <= 86_400,
            RateLimitAlgorithm.TokenBucket =>
                options.TokensPerPeriod is >= 1 and <= 100_000 &&
                options.TokensPerPeriod <= options.PermitLimit &&
                options.ReplenishmentPeriodSeconds is >= 1 and <= 86_400 &&
                HasSafeTokenBucketRetention(options),
            _ => false
        };
    }

    private static bool HasSafeTokenBucketRetention(RateLimitPolicyOptions options)
    {
        const int maximumFullRefillSeconds = 7 * 24 * 60 * 60;
        return (long)options.PermitLimit * options.ReplenishmentPeriodSeconds <=
            (long)options.TokensPerPeriod * maximumFullRefillSeconds;
    }

    private static bool IsValidRedisConnectionString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            return ConfigurationOptions.Parse(value, ignoreUnknown: false).EndPoints.Count > 0;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool IsValidRedisKeyPrefix(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 100 || !value.EndsWith(':'))
        {
            return false;
        }

        var segments = value[..^1].Split(':');
        return segments.Length == 3 &&
            segments[0].Length > 0 &&
            segments[1].Length > 0 &&
            segments[2].Length > 1 &&
            segments[2][0] == 'v' &&
            int.TryParse(segments[2][1..], out var version) &&
            version > 0 &&
            IsLowercaseSlug(segments[0]) &&
            IsLowercaseSlug(segments[1]);
    }

    private static bool IsLowercaseSlug(string value) =>
        value[0] != '-' &&
        value[^1] != '-' &&
        value.All(character =>
            character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');

    private static byte[] ResolveSigningKey(
        StorageOptions storageOptions,
        IdentitySecurityOptions identityOptions)
    {
        if (storageOptions.UseInMemory)
        {
            return RandomNumberGenerator.GetBytes(32);
        }

        if (!IsValidSigningKey(identityOptions.JwtSigningKeyBase64))
        {
            throw new InvalidOperationException(
                "SQL-backed authentication requires Identity:JwtSigningKeyBase64 containing at least 32 random bytes. " +
                "Provide it through a secret source or the Identity__JwtSigningKeyBase64 environment variable.");
        }

        return Convert.FromBase64String(identityOptions.JwtSigningKeyBase64);
    }

    private static bool IsValidOrigin(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
        string.IsNullOrEmpty(uri.Query) &&
        string.IsNullOrEmpty(uri.Fragment) &&
        uri.AbsolutePath == "/" &&
        !value.EndsWith("/", StringComparison.Ordinal);

    private static bool IsValidPublicBaseUrl(string value) =>
        IsValidOrigin(value);

    private static async Task WriteAuthenticationErrorAsync(
        HttpContext context,
        int statusCode,
        string code,
        string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var response = ApiErrorFactory.Create(context, code, message);
        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            response,
            JsonOptions,
            context.RequestAborted);
    }

    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        var configuredLevel = builder.Configuration["Serilog:MinimumLevel"];
        var parsed = Enum.TryParse<LogEventLevel>(configuredLevel, true, out var level);
        var minimumLevel = parsed ? level : LogEventLevel.Information;

        builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .MinimumLevel.Is(minimumLevel)
                .WriteTo.Console()
                .WriteTo.File("logs/url-shortener-.log", rollingInterval: RollingInterval.Day);
        });

        return builder;
    }
}
