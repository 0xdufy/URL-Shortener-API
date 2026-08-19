# Distributed Rate Limiting

## Policy Matrix

Rate limiting is opt-in endpoint metadata. Each active endpoint selects one named policy, and every
API instance evaluates that policy against atomic state in the configured Redis namespace. Redis
server time, rather than an API process clock, defines windows and refill calculations.

| Policy | Endpoints | Partition identity | Strategy | Default |
|---|---|---|---|---|
| `Anonymous` | `GET /api/v1/auth/bootstrap` | Effective client IP | Fixed window | 120 requests per 60 seconds |
| `AuthenticationRegistration` | `POST /api/v1/auth/register` | Effective client IP | Sliding window | 5 requests per 60 seconds |
| `AuthenticationSignIn` | `POST /api/v1/auth/sign-in` | Effective client IP | Sliding window | 10 requests per 60 seconds |
| `AuthenticationSession` | `POST /api/v1/auth/refresh`, `POST /api/v1/auth/sign-out` | Effective client IP | Sliding window | 30 requests per 60 seconds |
| `Authenticated` | Authenticated session metadata and short-URL management except creation | JWT `sub` user ID | Fixed window | 300 requests per 60 seconds |
| `UrlCreation` | `POST /api/v1/short-urls` | JWT `sub` user ID | Token bucket | Capacity 20; refill 20 tokens per 60 seconds |
| `ApiKey` | Scoped short-URL and analytics calls authenticated by API key | Authenticated `api_key_id` claim | Token bucket | Capacity 600; refill 600 tokens per 60 seconds |

The public `GET /r/{shortCode}` hot path deliberately has no rate-limit metadata. It is therefore
not charged to anonymous, authenticated, or URL-creation buckets and remains available when the
rate-limit store is unavailable. Edge-level redirect abuse controls may be added only as a
separate, explicitly measured policy.

IP policies use the effective `HttpContext.Connection.RemoteIpAddress` after the explicit proxy
trust middleware. Forwarding headers are disabled by default and are accepted only through the
known proxy/network boundary documented in [`proxy-trust.md`](proxy-trust.md). IPv4-mapped IPv6
addresses are normalized to native IPv4 text before the partition is hashed.

## Configuration and Validation

Every policy lives below `RateLimiting` and can be overridden with ASP.NET Core's double-underscore
environment-variable syntax. For example:

```powershell
$env:RateLimiting__AuthenticationSignIn__PermitLimit = "8"
$env:RateLimiting__AuthenticationSignIn__WindowSeconds = "60"
$env:RateLimiting__UrlCreation__PermitLimit = "15"
$env:RateLimiting__UrlCreation__TokensPerPeriod = "15"
$env:RateLimiting__UrlCreation__ReplenishmentPeriodSeconds = "60"
```

`Algorithm` must be `FixedWindow`, `SlidingWindow`, or `TokenBucket`. Every `PermitLimit` is bounded
to 1-100,000. Fixed/sliding windows must be 1-86,400 seconds. Token-bucket refill tokens must be
1-100,000 and no greater than bucket capacity; refill periods must be 1-86,400 seconds. Invalid
values fail startup through options validation. Capacity, refill amount, and refill period must
also allow an empty bucket to refill completely within seven days, which bounds inactive-key
retention. There is no silent fallback after configuration binding.

Changing an algorithm changes the stored value shape under the same feature key. Drain or remove
only the affected `ratelimit:v1:<policy>:*` keys during a coordinated configuration rollout, or
increment the feature key version in code when backward compatibility cannot be preserved.

## Redis State and Expiry

Physical keys use:

```text
<Redis:KeyPrefix>ratelimit:v1:<policy>:<sha256-partition>
```

The partition is SHA-256 hashed before it enters the key, so raw IP addresses, user IDs, and future
API-key IDs are not exposed through key names. One atomic Lua evaluation performs cleanup, count or
refill, admission, and expiry updates:

- Fixed-window hashes expire after the current-window remainder plus one full window.
- Sliding-window sorted sets expire one full window after their latest evaluation.
- Token-bucket hashes expire after the time required to refill an empty bucket plus one refill
  period.

Every limiter key therefore has a bounded positive TTL. API instances share the same
`RedisConnectionProvider` and connection multiplexer used by the distributed redirect cache; no
request or limiter creates a connection.

## HTTP and Failure Contract

Rejected requests return `429 Too Many Requests`, the common error envelope with
`error.code = RATE_LIMITED`, an integer-seconds `Retry-After` header, and `Cache-Control: no-store`.
The delay is computed from Redis state and rounded up to at least one second.

Rate-limited endpoints fail closed when Redis cannot evaluate a policy: they return
`503 RATE_LIMITING_UNAVAILABLE` rather than silently reverting to per-process or unbounded traffic.
The shared connection uses the bounded Redis timeouts and fail-fast backlog behavior documented in
[`redis.md`](redis.md). No application-layer retry is added to the request path.

Requests with absent or invalid credentials are left to authentication/authorization and return the
normal `401` contract; a user or API-key partition is never invented from an invalid credential.
Once an API key authenticates, its `ApiKey` policy overrides the endpoint's browser-session policy,
so each key has a partition distinct from both its owner's JWT `sub` partition and the owner's other
API keys.

## Two-Instance Verification

For a manual smoke check, run two API processes with the same SQL database, JWT signing key,
`Redis:ConnectionString`, and `Redis:KeyPrefix`, but different `--urls` values. Temporarily lower
limits through environment variables, alternate requests between the two ports, and confirm that
the combined request reaches one shared `429`. Exercise bootstrap, register, sign-in, refresh,
authenticated management, and creation independently. Then inspect only the verification
namespace and confirm every `ratelimit:v1:*` key has a positive `PTTL`.

Finally, repeat several redirects across both ports after exhausting creation and authenticated
management buckets. They must continue to return the redirect contract (`302`, `404`, or `410`)
rather than `429`.
