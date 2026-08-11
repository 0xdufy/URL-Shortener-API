# Phase 00 Repository Audit Report

**Audit date:** 2026-08-11  
**Audited state:** Current working tree, including pre-existing unstaged changes  
**SDK:** .NET SDK 10.0.110 selected by `global.json` (`10.0.100`, `latestPatch`)

## 1. Solution and project map

| Project | Role | Target | Direct project references | Direct packages |
|---|---|---|---|---|
| `UrlShortener.Domain` | Entities | `net10.0` | None | None |
| `UrlShortener.Application` | DTOs, validation, ports, orchestration | `net10.0` | Domain | FluentValidation 12.1.1 |
| `UrlShortener.Infrastructure` | EF Core, repositories, cache, rate limiting, system adapters | `net10.0` | Application, Domain | EF Core/SQL Server/Design 10.0.10; Microsoft.Extensions.Options 10.0.10 |
| `UrlShortener.Api` | HTTP controllers, middleware, composition root, OpenAPI, logging | `net10.0` | Application, Infrastructure | FluentValidation DI 12.1.1; EF Design 10.0.10; Serilog ASP.NET Core 10.0.0; Serilog File 7.0.0; Swashbuckle 10.2.3 |

The solution has no test, Angular, or worker projects. `web/` and `workers/` contain placeholder README files only. The repository-pinned local tool is `dotnet-ef` 10.0.10.

## 2. Dependency direction

The effective compile-time direction is:

`Domain <- Application <- Infrastructure <- Api`, with API also referencing Application directly and Infrastructure referencing Domain directly.

Domain has no outward dependencies. Application owns interfaces implemented by Infrastructure. API is the composition root. Business orchestration currently lives in `UrlShortener.Application/Services/ShortUrlService.cs`; transport concerns live in controllers; persistence and process-local mechanisms live in Infrastructure.

## 3. Public endpoint inventory

| Method | Route | Success | Relevant errors |
|---|---|---|---|
| POST | `/api/v1/short-urls` | `201` with `ShortUrlResponse` and relative `Location` | `400`, `409`, `429`, `500` |
| GET | `/api/v1/short-urls/{shortCode}` | `200` with `ShortUrlDetailsResponse` | `404` |
| PATCH | `/api/v1/short-urls/{shortCode}/status` | `200` with `ShortUrlDetailsResponse` | `400`, `404` |
| DELETE | `/api/v1/short-urls/{shortCode}` | `204` | `404` |
| GET | `/api/v1/short-urls/{shortCode}/stats?fromUtc=&toUtc=` | `200` with `StatsResponse` | `404` |
| GET | `/r/{shortCode}` | `302` with `Location` | `404`, `410` |

All explicit API errors use `{ traceId, error: { code, message, details[] } }`. Validation errors are normalized by `ExceptionHandlingMiddleware`. Unknown framework-level routes/methods and some failures occurring outside that middleware are not guaranteed to use this envelope.

## 4. Data model and indexes

`ShortUrl` contains `Id`, destination and case-sensitive `ShortCode`, creation/expiry timestamps, active/deleted state, deletion timestamp, total click count, last-access timestamp, and access-log navigation. SQL Server configuration limits the destination to 2,048 characters and the short code to 20, uses `Latin1_General_CS_AS`, and creates a unique index on `ShortCode` plus indexes on `IsDeleted` and `ExpiresAtUtc`.

`ShortUrlAccessLog` contains `Id`, required `ShortUrlId`, `AccessedAtUtc`, optional IP address (64), user agent (256), and referrer (512). It has a cascade foreign key to `ShortUrl` and a composite index on `(ShortUrlId, AccessedAtUtc)`. The initial EF migration and model snapshot exist under `UrlShortener.Infrastructure/Persistence/Migrations/` in the audited working tree.

Persistence modes are SQL Server through EF Core and a process-local in-memory repository. In-memory storage is permitted only in Development, is case-sensitive, process-local, non-durable, and guarded by locks.

## 5. Cache and rate-limit behavior

`ShortUrlCache` wraps `IMemoryCache` with key `su:{shortCode}`. Creation fills the cache; status change and deletion invalidate it. A cached item lives until link expiry, with a minimum one-minute TTL, or 24 hours for links without expiry. Missing links are not negative-cached. State is process-local, so cache contents and invalidation do not coordinate across instances.

`InMemoryRateLimiter` applies only to POST creation. It keys a fixed UTC-minute bucket by the directly observed remote IP, allows `RateLimiting:CreatePerMinuteLimit` requests (default 20), and returns `Retry-After` until the next minute. State and locking are process-local; forwarded-client-IP trust is not configured.

## 6. Redirect read/write path

`GET /r/{shortCode}` reads the process-local cache, falling back to the repository. Deleted, inactive, and missing records return `404`; expired records return `410`. Before returning `302`, every successful redirect synchronously:

1. Atomically increments `ClickCount` and updates `LastAccessedAtUtc` in SQL Server (or under the in-memory repository lock).
2. Creates a `ShortUrlAccessLog` containing IP, user agent, and referrer.
3. Saves the access log.

Therefore redirect latency and availability depend on analytics persistence. If the conditional increment fails, the service removes the cache entry and ultimately returns `404`.

## 7. Configuration and secrets assessment

Required sections are `Storage`, `Persistence`, and `RateLimiting`. SQL mode additionally requires `ConnectionStrings:SqlServer`. `Serilog:MinimumLevel` is optional in practice and defaults to Information. Development contains only a LocalDB trusted-connection example; no password, token, API key, or other private credential was found in inspected tracked source/configuration. Production defaults to SQL mode without committing a connection string, so it fails fast until one is supplied externally.

Serilog writes to console and rolling files under `logs/`. Swagger is enabled in every environment. The application derives public short URLs from the unvalidated incoming request scheme and Host header. Direct remote IP, user agent, and referrer values are persisted for analytics.

## 8. Tracked generated and local artifacts

Before cleanup, Git tracked 300 files, including 181 files under project `bin/` and `obj/` directories:

| Path group | Count |
|---|---:|
| `UrlShortener.Api/bin` | 71 |
| `UrlShortener.Api/obj` | 35 |
| `UrlShortener.Application/bin` | 5 |
| `UrlShortener.Application/obj` | 20 |
| `UrlShortener.Domain/bin` | 3 |
| `UrlShortener.Domain/obj` | 18 |
| `UrlShortener.Infrastructure/bin` | 8 |
| `UrlShortener.Infrastructure/obj` | 21 |

These outputs include stale .NET 8 assemblies and machine-local NuGet paths such as `C:\Users\Adham\.nuget\packages`. `UrlShortener.Api/UrlShortener.Api.csproj.user` is also tracked and contains a user-specific selected debug profile. No tracked log file was found. EF migrations, `global.json`, `.config/dotnet-tools.json`, launch settings, and safe appsettings files are intentional source/configuration and must remain.

## 9. Risk register

| Severity | Risk and evidence | Recommended disposition |
|---|---|---|
| High | Short-code creation checks existence and inserts later (`ShortUrlService.CreateAsync`), so concurrent requests can race. The database unique index is authoritative, but its violation is not mapped to `409`. | Make creation concurrency-safe and map uniqueness conflicts in Phase 03. |
| High | Redirects synchronously increment and persist a detailed access log (`ShortUrlService.RegisterAccessAsync`). Dependency latency/failure affects the hot path. | Introduce the queue/worker design in Phase 08. |
| High | Cache and rate limits use `IMemoryCache`; each API instance has independent state and invalidation. | Replace with distributed mechanisms in Phases 06 and 07. |
| Medium | The application persists raw client IP, user-agent, and referrer data with no documented retention policy. | Minimize/enrich privacy-safely in Phases 09, 12, and 13. |
| Medium | Client IP comes directly from `RemoteIpAddress`; proxy trust/forwarded headers are not configured. | Define trusted proxies and client-IP handling in Phase 07. |
| Medium | Generated binaries, intermediate outputs, `.csproj.user`, and absolute machine paths are tracked. | Remove from tracking and strengthen `.gitignore` in TASK-002. |
| Medium | Swagger is exposed in every environment and Host is used to construct returned URLs without an allowed-host/public-origin policy. | Harden HTTP/configuration boundaries in Phase 12. |
| Medium | Expiry validation reads `DateTime.UtcNow` directly while runtime services otherwise use `IDateTimeProvider`; DateTime kind/query-range ordering is not explicitly validated. | Normalize temporal validation/contracts in Phase 03. |
| Low | `UpdateStatusRequestValidator` accepts every possible Boolean value and provides no effective validation. | Remove or revise when mutation contracts are expanded in Phase 03. |
| Low | Stats report `TotalClicks` only within the selected date range despite an entity-level lifetime counter with the same conceptual name. | Clarify analytics naming/contracts in Phase 09. |

## 10. Preconditions before Phase 01

Complete repository cleanup, publish repository-specific engineering standards, and freeze the baseline contracts. Preserve the current unique database constraint and migration metadata. Do not interpret later distributed, identity, async, or analytics work as Phase 00 scope.

The audit found pre-existing unstaged changes consistent with platform modernization (including .NET 10, configuration work, and migrations). This report deliberately describes the inspected working tree so subsequent work has one truthful baseline; those changes were not reverted or overwritten by Phase 00.

## Verification evidence

- `dotnet --version` -> `10.0.110`.
- `dotnet restore UrlShortener.sln` -> succeeded for all four projects after NuGet network access was allowed. The first sandboxed attempt failed with `NU1301` because outbound access to `api.nuget.org:443` was blocked; this was environmental, not a repository defect.
- `dotnet build UrlShortener.sln --no-restore` -> succeeded, 0 warnings, 0 errors.
- `dotnet list UrlShortener.sln package --no-restore` -> succeeded and produced the dependency inventory summarized above.
- Tracked-file and credential/path scans used `git ls-files` and `git grep -n -I`; no private credential was found, and the generated/local findings above were confirmed.

