# TASK-062 — Angular Component/Service Tests and End-to-End Workflows

**Status:** Planned  
**Phase:** 16 — Automated Testing & Performance Validation

## Goal

Test high-value Angular behavior and critical browser workflows against the real application stack without attempting to unit-test every template detail.

## Dependencies

- TASK-061 completed.

## Scope

Add focused Angular tests for shared API/error/auth state logic and complex reusable components where unit/component coverage adds value. Add browser E2E coverage for critical product workflows using Playwright or the approved equivalent.

Critical E2E flows:

- Register/sign in/sign out/session expiry recovery.
- Create link with generated code and custom alias conflict handling.
- Search/filter/paginate link list.
- Edit/activate/deactivate/delete/restore lifecycle.
- Analytics page loading/range/empty state.
- API-key creation one-time secret and revocation.
- Custom-domain status flow using a controlled verification fixture/environment.
- QR preview/download availability.
- Two-user isolation from the UI and direct-navigation perspective.

## Acceptance Criteria

- [ ] E2E tests run against the documented full application environment, not mocked static pages for critical workflows.
- [ ] Selectors use stable accessibility/test identifiers rather than brittle CSS structure where practical.
- [ ] Auth/session setup avoids sharing state between tests unintentionally.
- [ ] Critical loading/error/empty/rate-limited states receive targeted coverage where deterministic fixtures permit.
- [ ] One-time API-key secret behavior is verified without persisting the secret in test logs/artifacts unnecessarily.
- [ ] Cross-user direct navigation cannot expose protected page data.
- [ ] Responsive smoke coverage includes at least one mobile viewport for core flows.
- [ ] Failed E2E tests collect bounded diagnostic artifacts without leaking real secrets.
- [ ] Angular unit/component and E2E suites pass repeatedly.

## Verification

Run the Angular fast tests and full browser E2E suite from a clean test environment and record duration, browser(s), and any intentionally excluded noncritical visual behavior.