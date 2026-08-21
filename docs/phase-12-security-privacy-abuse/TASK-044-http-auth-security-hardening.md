# TASK-044 — HTTP, Authentication, and Session Security Hardening

**Status:** Completed
**Phase:** 12 — Security, Privacy & Abuse Hardening

## Goal

Review the now-complete authentication/browser/API surfaces as one threat boundary and harden transport, session, origin, and credential handling.

## Dependencies

- Phase 11 completed.

## Scope

- Review CORS/trusted origins and credentials behavior.
- Review CSRF protection according to the Phase 02 browser-auth strategy.
- Review cookie/token flags, lifetimes, rotation/revocation, and logout semantics.
- Add appropriate security headers for API/Angular hosting topology.
- Ensure HTTPS/proxy scheme handling is correct in deployed mode.
- Review authentication/register/login rate limits and account-enumeration behavior.
- Review request logging and exception handling for secret leakage.

## Acceptance Criteria

- [x] CORS allows only documented origins/methods/headers required by the product; wildcard+credentials misconfiguration is impossible.
- [x] CSRF risk is explicitly addressed for the selected browser credential transport.
- [x] Auth cookies/tokens use the security attributes defined by the ADR.
- [x] Logout/revocation semantics remain enforceable server-side where designed.
- [x] Passwords, authorization headers, refresh/session tokens, API-key secrets, and verification secrets are excluded from logs.
- [x] HTTPS/reverse-proxy scheme handling does not generate insecure canonical URLs in production configuration.
- [x] Security headers are applied where compatible with Angular/API behavior and documented.
- [x] Auth errors do not expose stack traces or credential-validation internals.
- [x] Manual security configuration review is recorded.
- [x] No automated test files are added.

## Verification

Inspect browser/API response headers, credential transport, cross-origin behavior, CSRF-relevant flows, logs during auth failures, and logout/revocation using the documented deployment topology.

## Implementation Summary

- Replaced permissive credentialed CORS method/header behavior with the exact Angular/API contract,
  startup-validated unique origins, HTTPS-only deployed origins, and an explicit `Retry-After`
  exposure.
- Preserved the ADR-selected bearer-plus-rotating-refresh-cookie model and documented the layered
  exact-origin, antiforgery-cookie, antiforgery-header, strict-same-site, server-side family
  revocation, and short-lived access-token behavior.
- Added common API security headers, no-store handling for every auth response, Kestrel server-header
  suppression, production HSTS/HTTPS redirection, and Development-only Swagger exposure.
- Added trusted `X-Forwarded-Proto` processing beside client IP forwarding, kept forwarded host
  untrusted, and made production canonical URLs and custom-domain schemes fail closed to HTTPS.
- Kept request logs free of bodies and credential headers and suppressed unexpected exception
  details while retaining exception type and trace ID for coarse diagnosis.
- Recorded the configuration matrix and manual review in
  [HTTP, Authentication, and Session Security](../http-auth-security.md).

## Verification Results

- `dotnet build UrlShortener.sln --no-restore` completed with zero warnings and zero errors.
- A live Development API returned the expected no-store/security-header set and no `Server` header
  on auth errors. Its public `500` envelope contained no exception details or stack trace.
- Trusted-origin preflight returned only the documented origin, credentials, methods, and headers;
  the untrusted-origin preflight returned no CORS authorization headers.
- Production startup validation rejected HTTP browser-origin and canonical-base configuration.
- Development Swagger returned `200` with the compatible non-CSP security headers. No automated
  test files were added.
