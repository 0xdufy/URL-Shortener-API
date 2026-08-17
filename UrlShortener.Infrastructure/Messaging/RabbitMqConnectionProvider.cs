using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using UrlShortener.Infrastructure.Configuration;

namespace UrlShortener.Infrastructure.Messaging;

public sealed class RabbitMqConnectionProvider : IAsyncDisposable
{
    private readonly ConnectionFactory _connectionFactory;
    private readonly TimeSpan _connectionTimeout;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;
    private bool _disposed;

    public RabbitMqConnectionProvider(IOptions<RabbitMqOptions> options)
    {
        var settings = options.Value;
        _connectionTimeout = TimeSpan.FromMilliseconds(settings.ConnectionTimeoutMilliseconds);
        _connectionFactory = new ConnectionFactory
        {
            HostName = settings.HostName,
            Port = settings.Port,
            VirtualHost = settings.VirtualHost,
            UserName = settings.UserName,
            Password = settings.Password,
            ClientProvidedName = settings.ClientProvidedName,
            RequestedConnectionTimeout = _connectionTimeout,
            ContinuationTimeout = TimeSpan.FromMilliseconds(settings.OperationTimeoutMilliseconds),
            RequestedHeartbeat = TimeSpan.FromSeconds(settings.RequestedHeartbeatSeconds),
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromMilliseconds(settings.NetworkRecoveryIntervalMilliseconds),
            ConsumerDispatchConcurrency = 1,
            Ssl = new SslOption
            {
                Enabled = settings.UseTls,
                ServerName = settings.TlsServerName
            }
        };
    }

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }

            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(_connectionTimeout);
            _connection = await _connectionFactory.CreateConnectionAsync(timeoutSource.Token);
            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _connectionLock.WaitAsync();
        try
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }
        }
        finally
        {
            _connectionLock.Release();
            _connectionLock.Dispose();
        }
    }
}
