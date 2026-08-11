# TASK-039 — Angular API Key Management and Developer Guidance

**Status:** Planned  
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

- [ ] Existing key list never displays full plaintext secrets.
- [ ] New secret is displayed only from the immediate creation/rotation response.
- [ ] User receives a clear warning that the secret cannot be recovered later.
- [ ] Scope selection describes effective capability clearly.
- [ ] Revoke action requires confirmation and updates UI status immediately after success.
- [ ] Expired/revoked keys are visually distinct.
- [ ] Usage example places the newly created key only in the transient one-time screen.
- [ ] Copy actions provide feedback without logging the key.
- [ ] Mobile and keyboard workflows are usable.
- [ ] Production Angular build succeeds.
- [ ] No automated test files are added.

## Phase 10 Completion Gate

Phase 10 is complete when TASK-037 through TASK-039 are completed and a user can securely create, scope, use, inspect metadata for, and revoke API keys through the API and Angular.