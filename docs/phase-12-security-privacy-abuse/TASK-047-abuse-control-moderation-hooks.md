# TASK-047 — Abuse Controls and Moderation Hooks

**Status:** Planned  
**Phase:** 12 — Security, Privacy & Abuse Hardening

## Goal

Add bounded, auditable controls for obvious abuse cases without claiming the service can perfectly detect phishing or malicious destinations.

## Dependencies

- TASK-046 completed.

## Scope

- Define blocked destination domains/hosts and reserved patterns through operator-controlled configuration or persistence.
- Add an optional authenticated/public report-link workflow if it can be moderated meaningfully.
- Add link moderation state capable of disabling redirect without deleting owner data.
- Define owner/account suspension interaction with existing links if account status supports it.
- Log moderation actions without logging sensitive credentials.
- Keep external reputation/scanning integrations out of scope unless separately approved with SSRF, privacy, cost, and reliability considerations.

## Acceptance Criteria

- [ ] Blocked destination policy has one normalized matching strategy and cannot be bypassed trivially through hostname case/trailing-dot normalization.
- [ ] Moderation can disable a link's redirect and invalidates distributed cache state.
- [ ] Moderation status/reason exposure to owners follows a documented policy and does not leak internal security details unnecessarily.
- [ ] Report endpoint, if implemented, is rate limited and cannot create unbounded spam records.
- [ ] Operator/moderation actions are auditable.
- [ ] Existing ownership rules remain intact; moderation is not implemented by transferring ownership.
- [ ] The documentation explicitly distinguishes preventive controls from guaranteed malicious-link detection.
- [ ] Backend/Angular behavior for a moderated link is clear and manually verified.
- [ ] No automated test files are added.

## Phase 12 Completion Gate

Phase 12 is complete when TASK-044 through TASK-047 are completed and authentication/HTTP controls, input boundaries, analytics privacy, and abuse/moderation behavior are explicitly documented and enforced.