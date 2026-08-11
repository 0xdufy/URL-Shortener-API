# TASK-042 — Angular Custom Domain Management

**Status:** Planned  
**Phase:** 11 — Custom Domains & QR Codes

## Goal

Provide a clear Angular workflow for registering domains, following verification instructions, checking status, and selecting verified domains during link creation/editing.

## Dependencies

- TASK-041 completed.

## Scope

- Build `/app/domains` page.
- Register domain and display normalized value before confirmation when useful.
- Show DNS verification record/instructions from backend contract.
- Add explicit re-check verification action and status display.
- Support disable/remove behavior according to backend lifecycle rules.
- Add verified-domain selector to applicable link creation/edit forms.

## Acceptance Criteria

- [ ] UI clearly distinguishes pending, verified, failed/invalid, and disabled states.
- [ ] Verification record/token can be copied with feedback.
- [ ] UI does not claim verification success until backend confirms it.
- [ ] Unverified/disabled domains cannot be selected for link creation.
- [ ] DNS guidance explains propagation can delay verification without promising a fixed time.
- [ ] Domain errors are translated through shared API/error handling.
- [ ] Removing/disabling a domain requires confirmation when it can affect active links.
- [ ] Responsive and keyboard workflows remain usable.
- [ ] Production Angular build succeeds.
- [ ] No automated test files are added.

## Verification

Exercise registration, pending state, failed check, successful verification, link selection, and domain disable/removal using the backend's documented verification environment.