# TASK-016 — Angular Workspace and Application Foundation

**Status:** Completed
**Phase:** 04 — Angular Foundation & Design System

## Goal

Create a production-oriented Angular SPA foundation that can grow with the product without mixing generated boilerplate, API details, and feature logic.

## Dependencies

- Phase 03 completed.

## Scope

- Create the Angular workspace/application in the repository location approved by TASK-005.
- Select and pin the Angular/TypeScript/tooling versions used by the project.
- Configure strict TypeScript/Angular compiler settings appropriate for a maintained application.
- Define environment/runtime API base-URL strategy without hardcoding machine-local hosts into feature code.
- Establish application routing and lazy-loaded feature boundaries where useful.
- Establish formatting/linting commands consistent with `docs/engineering-standards.md`.
- Add a minimal application shell placeholder without implementing later feature pages.

## Acceptance Criteria

- [x] Angular app installs and builds from a clean checkout using documented commands.
- [x] TypeScript strictness is enabled unless a specific exception is documented.
- [x] API base URL is centralized and environment-aware.
- [x] No real secrets are stored in Angular environment files.
- [x] Routing structure can support public auth pages and protected `/app/*` areas.
- [x] Feature code is not placed indiscriminately in a single shared folder.
- [x] Build output is ignored by Git.
- [x] Backend and frontend development commands are documented.
- [x] No backend contract is duplicated as handwritten business validation when it belongs server-side.
- [x] Automated test-file creation remains deferred to Phase 16.

## Verification

Run install, lint/format check if configured, and production build. Start the development server and verify the shell loads using the documented API configuration strategy.

## Completion Notes

- Created the Angular workspace in `web/` with Angular 21.2.19, Angular CLI/build 21.2.20,
  TypeScript 5.9.3, standalone components, strict TypeScript/template compilation, SCSS, and exact
  package versions. Angular 21 was selected because it supports the repository machine's Node 24.13
  runtime; Angular 22.1 requires Node 24.15 or newer.
- Added ESLint 10 with Angular ESLint 21.4 and Prettier 3.8.1. `package.json` exposes `format`,
  `format:check`, `lint`, and production `build` scripts. Component/test schematics continue to skip
  test files until Phase 16 owns the test foundation.
- Added lazy `/auth/*` and `/app/*` route boundaries with minimal placeholders. The authentication
  guard and product/design-system pages remain intentionally deferred to their owning tasks.
- Centralized API-origin access behind the injectable `API_BASE_URL` token. Development uses
  `http://localhost:5034/api/v1`; production uses same-origin `/api/v1`. Environment files contain
  public configuration only, and the workspace guide documents secret handling and the future
  runtime-config option for deployment topologies that require it.
- Documented backend/frontend start, clean install, formatting, lint, and production-build commands
  in the root and Angular READMEs.
- Verified on 2026-08-12: `npm ci`, `npm run format:check`, `npm run lint`, and `npm run build` all
  succeeded. `npm audit --omit=dev` reported zero production dependency vulnerabilities. A temporary
  Angular development server returned `200` for `/`, `/app`, and `/auth`, and the root document
  contained the Angular application host element. The full audit reports five development-tool-only
  transitive findings in the current Angular CLI/build dependency graph; npm offers only incompatible
  Angular downgrades, so no forced override was applied. No automated test files were added.
