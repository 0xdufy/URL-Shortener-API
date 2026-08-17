# Persistence and Migrations

## Supported Modes

SQL Server is the production persistence mode. Set `Storage:UseInMemory` to `false` and provide `ConnectionStrings:SqlServer` through environment-specific configuration or the `ConnectionStrings__SqlServer` environment variable.

The in-memory repository is supported only for local development and manual smoke checks. It is process-local, loses all data at shutdown, does not execute relational constraints or transactions, does not validate EF Core mappings, and cannot model multi-instance behavior. Startup rejects it outside the `Development` environment.

The committed `appsettings.json` intentionally contains no connection string and defaults to SQL Server. `appsettings.Development.json` contains a LocalDB example and enables the in-memory repository so a developer can start the API without a database. Do not commit shared, staging, or production credentials.

The analytics worker always requires SQL Server, including in Development, because its event-ID
uniqueness and transaction guarantees cannot be represented by the API's in-memory repository.
End-to-end click analytics therefore require the API to use the same SQL Server database
(`Storage:UseInMemory=false`); links created only in the development in-memory repository are not
visible to the worker and their events are permanently rejected as missing-link events.

## Runtime Configuration

| Key | Purpose | Constraint |
|---|---|---|
| `Storage:UseInMemory` | Selects the development-only repository or SQL Server | Must be `false` outside Development |
| `ConnectionStrings:SqlServer` | SQL Server connection | Required when in-memory storage is disabled |
| `Persistence:CommandTimeoutSeconds` | SQL command timeout | 1 through 300 seconds |
| `Idempotency:RetentionHours` | Caller-scoped URL-create replay retention | 1 through 168 hours |
| `RateLimiting:<Policy>` | Redis-backed endpoint quota policy | See `rate-limiting.md` for per-algorithm bounds |

Options validation runs during startup. Invalid values fail with the full configuration key and
permitted range. Automatic SQL retries are intentionally disabled because commit ambiguity can
duplicate non-idempotent writes. Retry-capable URL creation uses its database-backed idempotency
contract instead; see [Idempotency and Request Resilience](idempotency-request-resilience.md).
The analytics consumer instead makes every delivery idempotent with the access-log primary key and
lets the bounded RabbitMQ redelivery policy retry transient failures.

## Migration Convention

- Treat committed migrations as append-only after they have been applied outside a disposable local database.
- Name migrations with a concise PascalCase outcome, for example `AddShortUrlOwner` or `AddApiKeyScopes`.
- Generate migrations in `UrlShortener.Infrastructure/Persistence/Migrations`.
- Review the generated operations and SQL before committing.
- Never call `Database.Migrate`, `EnsureCreated`, or an equivalent schema mutation from normal API startup.
- Apply migrations as an explicit deployment or operator step before routing traffic to the new application version.
- The design-time context factory uses `ConnectionStrings__SqlServer` when present and otherwise
  targets the local-development LocalDB database; it is not used by runtime dependency injection.

## Commands

Run commands from the repository root.

```powershell
dotnet tool restore
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:Storage__UseInMemory = "false"
dotnet ef migrations add <PascalCaseName> --project UrlShortener.Infrastructure --startup-project UrlShortener.Api --output-dir Persistence/Migrations
dotnet ef migrations script --idempotent --project UrlShortener.Infrastructure --startup-project UrlShortener.Api --output migration.sql
dotnet ef database update --project UrlShortener.Infrastructure --startup-project UrlShortener.Api
```

For Bash-compatible shells, set the same values with `export ASPNETCORE_ENVIRONMENT=Development` and `export Storage__UseInMemory=false`.

The development connection string can be overridden without editing tracked files:

```powershell
$env:ConnectionStrings__SqlServer = "<local SQL Server connection string>"
```

Production deployments should generate/review an idempotent script and apply it using a separately authorized identity. The API identity should not need schema-alter permissions.
