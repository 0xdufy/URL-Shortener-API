using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using UrlShortener.Infrastructure.Configuration;

namespace UrlShortener.Infrastructure.Caching;

public static class RedisServiceCollectionExtensions
{
    public static IServiceCollection AddRedisInfrastructure(
        this IServiceCollection services,
        RedisOptions redisOptions)
    {
        ArgumentNullException.ThrowIfNull(redisOptions);

        var configuration = ConfigurationOptions.Parse(
            redisOptions.ConnectionString,
            ignoreUnknown: false);

        configuration.AbortOnConnectFail = false;
        configuration.AllowAdmin = false;
        configuration.ConnectRetry = redisOptions.ConnectRetryCount;
        configuration.ConnectTimeout = redisOptions.ConnectTimeoutMilliseconds;
        configuration.SyncTimeout = redisOptions.OperationTimeoutMilliseconds;
        configuration.AsyncTimeout = redisOptions.OperationTimeoutMilliseconds;
        configuration.BacklogPolicy = BacklogPolicy.FailFast;
        configuration.ReconnectRetryPolicy = new ExponentialRetry(
            redisOptions.ReconnectBaseDelayMilliseconds,
            redisOptions.ReconnectMaxDelayMilliseconds);

        services.AddSingleton(_ => new RedisConnectionProvider(configuration));

        services.AddStackExchangeRedisCache(options =>
        {
            options.InstanceName = redisOptions.KeyPrefix;
        });
        services.AddOptions<RedisCacheOptions>()
            .Configure<RedisConnectionProvider>((options, connectionProvider) =>
            {
                options.ConnectionMultiplexerFactory = connectionProvider.GetConnectionAsync;
            });

        return services;
    }
}
