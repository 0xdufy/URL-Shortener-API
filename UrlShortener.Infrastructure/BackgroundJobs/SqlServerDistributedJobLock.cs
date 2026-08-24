using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UrlShortener.Application.BackgroundJobs;
using UrlShortener.Infrastructure.Persistence;

namespace UrlShortener.Infrastructure.BackgroundJobs;

public sealed class SqlServerDistributedJobLock : IDistributedJobLock
{
    private const string LockResourcePrefix = "url-shortener:maintenance:";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILoggerFactory _loggerFactory;

    public SqlServerDistributedJobLock(
        IServiceScopeFactory scopeFactory,
        ILoggerFactory loggerFactory)
    {
        _scopeFactory = scopeFactory;
        _loggerFactory = loggerFactory;
    }

    public async Task<IDistributedJobLease?> TryAcquireAsync(
        string resourceName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        var scope = _scopeFactory.CreateAsyncScope();

        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var connection = dbContext.Database.GetDbConnection();
            await connection.OpenAsync(cancellationToken);

            var lockResource = $"{LockResourcePrefix}{resourceName}";
            var result = await ExecuteLockCommandAsync(
                connection,
                lockResource,
                acquire: true,
                cancellationToken);

            if (result < 0)
            {
                await scope.DisposeAsync();
                return null;
            }

            return new SqlServerDistributedJobLease(
                scope,
                connection,
                lockResource,
                _loggerFactory.CreateLogger<SqlServerDistributedJobLease>());
        }
        catch
        {
            await scope.DisposeAsync();
            throw;
        }
    }

    private static async Task<int> ExecuteLockCommandAsync(
        DbConnection connection,
        string resourceName,
        bool acquire,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = acquire
            ? "DECLARE @result int; EXEC @result = sys.sp_getapplock " +
                "@Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Session', " +
                "@LockTimeout = 0; SELECT @result;"
            : "DECLARE @result int; EXEC @result = sys.sp_releaseapplock " +
                "@Resource = @resource, @LockOwner = 'Session'; SELECT @result;";
        command.CommandType = CommandType.Text;

        var resourceParameter = command.CreateParameter();
        resourceParameter.ParameterName = "@resource";
        resourceParameter.DbType = DbType.String;
        resourceParameter.Size = 255;
        resourceParameter.Value = resourceName;
        command.Parameters.Add(resourceParameter);

        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(scalar, System.Globalization.CultureInfo.InvariantCulture);
    }

    private sealed class SqlServerDistributedJobLease : IDistributedJobLease
    {
        private readonly AsyncServiceScope _scope;
        private readonly DbConnection _connection;
        private readonly string _resourceName;
        private readonly ILogger _logger;
        private bool _disposed;

        public SqlServerDistributedJobLease(
            AsyncServiceScope scope,
            DbConnection connection,
            string resourceName,
            ILogger logger)
        {
            _scope = scope;
            _connection = connection;
            _resourceName = resourceName;
            _logger = logger;
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                if (_connection.State == ConnectionState.Open)
                {
                    await ExecuteLockCommandAsync(
                        _connection,
                        _resourceName,
                        acquire: false,
                        CancellationToken.None);
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "The SQL Server maintenance-job lock {LockResource} could not be explicitly released; closing its session will release it.",
                    _resourceName);
            }
            finally
            {
                await _scope.DisposeAsync();
            }
        }
    }
}
