# Redis Infrastructure

## Provider and Lifecycle

The API registers `Microsoft.Extensions.Caching.StackExchangeRedis` 10.0.10 as the
`IDistributedCache` implementation. The framework provider is registered once in the
dependency-injection container and owns one lazily created StackExchange.Redis connection
multiplexer for the application lifetime. The provider disposes that connection during host
shutdown; request handlers and feature adapters must not create their own connections.

TASK-023 only establishes the distributed-cache connection boundary. `IShortUrlCache` remains
backed by the existing process-local memory implementation until TASK-024 defines redirect cache
serialization, TTL, invalidation, and fallback policy.

## Configuration

`RedisOptions` binds and validates the `Redis` section during startup. Every setting can be
overridden with ASP.NET Core's double-underscore environment variable form.

| Key | Environment variable | Constraint / behavior |
|---|---|---|
| `Redis:ConnectionString` | `Redis__ConnectionString` | Required valid StackExchange.Redis endpoint configuration. Supply credentials only through a secret source. |
| `Redis:KeyPrefix` | `Redis__KeyPrefix` | Required lowercase `application:environment:vN:` namespace, at most 100 characters. |
| `Redis:ConnectTimeoutMilliseconds` | `Redis__ConnectTimeoutMilliseconds` | 100-10,000 ms; default 2,000 ms. |
| `Redis:OperationTimeoutMilliseconds` | `Redis__OperationTimeoutMilliseconds` | 50-5,000 ms for synchronous and asynchronous operations; default 1,000 ms. |
| `Redis:ConnectRetryCount` | `Redis__ConnectRetryCount` | 0-5 repeat attempts during initial connection; default 2. |
| `Redis:ReconnectBaseDelayMilliseconds` | `Redis__ReconnectBaseDelayMilliseconds` | 100-60,000 ms; default 1,000 ms. |
| `Redis:ReconnectMaxDelayMilliseconds` | `Redis__ReconnectMaxDelayMilliseconds` | At least the base delay and no more than 300,000 ms; default 5,000 ms. |

The committed shared configuration contains no Redis endpoint or credentials. Development uses
the unauthenticated loopback endpoint `127.0.0.1:6379`; never expose that local instance outside
the developer machine. A deployed environment must provide its own connection string and a
deployment-specific prefix, for example:

```powershell
$env:Redis__ConnectionString = "redis.internal.example:6380,user=<user>,password=<secret>,ssl=true"
$env:Redis__KeyPrefix = "url-shortener:production:v1:"
```

Do not place the real value in a tracked `appsettings*.json` file, command log, or diagnostic
message. Use the deployment secret store or an untracked local configuration source.

## Key Namespace

The provider prepends `Redis:KeyPrefix` to every key used through `IDistributedCache`. The prefix
has exactly three segments:

```text
<application>:<environment>:<schema-version>:
```

Feature adapters append a feature-owned key such as `redirect:<short-code>`, producing a physical
key such as `url-shortener:production:v1:redirect:Ab12Cd34`. A schema-incompatible key layout must
increment the final version segment. Adapters must not use unprefixed global keys or duplicate the
application/environment prefix themselves.

## Timeout, Retry, and Outage Behavior

- `AbortOnConnectFail` is disabled so a temporary outage does not prevent host startup.
- Initial connection repeats are bounded by `ConnectRetryCount` and `ConnectTimeoutMilliseconds`.
- Disconnected commands use the fail-fast backlog policy and are not queued for later execution.
- Cache operations have the configured operation timeout and no application-layer retry in this
  infrastructure adapter.
- The shared multiplexer reconnects in the background for its lifetime with exponential delays
  bounded between the configured base and maximum values.
- When Redis is unavailable, resolving unrelated application routes continues to work. An actual
  `IDistributedCache` operation fails with a StackExchange.Redis connection/timeout exception
  instead of hanging or silently switching to process-local state. The consuming feature owns its
  correctness fallback; TASK-024 will define that policy for redirect lookup caching.

## Local Development

Start a provisional local Redis container from the repository root:

```powershell
docker run --name url-shortener-redis -d -p 6379:6379 redis:7.4-alpine redis-server --save "" --appendonly no
docker exec url-shortener-redis redis-cli ping
```

The ping must return `PONG`. The Development configuration already points to `127.0.0.1:6379`, so
the backend can then start normally:

```powershell
dotnet run --project UrlShortener.Api
```

If another local Redis endpoint is used, override it without editing tracked configuration:

```powershell
$env:Redis__ConnectionString = "127.0.0.1:6380"
dotnet run --project UrlShortener.Api
```

Inspect the namespace without assuming the provider's internal value representation:

```powershell
docker exec url-shortener-redis redis-cli --scan --pattern "url-shortener:development:v1:*"
```

Stop and remove only this named development container when it is no longer needed:

```powershell
docker stop url-shortener-redis
docker rm url-shortener-redis
```

The complete Docker Compose wiring remains deferred to Phase 15.
