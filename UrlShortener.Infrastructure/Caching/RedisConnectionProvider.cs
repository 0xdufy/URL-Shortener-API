using StackExchange.Redis;

namespace UrlShortener.Infrastructure.Caching;

public sealed class RedisConnectionProvider : IAsyncDisposable
{
    private readonly Lazy<Task<IConnectionMultiplexer>> _connection;

    public RedisConnectionProvider(ConfigurationOptions configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _connection = new Lazy<Task<IConnectionMultiplexer>>(
            async () => await ConnectionMultiplexer.ConnectAsync(configuration).ConfigureAwait(false),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public Task<IConnectionMultiplexer> GetConnectionAsync() => _connection.Value;

    public async ValueTask DisposeAsync()
    {
        if (!_connection.IsValueCreated)
        {
            return;
        }

        IConnectionMultiplexer connection;
        try
        {
            connection = await _connection.Value.ConfigureAwait(false);
        }
        catch (RedisException)
        {
            return;
        }

        try
        {
            await connection.CloseAsync().ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            // The framework Redis cache can release the same shared connection first at shutdown.
        }

        connection.Dispose();
    }
}
