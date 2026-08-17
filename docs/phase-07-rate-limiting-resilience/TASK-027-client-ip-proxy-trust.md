# TASK-027 — Client IP and Reverse-Proxy Trust Model

**Status:** Completed
**Phase:** 07 — Distributed Rate Limiting & API Resilience

## Goal

Ensure client-IP-dependent security, analytics, and rate limiting use a deliberate reverse-proxy trust model rather than blindly trusting spoofable forwarding headers.

## Dependencies

- TASK-026 completed.

## Scope

- Document expected deployment topology and trusted proxy boundaries.
- Configure ASP.NET Core forwarded-header handling according to that topology.
- Define how client IP is derived for direct local development and proxied deployment.
- Ensure untrusted clients cannot override effective IP simply by sending forwarding headers.
- Define IPv4/IPv6 normalization where rate-limit keys or privacy hashing depend on it.
- Update deployment configuration requirements for later Docker/reverse-proxy phases.

## Acceptance Criteria

- [x] Trusted proxy/network configuration is explicit and environment-configurable.
- [x] Forwarded headers are not trusted from arbitrary sources by default.
- [x] Rate limiting receives the normalized effective client identity intended by the deployment model.
- [x] Direct local development still produces a usable client identity.
- [x] IPv4/IPv6 representations do not create obvious duplicate limiter identities for equivalent addresses where normalization is feasible.
- [x] Configuration mistakes fail safely or are prominently diagnosed.
- [x] Documentation explains what must change when deploying behind a new proxy/load balancer.
- [x] Build and manual direct/proxied-header scenarios are verified.
- [x] No automated test files are added.

## Verification

Demonstrate behavior for a direct request, a request through the configured trusted proxy path, and a spoofed forwarding-header request from an untrusted path.

## Completion Notes

- Added startup-validated `ProxyTrust` configuration with forwarding disabled by default, an
  explicit 1-10 hop bound, and deliberate known-proxy/known-network lists. Framework loopback
  defaults are cleared, and only `X-Forwarded-For` is processed before logging, authentication,
  rate limiting, and controllers.
- Centralized effective client-IP formatting for rate limits, URL creation, and redirect analytics.
  IPv4-mapped IPv6 addresses collapse to native IPv4 text; absent addresses use the conservative
  shared `unknown` identity.
- Added `docs/proxy-trust.md` with direct/proxied topologies, environment-variable examples,
  proxy-chain sanitization requirements, topology-change steps, safe-failure behavior, and manual
  verification guidance. Updated rate-limit and repository documentation to reference the model.
- On 2026-08-17, an isolated one-request registration policy produced `400,429` when a fabricated
  forwarding header was followed by a direct request with forwarding disabled. With loopback
  explicitly trusted, two forwarded client addresses produced `400,400`. With a different proxy
  trusted, the same two headers produced `400,429`, confirming the untrusted peer could not select
  its partition. Forwarded `::ffff:192.0.2.25` followed by `192.0.2.25` produced `400,429`,
  confirming mapped/native normalization.
- Enabling proxy processing with empty trust lists failed startup with the expected options
  validation message. `dotnet format UrlShortener.sln --verify-no-changes --no-restore` and
  `dotnet build UrlShortener.sln --no-restore` completed with zero warnings and errors. Temporary
  API processes and isolated Redis keys were removed. No automated test files were added.
