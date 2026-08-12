using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using UrlShortener.Application.Exceptions;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.RateLimiting;
using UrlShortener.Infrastructure.Caching;
using UrlShortener.Infrastructure.Configuration;

namespace UrlShortener.Infrastructure.RateLimiting;

public sealed class RedisDistributedRateLimiter : IDistributedRateLimiter
{
    private const string FixedWindowScript = """
        local nowParts = redis.call('TIME')
        local nowMilliseconds = (tonumber(nowParts[1]) * 1000) + math.floor(tonumber(nowParts[2]) / 1000)
        local permitLimit = tonumber(ARGV[1])
        local windowMilliseconds = tonumber(ARGV[2])
        local windowStart = nowMilliseconds - (nowMilliseconds % windowMilliseconds)
        local storedStart = tonumber(redis.call('HGET', KEYS[1], 'start'))
        local count = 0

        if storedStart == windowStart then
            count = tonumber(redis.call('HGET', KEYS[1], 'count')) or 0
        end

        local remainingMilliseconds = windowMilliseconds - (nowMilliseconds - windowStart)

        if count >= permitLimit then
            redis.call('PEXPIRE', KEYS[1], remainingMilliseconds + windowMilliseconds)
            return { 0, 0, math.max(1, math.ceil(remainingMilliseconds / 1000)) }
        end

        count = count + 1
        redis.call('HSET', KEYS[1], 'start', windowStart, 'count', count)
        redis.call('PEXPIRE', KEYS[1], remainingMilliseconds + windowMilliseconds)
        return { 1, permitLimit - count, 0 }
        """;

    private const string SlidingWindowScript = """
        local nowParts = redis.call('TIME')
        local nowMilliseconds = (tonumber(nowParts[1]) * 1000) + math.floor(tonumber(nowParts[2]) / 1000)
        local permitLimit = tonumber(ARGV[1])
        local windowMilliseconds = tonumber(ARGV[2])
        local cutoff = nowMilliseconds - windowMilliseconds

        redis.call('ZREMRANGEBYSCORE', KEYS[1], '-inf', cutoff)
        local count = tonumber(redis.call('ZCARD', KEYS[1]))

        if count >= permitLimit then
            local oldest = redis.call('ZRANGE', KEYS[1], 0, 0, 'WITHSCORES')
            local retryMilliseconds = windowMilliseconds
            if oldest[2] then
                retryMilliseconds = math.max(1, tonumber(oldest[2]) + windowMilliseconds - nowMilliseconds)
            end

            redis.call('PEXPIRE', KEYS[1], windowMilliseconds)
            return { 0, 0, math.max(1, math.ceil(retryMilliseconds / 1000)) }
        end

        local member = tostring(nowMilliseconds) .. '-' .. ARGV[3]
        redis.call('ZADD', KEYS[1], nowMilliseconds, member)
        redis.call('PEXPIRE', KEYS[1], windowMilliseconds)
        return { 1, permitLimit - count - 1, 0 }
        """;

    private const string TokenBucketScript = """
        local nowParts = redis.call('TIME')
        local nowMilliseconds = (tonumber(nowParts[1]) * 1000) + math.floor(tonumber(nowParts[2]) / 1000)
        local capacity = tonumber(ARGV[1])
        local tokensPerPeriod = tonumber(ARGV[2])
        local periodMilliseconds = tonumber(ARGV[3])
        local tokens = tonumber(redis.call('HGET', KEYS[1], 'tokens'))
        local lastRefill = tonumber(redis.call('HGET', KEYS[1], 'lastRefill'))

        if not tokens or not lastRefill then
            tokens = capacity
            lastRefill = nowMilliseconds
        else
            local elapsed = math.max(0, nowMilliseconds - lastRefill)
            tokens = math.min(capacity, tokens + ((elapsed * tokensPerPeriod) / periodMilliseconds))
            lastRefill = nowMilliseconds
        end

        local allowed = 0
        local retrySeconds = 0
        if tokens >= 1 then
            allowed = 1
            tokens = tokens - 1
        else
            local missingTokens = 1 - tokens
            retrySeconds = math.max(1, math.ceil((missingTokens * periodMilliseconds / tokensPerPeriod) / 1000))
        end

        redis.call('HSET', KEYS[1], 'tokens', tostring(tokens), 'lastRefill', lastRefill)
        local fullyRefilledMilliseconds = math.ceil((capacity * periodMilliseconds) / tokensPerPeriod)
        redis.call('PEXPIRE', KEYS[1], fullyRefilledMilliseconds + periodMilliseconds)
        return { allowed, math.floor(tokens), retrySeconds }
        """;

    private readonly RedisConnectionProvider _connectionProvider;
    private readonly DistributedRateLimitingOptions _options;
    private readonly string _keyPrefix;
    private readonly ILogger<RedisDistributedRateLimiter> _logger;

    public RedisDistributedRateLimiter(
        RedisConnectionProvider connectionProvider,
        IOptions<DistributedRateLimitingOptions> options,
        IOptions<RedisOptions> redisOptions,
        ILogger<RedisDistributedRateLimiter> logger)
    {
        _connectionProvider = connectionProvider;
        _options = options.Value;
        _keyPrefix = redisOptions.Value.KeyPrefix;
        _logger = logger;
    }

    public async Task<RateLimitDecision> CheckAsync(
        RateLimitPolicy policy,
        string partitionKey,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var settings = GetSettings(policy);
            var connection = await _connectionProvider
                .GetConnectionAsync()
                .WaitAsync(cancellationToken);
            var database = connection.GetDatabase();
            var key = CreateKey(policy, partitionKey);
            var result = settings.Algorithm switch
            {
                RateLimitAlgorithm.FixedWindow => await EvaluateAsync(
                    database,
                    FixedWindowScript,
                    key,
                    [settings.PermitLimit, ToMilliseconds(settings.WindowSeconds)],
                    cancellationToken),
                RateLimitAlgorithm.SlidingWindow => await EvaluateAsync(
                    database,
                    SlidingWindowScript,
                    key,
                    [settings.PermitLimit, ToMilliseconds(settings.WindowSeconds), Guid.NewGuid().ToString("N")],
                    cancellationToken),
                RateLimitAlgorithm.TokenBucket => await EvaluateAsync(
                    database,
                    TokenBucketScript,
                    key,
                    [settings.PermitLimit, settings.TokensPerPeriod, ToMilliseconds(settings.ReplenishmentPeriodSeconds)],
                    cancellationToken),
                _ => throw new InvalidOperationException($"Unsupported rate-limit algorithm '{settings.Algorithm}'.")
            };

            return ParseDecision(result);
        }
        catch (RedisException exception)
        {
            _logger.LogWarning(
                exception,
                "Redis rate-limit evaluation failed for policy {RateLimitPolicy}.",
                policy);
            throw new RateLimitingUnavailableException(exception);
        }
    }

    private static async Task<RedisResult> EvaluateAsync(
        IDatabase database,
        string script,
        RedisKey key,
        RedisValue[] values,
        CancellationToken cancellationToken)
    {
        return await database
            .ScriptEvaluateAsync(script, [key], values)
            .WaitAsync(cancellationToken);
    }

    private RateLimitPolicyOptions GetSettings(RateLimitPolicy policy) => policy switch
    {
        RateLimitPolicy.Anonymous => _options.Anonymous,
        RateLimitPolicy.AuthenticationRegistration => _options.AuthenticationRegistration,
        RateLimitPolicy.AuthenticationSignIn => _options.AuthenticationSignIn,
        RateLimitPolicy.AuthenticationSession => _options.AuthenticationSession,
        RateLimitPolicy.Authenticated => _options.Authenticated,
        RateLimitPolicy.UrlCreation => _options.UrlCreation,
        RateLimitPolicy.ApiKey => _options.ApiKey,
        _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, null)
    };

    private string CreateKey(RateLimitPolicy policy, string partitionKey)
    {
        var partitionBytes = Encoding.UTF8.GetBytes(partitionKey);
        var partitionHash = Convert.ToHexStringLower(SHA256.HashData(partitionBytes));
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{_keyPrefix}ratelimit:v1:{GetPolicyName(policy)}:{partitionHash}");
    }

    private static string GetPolicyName(RateLimitPolicy policy) => policy switch
    {
        RateLimitPolicy.Anonymous => "anonymous",
        RateLimitPolicy.AuthenticationRegistration => "auth-register",
        RateLimitPolicy.AuthenticationSignIn => "auth-sign-in",
        RateLimitPolicy.AuthenticationSession => "auth-session",
        RateLimitPolicy.Authenticated => "authenticated",
        RateLimitPolicy.UrlCreation => "url-create",
        RateLimitPolicy.ApiKey => "api-key",
        _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, null)
    };

    private static RateLimitDecision ParseDecision(RedisResult result)
    {
        var values = (RedisResult[]?)result
            ?? throw new RedisServerException("The rate-limit script returned an invalid response.");
        if (values.Length != 3)
        {
            throw new RedisServerException("The rate-limit script returned an invalid response.");
        }

        return new RateLimitDecision(
            ParseInt64(values[0]) == 1,
            checked((int)ParseInt64(values[1])),
            checked((int)ParseInt64(values[2])));
    }

    private static long ParseInt64(RedisResult value) =>
        long.Parse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture);

    private static long ToMilliseconds(int seconds) => checked(seconds * 1000L);
}
