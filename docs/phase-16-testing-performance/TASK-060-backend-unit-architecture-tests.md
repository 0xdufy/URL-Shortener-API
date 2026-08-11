# TASK-060 — Backend Unit and Architecture Test Coverage

**Status:** Planned  
**Phase:** 16 — Automated Testing & Performance Validation

## Goal

Cover deterministic business/application behavior and enforce the architectural boundaries established in Phase 01.

## Dependencies

- TASK-059 completed.

## Scope

Add focused unit coverage for at least:

- Short-code generation constraints/collision policy boundaries.
- URL/alias/expiry validation.
- Link state transitions and authorization-relevant application behavior.
- Cache TTL/state evaluation helpers.
- Analytics categorization/aggregation logic.
- API-key scope/expiry/revocation logic.
- Privacy identifier/enrichment helpers.
- Retention eligibility calculations.

Add architecture tests enforcing approved project/layer dependency rules.

## Acceptance Criteria

- [ ] Unit tests target deterministic logic and avoid unnecessary mocking of framework internals.
- [ ] Critical validation and state-transition edge cases have explicit coverage.
- [ ] Authorization/business rules are covered at the application/domain level where they live.
- [ ] Analytics retry/idempotency helper logic has deterministic coverage where unit-level testing is appropriate.
- [ ] Architecture tests fail if Domain/Application acquire prohibited references under the Phase 01 ADR.
- [ ] Tests do not duplicate integration scenarios simply to inflate test count.
- [ ] Test names describe behavior/expected outcome clearly.
- [ ] Unit/architecture suite runs without Docker dependencies.
- [ ] All new tests pass consistently in repeated local runs.

## Verification

Run the fast backend test command multiple times and record total tests, duration, and any deliberately uncovered behavior reserved for integration/E2E.