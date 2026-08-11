# TASK-017 — Angular Design System and Application Shell

**Status:** Planned  
**Phase:** 04 — Angular Foundation & Design System

## Goal

Create a consistent, responsive UI foundation that later features can reuse rather than implementing page-specific visual patterns repeatedly.

## Dependencies

- TASK-016 completed.

## Scope

- Define typography, spacing, form, button, table, badge, dialog, notification, and layout conventions.
- Select an Angular-compatible UI/component approach and document why it was chosen; avoid adding multiple overlapping component libraries.
- Implement the responsive application shell: top bar/side navigation as appropriate, content area, mobile behavior, page header pattern, and account/navigation placeholders.
- Implement reusable loading, empty, error, confirmation, and toast/feedback patterns.
- Define accessible focus states and keyboard behavior.
- Establish light/dark theming only if it can be maintained consistently; it is not mandatory.

## Acceptance Criteria

- [ ] Application shell works at representative desktop, tablet, and mobile widths.
- [ ] Reusable controls follow one visual/interaction system.
- [ ] Navigation structure includes placeholders for Dashboard, Links, Analytics-context pages, API Keys, Domains, and Account without implementing those features prematurely.
- [ ] Forms expose labels, validation-message slots, disabled/loading states, and keyboard focus behavior.
- [ ] Confirmation patterns exist for destructive actions.
- [ ] Empty/error/loading patterns can be reused by later feature pages.
- [ ] UI does not rely on color alone to communicate important status.
- [ ] No hardcoded business/API data is required for the shell to build.
- [ ] Production Angular build succeeds.
- [ ] Automated test files remain deferred to Phase 16.

## Verification

Run the Angular app and manually inspect keyboard navigation, focus visibility, shell responsiveness, reusable form controls, loading state, empty state, and destructive confirmation pattern.