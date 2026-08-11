# TASK-044 — HTTP, Authentication, and Session Security Hardening

**Status:** Planned  
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

- [ ] CORS allows only documented origins/methods/headers required by the product; wildcard+credentials misconfiguration is impossible.
- [ ] CSRF risk is explicitly addressed for the selected browser credential transport.
- [ ] Auth cookies/tokens use the security attributes defined by the ADR.
- [ ] Logout/revocation semantics remain enforceable server-side where designed.
- [ ] Passwords, authorization headers, refresh/session tokens, API-key secrets, and verification secrets are excluded from logs.
- [ ] HTTPS/reverse-proxy scheme handling does not generate insecure canonical URLs in production configuration.
- [ ] Security headers are applied where compatible with Angular/API behavior and documented.
- [ ] Auth errors do not expose stack traces or credential-validation internals.
- [ ] Manual security configuration review is recorded.
- [ ] No automated test files are added.

## Verification

Inspect browser/API response headers, credential transport, cross-origin behavior, CSRF-relevant flows, logs during auth failures, and logout/revocation using the documented deployment topology.