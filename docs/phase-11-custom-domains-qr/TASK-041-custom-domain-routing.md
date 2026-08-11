# TASK-041 — Verified Domain Routing and Short URL Generation

**Status:** Planned  
**Phase:** 11 — Custom Domains & QR Codes

## Goal

Use verified custom domains safely in link creation and redirect resolution while keeping cache keys and host routing unambiguous.

## Dependencies

- TASK-040 completed.

## Scope

- Allow link creation/update to select an owned verified domain where supported.
- Define uniqueness semantics for `(host, shortCode)` versus global short-code uniqueness and migrate only through an explicit decision.
- Resolve redirects using both effective host and code when custom-domain routing requires it.
- Include host/domain identity in distributed cache keys where necessary.
- Generate short URLs from canonical configured/public host data rather than untrusted request headers.
- Document DNS/TLS/reverse-proxy requirements for actual deployment.

## Acceptance Criteria

- [ ] Only verified, enabled, owner-accessible domains can be selected for new branded links.
- [ ] Redirect resolution cannot confuse the same code across hosts if per-domain code reuse is allowed.
- [ ] Cache keys uniquely represent redirect identity under the chosen host/code model.
- [ ] User-controlled Host/X-Forwarded-Host headers cannot generate arbitrary persisted public URLs outside the trusted proxy model.
- [ ] Disabling/unverifying a domain prevents its continued use according to a documented policy.
- [ ] Platform default-domain links continue to work.
- [ ] TLS/DNS expectations are documented; application code does not falsely claim it can provision certificates without configured infrastructure.
- [ ] Backend build and controlled host-routing verification succeed.
- [ ] No automated test files are added.

## Verification

Use the documented local host-mapping/proxy setup to verify default host, verified custom host, unverified host, wrong host/code combination, and cache invalidation after domain disable.