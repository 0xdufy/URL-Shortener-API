# Click Event Transport

RabbitMQ is the selected durable transport for asynchronous click analytics. The architectural
rationale and reliability boundary are in [ADR 0004](adr/0004-rabbitmq-click-event-transport.md).
TASK-029 provides transport plumbing only: redirects do not publish yet, and the worker does not
persist analytics until TASK-030 and TASK-031 respectively.

## Local RabbitMQ

Run a current RabbitMQ 4 management image and retain broker state in a named volume:

```powershell
docker run --detach --name url-shortener-rabbitmq `
  --hostname url-shortener-rabbitmq `
  --publish 5672:5672 --publish 15672:15672 `
  --volume url-shortener-rabbitmq-data:/var/lib/rabbitmq `
  rabbitmq:4.2-management
```

Development configuration uses the image's loopback-only `guest`/`guest` account. The management UI
is available at `http://localhost:15672`. Do not reuse that account outside local development.

Start the reserved worker host with its Development profile:

```powershell
dotnet run --project workers/UrlShortener.Analytics.Worker
```

Startup declares these durable resources:

| Resource | Default name | Role |
| --- | --- | --- |
| Topic exchange | `url-shortener.events.v1` | Accepted application events |
| Quorum queue | `url-shortener.analytics.clicks.v1` | Click deliveries |
| Routing key | `analytics.click.v1` | Click binding |
| Dead-letter exchange | `url-shortener.events.dead.v1` | Failed-event routing |
| Dead-letter quorum queue | `url-shortener.analytics.clicks.dead.v1` | Operator-visible failures |

The `v1` names identify topology, not the click contract version. Contract compatibility is carried
in the provider-neutral event envelope described below.

## Click contract

Successful redirects publish event name `analytics.click`, contract version `1`. The application
creates one stable UUID event ID per logical publication attempt. RabbitMQ also receives that ID as
the AMQP message ID, so a future consumer can use it as its idempotency key.

The JSON serialization uses the .NET web defaults: UTF-8, `application/json`, camel-case property
names, UUID strings, ISO 8601 UTC timestamps, and an ISO `yyyy-MM-dd` date. A representative event
is:

```json
{
  "eventId": "018f65a1-7f69-7d3e-89a2-2fbc45bd53a4",
  "eventName": "analytics.click",
  "contractVersion": 1,
  "occurredAtUtc": "2026-08-17T12:34:56.789+00:00",
  "payload": {
    "shortUrlId": "9a3b7418-42de-48f4-a1f7-a4e6116e863a",
    "accessedAtUtc": "2026-08-17T12:34:56.789+00:00",
    "referrerHost": "example.com",
    "userAgent": "ExampleBrowser/1.0",
    "pseudonymousVisitorId": "7F4ML9F_BbVkxawXTwJrJzms1k85fAQBt1b8UrbX4tI",
    "visitorIdentityPeriodUtc": "2026-08-17",
    "visitorIdentityScheme": "hmac-sha256-utc-day-v1"
  }
}
```

The redirect resolver captures the access time in UTC once, at the start of resolution before the
cache lookup. The same instant becomes both `occurredAtUtc` and `payload.accessedAtUtc`. Publication
occurs only after the existing authoritative access guard accepts the active, non-deleted,
non-expired link. Missing, inactive, deleted, expired, and stale-cache-rejected requests do not
publish successful-click events.

Version 1 is immutable for field names, types, requiredness, and meaning. Consumers must select on
both event name and contract version and ignore unknown JSON properties. A new optional field may be
added compatibly only when old consumers can safely ignore it; removing or renaming a field,
changing its type/meaning, or making an optional field required needs a new contract version and a
deployment overlap that can read both versions.

## Privacy boundary

The queue payload deliberately excludes the destination URL, short code, owner, raw IP address,
referrer path/query/fragment, and HTTP request identifiers. An absolute HTTP(S) referrer is reduced
to its lower-case IDN host; missing, malformed, and non-HTTP referrers become `null`. User agent is
trimmed and capped at 256 characters because TASK-035 needs it for later category enrichment.

The normalized effective client IP is converted before publication to a base64url HMAC-SHA-256
identifier. The HMAC input includes the UTC calendar date, and the date is carried explicitly in
`visitorIdentityPeriodUtc`; identifiers therefore support same-day unique-visitor estimates but
cannot be joined across days. Raw IP never enters the event body or publication logs. The HMAC key
must contain at least 32 random bytes and is configured through
`ClickEvents:VisitorIdentityHmacKeyBase64` (environment variable
`ClickEvents__VisitorIdentityHmacKeyBase64`). Development contains an obvious local-only key;
deployed environments must inject a distinct secret and preserve it across API replicas. Key
rotation intentionally starts a new identity population. TASK-046 still owns the full metadata
inventory, retention rules, and any future contract migration.

Generate a deployment key with:

```powershell
$clickEventKey = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Fill($clickEventKey)
$env:ClickEvents__VisitorIdentityHmacKeyBase64 = [Convert]::ToBase64String($clickEventKey)
```

## Redirect-facing publication policy

Publication is best effort and fail-open for the redirect. The API makes one publisher-confirmed
attempt within the configured RabbitMQ connection/operation bounds. It has no process-local buffer
and does not retry an ambiguous failure. A broker rejection, timeout, or connection failure writes
a warning containing only event ID and short-link ID, then the valid redirect remains available.
Consequently that click may be lost; if RabbitMQ accepted it but its confirmation was lost, it may
instead be present once and a future external replay could duplicate it. TASK-031 must deduplicate
on event ID.

This policy prioritizes redirect availability over complete analytics during a broker outage.
Operators can observe each failed attempt in structured logs until Phase 14 adds aggregate
telemetry. Cancellation caused by the HTTP request is propagated rather than converted into a
publication failure.

## Configuration

All settings use the `RabbitMq` configuration section and can be overridden with double-underscore
environment variables. Credentials are intentionally empty in base configuration so a non-
Development host fails validation until secrets are supplied.

| Key | Default | Meaning |
| --- | --- | --- |
| `HostName`, `Port`, `VirtualHost` | `localhost`, `5672`, `/` | Broker endpoint and isolated vhost |
| `UserName`, `Password` | empty | Required broker credentials; use a secret source |
| `UseTls`, `TlsServerName` | `false`, empty | TLS transport and expected server name |
| `ClientProvidedName` | host-specific | Connection label visible to operators |
| `ConnectionTimeoutMilliseconds` | `5000` | Initial/replacement connection bound |
| `OperationTimeoutMilliseconds` | `5000` | Topology RPC and publish-confirm bound |
| `RequestedHeartbeatSeconds` | `30` | AMQP heartbeat request |
| `NetworkRecoveryIntervalMilliseconds` | `5000` | Established-connection recovery cadence |
| `ConsumerPrefetchCount` | `32` | Maximum unacknowledged deliveries per consumer channel |
| `DeliveryLimit` | `5` | Quorum delivery attempts before dead lettering |
| `RetryBaseDelayMilliseconds` | `250` | Consumer exponential-backoff base |
| Exchange/queue/routing keys | table above | Environment-specific topology names when isolation requires it |

Example production secret injection:

```powershell
$env:RabbitMq__HostName = "rabbitmq.internal"
$env:RabbitMq__VirtualHost = "/url-shortener-production"
$env:RabbitMq__UserName = "url-shortener"
$env:RabbitMq__Password = "<secret-from-vault>"
$env:RabbitMq__UseTls = "true"
$env:RabbitMq__TlsServerName = "rabbitmq.internal"
$env:ClickEvents__VisitorIdentityHmacKeyBase64 = "<base64-secret-from-vault>"
```

Use separate topology names or, preferably, separate virtual hosts for environments that share a
broker. Production uses a three-member RabbitMQ cluster; a local one-node quorum queue verifies the
protocol but not node-failure tolerance.

## Outage verification

With RabbitMQ stopped, worker startup must fail after the configured connection bound and must not
fall back to memory. A publisher call fails with the same bounded connection/operation behaviour.
Because a confirmation can be lost after RabbitMQ accepted a message, a failed publish is sometimes
ambiguous and a caller retry can duplicate it.

After RabbitMQ starts, worker startup declares compatible topology. A publisher-confirmed event is
visible in the click queue until a consumer manually acknowledges it. A consumer `Retry` outcome is
requeued with bounded backoff; after the quorum delivery limit it is visible in the dead-letter
queue. Invalid or permanently rejected messages are dead-lettered immediately. No automatic dead-
letter replay is configured.
