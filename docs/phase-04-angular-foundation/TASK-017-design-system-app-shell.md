# TASK-017 — Angular Design System and Application Shell

**Status:** Completed
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

- [x] Application shell works at representative desktop, tablet, and mobile widths.
- [x] Reusable controls follow one visual/interaction system.
- [x] Navigation structure includes placeholders for Dashboard, Links, Analytics-context pages, API Keys, Domains, and Account without implementing those features prematurely.
- [x] Forms expose labels, validation-message slots, disabled/loading states, and keyboard focus behavior.
- [x] Confirmation patterns exist for destructive actions.
- [x] Empty/error/loading patterns can be reused by later feature pages.
- [x] UI does not rely on color alone to communicate important status.
- [x] No hardcoded business/API data is required for the shell to build.
- [x] Production Angular build succeeds.
- [x] Automated test files remain deferred to Phase 16.

## Verification

Run the Angular app and manually inspect keyboard navigation, focus visibility, shell responsiveness, reusable form controls, loading state, empty state, and destructive confirmation pattern.

## Completion Notes

- Added a responsive application shell with desktop side navigation, tablet/mobile top bar and
  drawer, skip link, active-route feedback, Escape-close behavior, and placeholders for Dashboard,
  Links, Analytics, API Keys, Domains, and Account.
- Chose a small in-house standalone-component layer backed by CSS design tokens instead of a broad
  component library. The choice and extension policy are documented in `web/README.md`.
- Added shared button, field, badge, icon, page-header, loading/empty/error state, native confirmation
  dialog, and toast feedback primitives. Status patterns include text or symbols in addition to
  color, and global controls expose consistent visible keyboard focus.
- Added a foundation-only dashboard preview that exercises form labels, help/error slots,
  disabled/loading controls, empty and table conventions, destructive confirmation, and live toast
  feedback without loading or inventing business/API data. Later feature routes remain placeholders.
- Verified on 2026-08-12: Prettier check, Angular ESLint, and the production Angular build succeeded.
  Browser inspection at 1440x900, 800x900, and 390x844 confirmed responsive layout with no horizontal
  overflow. Mobile navigation opened and closed with Escape, navigation reached its placeholder
  route, the modal initially focused its safe Cancel action, confirmation produced an announced
  toast, and the browser reported no console warnings or errors. No automated test files were added.
