# ADR 0004: RabbitMQ Click Event Transport

- Status: Accepted
- Date: 2026-08-17
- Decision owners: URL Shortener maintainers
- Related task: TASK-029

## Context

Successful redirects currently perform analytics persistence in the request path. Phase 08 moves
that work into an independently hosted worker, so the API and worker need a transport that survives
process restarts and supports multiple API/worker instances. Click processing may be retried, and a
duplicate delivery must not produce a duplicate logical click.

The repository already uses Redis for caching and distributed rate limiting, but those uses are
configured to fail fast and may use eviction. Analytics handoff needs a deliberately durable store,
visible failed-message disposition, and acknowledgement semantics rather than cache semantics.

## Decision

Use RabbitMQ AMQP 0-9-1 with a durable topic exchange, a durable quorum click queue, publisher
confirms, persistent messages, manual consumer acknowledgements, and a durable quorum dead-letter
queue. The API and analytics worker share the transport adapter in Infrastructure while Application
owns the provider-neutral `IEventPublisher`, `IEventConsumer`, event envelope, and handling outcomes.
RabbitMQ client types do not enter redirect, domain, or analytics use-case code.

The production queue must have three RabbitMQ members on separate failure domains. A single-node
RabbitMQ instance is acceptable only for local development and cannot provide node-failure
durability. Exchanges and queues are declared idempotently by the publisher, consumer, and worker
startup boundary. The API does not open a broker connection until publication is requested; the
worker establishes connectivity and topology during startup. Connections are reused for process
lifetime, channels are owned by their publisher/consumer boundary, and established connections use
the client's automatic network and topology recovery.

### Delivery and durability semantics

The design is **at least once**, not exactly once:

- A publish is successful only after the broker confirms the persistent message. A quorum queue
  confirms after a quorum has accepted the message. An unconfirmed publish can be lost.
- A publish timeout or connection loss around confirmation is ambiguous: the caller can observe a
  failure even though RabbitMQ retained the event. Retrying may create a duplicate.
- A consumer acknowledges only after its application handler reports successful durable work.
  Worker termination or connection loss before acknowledgement causes redelivery.
- TASK-030 must assign every click a stable event ID. TASK-031 must enforce that ID as a unique
  persistence key in the same correctness boundary as analytics updates. Broker behaviour alone
  does not prevent double counting.
- The queue preserves broker order for ready deliveries, but processing order is not a product
  guarantee. Multiple consumers, redelivery, dead-lettering, and late events can reorder clicks.
  Analytics logic must use event timestamps and idempotency rather than arrival order.

### Retry and failure disposition

- Publishing has a bounded connection timeout and operation/confirmation timeout. The adapter does
  not blindly replay an ambiguous publish. TASK-030 owns the redirect-facing policy when a bounded
  publish fails.
- A transient handler failure maps to a negative acknowledgement with requeue. The consumer waits
  with exponential backoff based on the quorum queue's delivery count. The queue has a configured
  delivery limit (five by default), after which RabbitMQ dead-letters the event.
- Invalid JSON, incompatible envelope metadata, and an application-declared permanent failure are
  rejected without requeue and dead-lettered immediately.
- The source quorum queue uses RabbitMQ's at-least-once dead-letter strategy and `reject-publish`
  overflow mode. The dead-letter queue is durable and has no automatic replay or expiry. Operators
  inspect and explicitly replay or discard its messages after correcting the cause. Queue-depth and
  alert automation is deferred to the observability phase.

### Broker outage behaviour

There is no process-local fallback and no hidden in-memory buffer. Before TASK-030 integrates the
publisher, redirect behaviour is unchanged. After integration, a broker outage makes publication
fail within the configured bound; TASK-030 must explicitly choose the user-visible redirect policy
and acceptable analytics loss rather than allowing transport code to make that product decision.
The worker fails startup if it cannot connect or declare compatible topology. An established client
connection attempts recovery at the configured interval, and unacknowledged deliveries remain in
RabbitMQ for redelivery.

Credentials are supplied only through environment variables or another secret provider. Production
deployments use a least-privilege virtual-host user and TLS when traffic leaves a trusted private
network. Secret-bearing connection data is never logged.

## Alternatives Considered

### Process-local `Channel<T>`

Rejected as the production transport. It is inexpensive and useful for single-process throughput,
but queued clicks disappear on API restart, cannot be shared across API replicas, cannot feed an
independent worker without adding another transport, and provides no durable dead-letter state. A
transitional channel would create two failure models and migration work immediately before the
durable design is required.

### Redis Streams

Rejected for this deployment. Reusing Redis would reduce the number of services, and consumer
groups provide pending-entry recovery, but reliable analytics would require a separately managed
non-evicting, persisted Redis role plus custom retry/dead-letter operations. Sharing the current
cache/rate-limit Redis makes eviction and cache maintenance part of event durability. Once isolated,
the operational saving over a purpose-built RabbitMQ broker is small while its failure boundaries
are less explicit for this portfolio.

### Apache Kafka

Rejected at the expected workload. Kafka has excellent retained-log replay and partitioned
throughput, but its cluster, partition, retention, and consumer-group operating cost is not justified
for one click-processing workflow. RabbitMQ demonstrates the required distributed delivery
semantics with less local and hosted complexity.

## Consequences

- RabbitMQ becomes a required API/worker deployment dependency and is added to the Phase 15 compose
  stack. Production operation carries the cost of a three-member broker cluster, persistent storage,
  monitoring, backups/policies, upgrades, and dead-letter triage.
- Quorum replication and publisher confirms add more publish latency than `Channel<T>` or a
  non-durable queue. That latency buys a clear accepted-message durability boundary.
- The API and worker can scale independently without splitting business ownership into a new
  microservice. The worker remains a composition root that invokes Application use cases.
- TASK-030 defines the privacy-aware click payload and redirect-facing failure policy. TASK-031 adds
  the idempotent persistence handler and operational retry classification. Neither concern belongs
  in the RabbitMQ adapter.

## References

- [RabbitMQ quorum queue data safety and dead lettering](https://www.rabbitmq.com/docs/quorum-queues)
- [RabbitMQ publisher confirms and consumer acknowledgements](https://www.rabbitmq.com/docs/confirms)
