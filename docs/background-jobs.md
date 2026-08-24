# Background Job Scheduling

Maintenance jobs run in `UrlShortener.Analytics.Worker` beside, but independently from, the RabbitMQ
click consumer. The API does not register the scheduler and never waits for cleanup during startup.
The architectural rationale is recorded in [ADR 0005](adr/0005-background-job-scheduling.md).

## Registration and configuration

An implementation of `IMaintenanceJob` is registered once in
`MaintenanceServiceCollectionExtensions`. Its unique `Name` must have a matching entry in
`MaintenanceJobs:Jobs`. Unknown configured names fail worker startup rather than silently doing
nothing.

```json
{
  "MaintenanceJobs": {
    "Enabled": true,
    "PollIntervalSeconds": 5,
    "Jobs": {
      "foundation-heartbeat": {
        "Enabled": false,
        "IntervalSeconds": 300,
        "TimeoutSeconds": 10,
        "MaxAttempts": 1,
        "RetryDelaySeconds": 0
      }
    }
  }
}
```

Production defaults keep the harmless foundation heartbeat off. Development configuration enables
it every 30 seconds. Any value can be overridden with the normal .NET configuration hierarchy, for
example `MaintenanceJobs__Jobs__foundation-heartbeat__IntervalSeconds=60`. Secrets do not belong in
this section. SQL lock authentication comes only from `ConnectionStrings__SqlServer` or another
configured secret provider.

## Execution contract

Each local schedule tick first tries the SQL Server application lock for the job. Only the owning
replica runs; contenders log a skip without waiting. The session stays open across retries, so another
replica cannot enter between attempts. Jobs with different names do not exclude one another.

Every job implementation must:

- treat re-execution as safe and use bounded batches/transactions;
- pass the received cancellation token to database, cache, and delay operations;
- avoid detached work that can outlive `ExecuteAsync`;
- avoid logging secrets, raw tokens, raw IP addresses, or sensitive payloads;
- throw on failure so the scheduler can retry and record the terminal outcome.

The configured timeout applies to each attempt. `MaxAttempts` includes the first attempt. A timeout
requests cooperative cancellation; it cannot safely kill code that ignores its token. Worker shutdown
cancels the active attempt and does not count as a failed run.

## Operator visibility

Search worker logs by `JobName` and `RunId`. Every acquired run logs start and one terminal outcome:
success, exhausted failure, or shutdown cancellation. Attempts log their number, exceptions, and
timeouts; lock contention logs an explicit skip. Success and terminal failure include total duration.
These structured fields are the Phase 13 status surface and are stable inputs to Phase 14 telemetry.

## Manual verification

1. Apply current migrations and start SQL Server and RabbitMQ using non-production credentials.
2. Start two Development worker instances against the same SQL database and RabbitMQ virtual host.
3. Confirm `foundation-heartbeat` runs immediately and then every configured interval.
4. For a deterministic contention window, use a database session to acquire the exclusive
   session-owned application lock named `url-shortener:maintenance:foundation-heartbeat`, then start
   the other worker. Confirm it logs that the job was skipped and does not log a concurrent start.
5. Stop the owner and confirm a later tick on the remaining instance can acquire the lock.
6. Temporarily lower `TimeoutSeconds` below the harmless delay and confirm attempts are bounded and a
   terminal `Error` is logged after `MaxAttempts`.

Do not use production credentials or leave verification-only schedule overrides deployed.
