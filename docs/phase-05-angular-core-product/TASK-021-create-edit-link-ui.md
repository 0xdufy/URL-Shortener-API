# TASK-021 — Create and Edit Link UI

**Status:** Planned  
**Phase:** 05 — Angular Auth, Dashboard & Link Management

## Goal

Deliver complete Angular forms for creating and editing owned short links using backend validation as the authoritative contract.

## Dependencies

- TASK-020 completed.

## Scope

- Build create-link workflow for destination URL, optional custom alias, and optional expiry.
- Build edit workflow for fields declared mutable by Phase 03.
- Provide clear generated-vs-custom alias behavior.
- Map backend field-validation and alias-conflict responses to actionable form feedback.
- Show created short URL with copy/navigation actions after success.
- Prevent accidental duplicate submissions while a request is in flight.

## Acceptance Criteria

- [ ] Create form supports all Phase 03 creation fields and no unauthorized/system fields.
- [ ] Edit form exposes only fields the API declares mutable.
- [ ] Invalid URL, alias, expiry, conflict, rate-limit, and unexpected errors have distinct understandable feedback.
- [ ] Client-side validation improves UX but does not replace server validation.
- [ ] Submit controls show pending state and prevent accidental repeat submission.
- [ ] Successful create/edit updates relevant cached/view state without requiring a full browser reload.
- [ ] Created short URL can be copied and opened.
- [ ] Form remains keyboard accessible and responsive.
- [ ] Production Angular build succeeds.
- [ ] No automated test files are added.

## Verification

Manually exercise generated code, custom alias, duplicate alias, invalid destination, invalid expiry, successful edit, and server-side validation mismatch scenarios.