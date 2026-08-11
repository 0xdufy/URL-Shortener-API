using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using UrlShortener.Api.Models;
using UrlShortener.Api.Security;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.Services;
using UrlShortener.Application.Validators;
using UrlShortener.Infrastructure.Caching;
using UrlShortener.Infrastructure.Configuration;
using UrlShortener.Infrastructure.Identity;
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
        var persistenceSection = configuration.GetRequiredSection(PersistenceOptions.SectionName);
        var rateLimitingSection = configuration.GetRequiredSection(RateLimitingOptions.SectionName);
        var identitySection = configuration.GetRequiredSection(IdentitySecurityOptions.SectionName);
        var authenticationRateLimitingSection = configuration.GetRequiredSection(AuthenticationRateLimitingOptions.SectionName);
        var shortUrlLifecycleSection = configuration.GetRequiredSection(ShortUrlLifecycleOptions.SectionName);
        var publicUrlSection = configuration.GetRequiredSection(PublicUrlOptions.SectionName);

        var storageOptions = storageSection.Get<StorageOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{StorageOptions.SectionName}' is invalid.");
        services.AddOptions<StorageOptions>()
            .Bind(storageSection)
            .Validate(
                options => environment.IsDevelopment() || !options.UseInMemory,
                $"{StorageOptions.SectionName}:UseInMemory must be false outside the Development environment.")
            .ValidateOnStart();

        services.AddOptions<PersistenceOptions>()
            .Bind(persistenceSection)
            .Validate(options => options.MaxRetryCount is >= 0 and <= 10, "Persistence:MaxRetryCount must be between 0 and 10.")
            .Validate(options => options.MaxRetryDelaySeconds is >= 1 and <= 60, "Persistence:MaxRetryDelaySeconds must be between 1 and 60.")
            .Validate(options => options.CommandTimeoutSeconds is >= 1 and <= 300, "Persistence:CommandTimeoutSeconds must be between 1 and 300.")
            .ValidateOnStart();

        services.AddOptions<RateLimitingOptions>()
            .Bind(rateLimitingSection)
            .Validate(options => options.CreatePerMinuteLimit is >= 1 and <= 10_000, "RateLimiting:CreatePerMinuteLimit must be between 1 and 10000.")
            .ValidateOnStart();

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

        services.AddOptions<AuthenticationRateLimitingOptions>()
            .Bind(authenticationRateLimitingSection)
            .Validate(options => options.RegistrationPerMinuteLimit is >= 1 and <= 10_000, "AuthenticationRateLimiting:RegistrationPerMinuteLimit must be between 1 and 10000.")
            .Validate(options => options.SignInPerMinuteLimit is >= 1 and <= 10_000, "AuthenticationRateLimiting:SignInPerMinuteLimit must be between 1 and 10000.")
            .Validate(options => options.RefreshPerMinuteLimit is >= 1 and <= 10_000, "AuthenticationRateLimiting:RefreshPerMinuteLimit must be between 1 and 10000.")
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
                        sqlOptions.EnableRetryOnFailure(
                            persistenceOptions.MaxRetryCount,
                            TimeSpan.FromSeconds(persistenceOptions.MaxRetryDelaySeconds),
                            errorNumbersToAdd: null);
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
        services.AddSingleton(new ShortUrlLifecycleSettings(shortUrlLifecycleOptions.SoftDeleteRetentionDays));
        services.AddSingleton(new ShortUrlContractSettings(publicUrlOptions.BaseUrl));
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
        services.AddSingleton<IRateLimiter, InMemoryRateLimiter>();
        services.AddSingleton<IAuthenticationRateLimiter, InMemoryAuthenticationRateLimiter>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        return services;
    }

    private static bool IsValidSigningKey(string value)
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
        await JsonSerializer.SerializeAsync(context.Response.Body, response, JsonOptions);
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
