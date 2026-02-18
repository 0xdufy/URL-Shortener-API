using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.Mappings;
using UrlShortener.Application.Services;
using UrlShortener.Application.Validators;
using UrlShortener.Infrastructure.Caching;
using UrlShortener.Infrastructure.Persistence;
using UrlShortener.Infrastructure.Persistence.Repositories;
using UrlShortener.Infrastructure.RateLimiting;
using UrlShortener.Infrastructure.Services;

namespace UrlShortener.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("SqlServer")));

        services.AddMemoryCache();

        var mvcBuilder = services.AddControllers();
        mvcBuilder.ConfigureApiBehaviorOptions(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<CreateShortUrlRequestValidator>();

        services.AddAutoMapper(typeof(MappingProfile).Assembly);

        services.AddScoped<IShortUrlService, ShortUrlService>();
        services.AddScoped<IShortUrlRepository, ShortUrlRepository>();
        services.AddSingleton<IShortCodeGenerator, ShortCodeGenerator>();
        services.AddSingleton<IShortUrlCache, ShortUrlCache>();
        services.AddSingleton<IRateLimiter, InMemoryRateLimiter>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        return services;
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
