# TASK-019 — Angular Authentication Experience

**Status:** Completed
**Phase:** 05 — Angular Auth, Dashboard & Link Management

## Goal

Deliver secure and usable Angular sign-in/registration/session flows that follow the backend authentication ADR exactly.

## Dependencies

- Phase 04 completed.

## Scope

- Build login and registration screens when registration is enabled.
- Integrate current-user/session bootstrap.
- Implement protected-route guard behavior.
- Implement sign-out.
- Handle expired/revoked sessions without loops or stale protected UI.
- Preserve intended return URL after authentication where safe.
- Surface validation and generic credential errors according to backend policy.

## Acceptance Criteria

- [x] Unauthenticated access to protected `/app/*` routes follows the approved redirect/guard behavior.
- [x] Successful authentication enters the protected application shell and loads safe current-user metadata.
- [x] Invalid credentials display the approved generic failure without exposing account existence.
- [x] Registration validation maps field errors correctly when registration exists.
- [x] Sign-out clears client auth state and server-side session/token state as defined by Phase 02.
- [x] Expired/revoked authentication returns the user to a recoverable signed-out state.
- [x] Authentication secrets are not written to console/log UI or insecure storage contrary to the ADR.
- [x] Forms have labels, keyboard submission, loading/disabled state, and visible error feedback.
- [x] Production Angular and backend builds succeed.
- [x] Automated test files remain deferred to Phase 16.

## Verification

Manually verify register/sign-in/session refresh-or-bootstrap/sign-out plus invalid and expired-session flows using the real backend.

## Completion Notes

- Replaced the authentication placeholder with responsive sign-in and conditional-registration
  experiences using labeled reactive forms, keyboard submission, loading/disabled controls,
  client/server field validation, generic credential/account errors, and retry guidance.
- Added a guarded `/app/*` session bootstrap that obtains a fresh antiforgery pair, rotates the
  HttpOnly refresh session, reconciles `/auth/me`, and preserves only safe internal return URLs.
  Access and CSRF tokens remain in memory; refresh credentials remain HttpOnly.
- Added the safe `GET /api/v1/auth/bootstrap` contract so a fresh Angular process can refresh
  without persisting the antiforgery request token. It also exposes only public registration and
  password-policy metadata needed by the form.
- Added application-shell current-user metadata, explicit sign-out, and a single recoverable route
  transition when an access or refresh session becomes invalid.
- Browser-checked on 2026-08-12 at 1440×900 and 390×844: layout had no horizontal overflow, labels
  and inline alerts were exposed, native form submission was present, and the console had no errors.
- Verified against the real LocalDB-backed API on 2026-08-12: bootstrap `200`, registration `201`,
  current user `200`, refresh `200`, unknown-account sign-in returned generic
  `401 AUTHENTICATION_FAILED`, sign-out returned `204`, and post-sign-out refresh returned `401`.
  The disposable verification account is `task019-55670eeba5@example.invalid`.
- Verified Angular formatting, lint, and production build plus the Release backend build. No
  automated test files were added; that setup remains owned by Phase 16.
