# TASK-055 — Production-Oriented Container Images

**Status:** Planned  
**Phase:** 15 — Containerization & Deployment Baseline

## Goal

Create reproducible, minimal container images for the ASP.NET Core API, analytics/maintenance worker, and Angular web application or approved static-hosting image.

## Dependencies

- Phase 14 completed.

## Scope

- Add multi-stage Dockerfiles for backend hosts and Angular build/serve strategy.
- Run application processes as non-root where practical.
- Keep SDK/build tooling out of final runtime images.
- Add `.dockerignore` appropriate to repository layout.
- Define environment-variable configuration rather than baking secrets into images.
- Add image labels/version metadata where useful.

## Acceptance Criteria

- [ ] API image builds from a clean repository checkout.
- [ ] Worker image builds independently and starts the intended worker host.
- [ ] Angular production image/build serves the SPA and supports client-side routing according to the chosen hosting topology.
- [ ] Final images do not contain source-control metadata, local secrets, `obj/bin` trees beyond required published output, or unnecessary SDK tooling.
- [ ] Processes do not require root privileges unless a documented platform constraint exists.
- [ ] Health endpoint/probe commands can target the appropriate host.
- [ ] Configuration and credentials are injected at runtime.
- [ ] Image build commands and expected ports are documented.
- [ ] No automated test files are added.

## Verification

Build all images locally, inspect final image contents/configuration at a high level, and run each image with minimal required environment configuration.