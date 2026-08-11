# TASK-016 — Angular Workspace and Application Foundation

**Status:** Planned  
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

- [ ] Angular app installs and builds from a clean checkout using documented commands.
- [ ] TypeScript strictness is enabled unless a specific exception is documented.
- [ ] API base URL is centralized and environment-aware.
- [ ] No real secrets are stored in Angular environment files.
- [ ] Routing structure can support public auth pages and protected `/app/*` areas.
- [ ] Feature code is not placed indiscriminately in a single shared folder.
- [ ] Build output is ignored by Git.
- [ ] Backend and frontend development commands are documented.
- [ ] No backend contract is duplicated as handwritten business validation when it belongs server-side.
- [ ] Automated test-file creation remains deferred to Phase 16.

## Verification

Run install, lint/format check if configured, and production build. Start the development server and verify the shell loads using the documented API configuration strategy.