# TASK-057 — Edge Routing, TLS, and Public URL Configuration

**Status:** Planned  
**Phase:** 15 — Containerization & Deployment Baseline

## Goal

Define how Angular, API routes, redirects, health/metrics, and custom domains are exposed through a reverse proxy/load balancer with correct trusted forwarding and HTTPS semantics.

## Dependencies

- TASK-056 completed.

## Scope

- Configure/document reverse-proxy routing for Angular, `/api`, `/r`, and operational endpoints according to the chosen topology.
- Align proxy trusted networks/headers with TASK-027.
- Ensure public short URL generation uses canonical configured/trusted host/scheme.
- Document TLS termination and custom-domain certificate requirements without hardcoding one hosting vendor.
- Ensure SPA fallback does not swallow API/redirect/health routes.
- Restrict operational endpoints where appropriate.

## Acceptance Criteria

- [ ] Angular client-side routes work through the reverse proxy without intercepting `/api`, `/r`, health, or metrics routes.
- [ ] HTTPS scheme/host forwarded by trusted infrastructure produces correct canonical short URLs.
- [ ] Untrusted forwarded headers cannot override public URL generation.
- [ ] Default platform host and verified custom-domain routing have documented edge requirements.
- [ ] Metrics/administrative operational endpoints are not exposed publicly by accident.
- [ ] TLS/certificate responsibilities are explicit for default and custom domains.
- [ ] WebSocket/special proxy settings are added only if an actual selected dependency needs them.
- [ ] Manual HTTP→HTTPS/proxy/custom-host scenarios succeed in the documented environment.
- [ ] No automated test files are added.

## Verification

Run the stack through the selected local reverse proxy and verify Angular routes, API calls, redirect links, effective client IP, canonical scheme/host, health endpoints, and one custom-host scenario.