# ADR 0005: Background Job Scheduling

- Status: Accepted
- Date: 2026-08-24
- Decision owners: URL Shortener maintainers
- Related task: TASK-048

## Context

Phase 13 introduces retention, cleanup, and analytics reconciliation work. These operations must not
extend API startup, share the click-message consumption loop, or execute concurrently when multiple
worker replicas are deployed. Each operation also needs bounded execution and an observable failure
boundary.

The application already has an independently deployable analytics worker and SQL Server is required
by both that worker and the maintenance operations. The current number of jobs does not require a
separate scheduler service, dashboard, or another durable infrastructure dependency.

## Decision

Run maintenance scheduling as a separate `BackgroundService` in
`UrlShortener.Analytics.Worker`. The scheduler and click consumer are independently registered hosted
services; a long maintenance operation never runs during API startup or inside message handling.
Job registrations and their schedule/retry/timeout settings are centralized under the
`MaintenanceJobs` worker configuration section.

Before executing a single-owner job, every worker replica attempts a SQL Server session-owned
exclusive application lock named `url-shortener:maintenance:<job-name>`. Lock acquisition does not
wait. A replica that does not acquire the lock records a structured skip and waits for its next local
schedule. The owning connection remains open for all attempts and releases the lock after completion;
SQL Server also releases it if the worker process or connection dies.

The scheduler provides these execution semantics:

- Every attempt receives the host shutdown token and a configurable per-attempt timeout token.
- `MaxAttempts` and `RetryDelaySeconds` bound retry count and delay. Retry happens while the same
  distributed lock is owned.
- Jobs must honor cancellation, be idempotent, and bound their own database batches. A cancellation
  token cannot forcibly terminate code that ignores it.
- Startup and each run emit structured job name, run ID, attempt, outcome, duration, and exception
  logs. A terminal failure is logged at `Error`; lock contention and shutdown cancellation are not
  failures. Phase 14 can turn these stable fields into metrics, traces, alerts, and status views.
- Schedules are fixed-delay, process-local triggers rather than exactly-once durable timers. A restart
  may make a job immediately eligible. The SQL lock prevents concurrent ownership, while idempotent
  job behavior makes restart and retry safe.

SQL scheduler credentials are not separate or embedded. The lock adapter uses the worker's
environment-supplied `ConnectionStrings:SqlServer` configuration and does not log it. Production can
later give the worker a least-privilege database principal that has the documented application-lock
and maintenance-data permissions.

## Alternatives Considered

### Run jobs in the API host

Rejected. API replicas scale for request traffic, deployments would couple API availability to
maintenance dependencies, and an accidental synchronous startup operation could delay readiness.

### Add Hangfire or Quartz with a persistent store

Deferred. Both offer durable schedules, richer misfire policies, and dashboards, but add package,
schema, upgrade, security, and operating surface that the current three-job phase does not need. The
host-native scheduler has an explicit replacement boundary; adopt a persistent scheduler if calendar
schedules, operator-triggered durable runs, or cross-restart execution history become requirements.

### Use only an in-process semaphore

Rejected. It cannot coordinate multiple worker replicas and would permit concurrent cleanup against
the shared database.

### Use a Redis lease

Rejected for this foundation. A lease needs renewal, expiry, and fencing-token semantics to prevent a
paused owner from overlapping a replacement. SQL Server session locks match the database maintenance
ownership boundary and are released with the owning session.

## Consequences

- The existing worker is the one maintenance deployment unit; no new deployable service is added.
- SQL Server must be reachable before a job can run. Lock acquisition failure is visible and the
  click consumer continues according to its own failure behavior.
- Different job names may execute concurrently. Jobs that touch the same exclusive resource must use
  the same registered job/lock name or add finer-grained coordination deliberately.
- There is no scheduler dashboard or durable run history in Phase 13. Structured logs are the current
  operator record; Phase 14 owns aggregation, alerting, and health presentation.
