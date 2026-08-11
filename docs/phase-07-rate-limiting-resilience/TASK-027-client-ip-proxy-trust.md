# TASK-027 — Client IP and Reverse-Proxy Trust Model

**Status:** Planned  
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

- [ ] Trusted proxy/network configuration is explicit and environment-configurable.
- [ ] Forwarded headers are not trusted from arbitrary sources by default.
- [ ] Rate limiting receives the normalized effective client identity intended by the deployment model.
- [ ] Direct local development still produces a usable client identity.
- [ ] IPv4/IPv6 representations do not create obvious duplicate limiter identities for equivalent addresses where normalization is feasible.
- [ ] Configuration mistakes fail safely or are prominently diagnosed.
- [ ] Documentation explains what must change when deploying behind a new proxy/load balancer.
- [ ] Build and manual direct/proxied-header scenarios are verified.
- [ ] No automated test files are added.

## Verification

Demonstrate behavior for a direct request, a request through the configured trusted proxy path, and a spoofed forwarding-header request from an untrusted path.