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
in the provider-neutral event envelope and is finalized in TASK-030.

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
