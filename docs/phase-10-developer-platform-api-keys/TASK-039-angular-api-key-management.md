# TASK-039 — Angular API Key Management and Developer Guidance

**Status:** Completed
**Phase:** 10 — Developer Platform & API Keys

## Goal

Provide a safe Angular workflow for creating, viewing metadata, rotating/revoking API keys, and understanding how to use them.

## Dependencies

- TASK-038 completed.

## Scope

- Build `/app/api-keys` management page.
- List key name/prefix, scopes, created/expiry/last-used metadata, and status without exposing secret material.
- Create-key flow with scope selection and one-time plaintext-secret reveal.
- Require explicit acknowledgement before dismissing the one-time secret where practical.
- Add revoke and approved rotation flow with destructive confirmation.
- Add concise copyable API usage examples that never embed another stored secret later.

## Acceptance Criteria

- [x] Existing key list never displays full plaintext secrets.
- [x] New secret is displayed only from the immediate creation/rotation response.
- [x] User receives a clear warning that the secret cannot be recovered later.
- [x] Scope selection describes effective capability clearly.
- [x] Revoke action requires confirmation and updates UI status immediately after success.
- [x] Expired/revoked keys are visually distinct.
- [x] Usage example places the newly created key only in the transient one-time screen.
- [x] Copy actions provide feedback without logging the key.
- [x] Mobile and keyboard workflows are usable.
- [x] Production Angular build succeeds.
- [x] No automated test files are added.

## Phase 10 Completion Gate

Phase 10 is complete when TASK-037 through TASK-039 are completed and a user can securely create, scope, use, inspect metadata for, and revoke API keys through the API and Angular.

## Implementation and Verification Notes

- 2026-08-19: Replaced the `/app/api-keys` placeholder with a lazy-loaded, responsive management
  page backed by a typed API client. The list exposes only safe metadata: name, public prefix,
  scopes, state, creation, expiry, last-used, and revocation timestamps.
- The creation form validates names and future optional expiry, explains each supported scope in
  capability terms, and requires at least one scope. Successful creation and approved rotation move
  into a transient one-time screen containing the response credential and a credential-bearing cURL
  example. The screen warns that recovery is impossible, warns before browser unload, and requires
  an explicit saved-key acknowledgement before its own dismiss action clears the response state.
- The persistent developer example uses only `$SHORTLY_API_KEY`. Clipboard actions produce toast
  feedback and never pass credentials to logging. Revoke and rotation use destructive confirmation;
  successful actions immediately replace safe list state, with expired and revoked entries receiving
  distinct state treatments.
- Keyboard-visible native controls, semantic fieldsets/dialogs/status regions, focused error and
  reveal actions, and single-column responsive layouts cover keyboard and mobile workflows.
- `npm run lint`, Angular template compilation, Prettier verification, and the production
  `npm run build` succeeded. The build reports only component-style budget warnings below the
  configured error threshold. No automated test file was added.
