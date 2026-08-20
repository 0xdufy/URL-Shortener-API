# TASK-041 — Verified Domain Routing and Short URL Generation

**Status:** Completed
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

- [x] Only verified, enabled, owner-accessible domains can be selected for new branded links.
- [x] Redirect resolution cannot confuse the same code across hosts if per-domain code reuse is allowed.
- [x] Cache keys uniquely represent redirect identity under the chosen host/code model.
- [x] User-controlled Host/X-Forwarded-Host headers cannot generate arbitrary persisted public URLs outside the trusted proxy model.
- [x] Disabling/unverifying a domain prevents its continued use according to a documented policy.
- [x] Platform default-domain links continue to work.
- [x] TLS/DNS expectations are documented; application code does not falsely claim it can provision certificates without configured infrastructure.
- [x] Backend build and controlled host-routing verification succeed.
- [x] No automated test files are added.

## Verification

Use the documented local host-mapping/proxy setup to verify default host, verified custom host, unverified host, wrong host/code combination, and cache invalidation after domain disable.

## Implementation and Verification Notes

- 2026-08-20: Retained global case-sensitive short-code uniqueness explicitly. Added nullable
  custom-domain assignment plus a composite owner/domain foreign key, verified-owner selection on
  create and full update, branded response metadata, and `409 CUSTOM_DOMAIN_UNAVAILABLE` for any
  ineligible selection.
- Redirect resolution now keys authoritative lookup by normalized effective host plus code. The
  platform host accepts only unassigned links; a custom host accepts only links assigned to its
  currently verified claim. Wrong, unknown, failed, pending, and disabled host routes fail closed
  as `404` while platform links retain their prior behavior.
- Redirect cache schema/key version 2 includes normalized routing host. Domain disable and
  verification restart invalidate all assigned keys, and the persisted cache guard independently
  checks current host assignment and verified state to cover removal failure and fill races.
- Canonical platform URLs still use `PublicUrls:BaseUrl`; branded URLs use the persisted normalized
  domain and startup-validated `PublicUrls:CustomDomainScheme`. Request and forwarding headers do
  not construct public URLs. DNS traffic records, certificate issuance/renewal, SNI, and proxy
  `Host` preservation remain explicit deployment responsibilities documented in
  `docs/custom-domain-routing.md`.
- Migrations `20260820190104_AddCustomDomainVerificationModel` and
  `20260820190159_AddCustomDomainRouting` are split at the TASK-040/TASK-041 boundary. EF reported
  no pending model changes. A disposable in-memory harness exercised the default host, verified
  custom host, unverified host, both wrong host/code combinations, global code collision,
  host-separated cache identity, and cache/route invalidation after disable; it passed and was
  removed. No automated test file was added.
