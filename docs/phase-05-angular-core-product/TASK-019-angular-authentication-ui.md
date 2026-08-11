# TASK-019 — Angular Authentication Experience

**Status:** Planned  
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

- [ ] Unauthenticated access to protected `/app/*` routes follows the approved redirect/guard behavior.
- [ ] Successful authentication enters the protected application shell and loads safe current-user metadata.
- [ ] Invalid credentials display the approved generic failure without exposing account existence.
- [ ] Registration validation maps field errors correctly when registration exists.
- [ ] Sign-out clears client auth state and server-side session/token state as defined by Phase 02.
- [ ] Expired/revoked authentication returns the user to a recoverable signed-out state.
- [ ] Authentication secrets are not written to console/log UI or insecure storage contrary to the ADR.
- [ ] Forms have labels, keyboard submission, loading/disabled state, and visible error feedback.
- [ ] Production Angular and backend builds succeed.
- [ ] Automated test files remain deferred to Phase 16.

## Verification

Manually verify register/sign-in/session refresh-or-bootstrap/sign-out plus invalid and expired-session flows using the real backend.