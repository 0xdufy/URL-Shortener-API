# TASK-058 — Deployment and Database Migration Runbook

**Status:** Planned  
**Phase:** 15 — Containerization & Deployment Baseline

## Goal

Define a repeatable deployment procedure that applies database migrations intentionally, starts compatible API/worker/web versions, and supports diagnosis/rollback without relying on undocumented manual steps.

## Dependencies

- TASK-057 completed.

## Scope

- Define image/version promotion flow for API, worker, and Angular artifacts.
- Define when/how EF migrations are applied and which process/account is authorized to apply them.
- Define backward-compatible deployment expectations when API and worker versions briefly overlap.
- Document configuration/secrets required per environment.
- Define rollback limitations when a schema migration is not backward reversible.
- Document post-deploy health/readiness and smoke checks.

## Acceptance Criteria

- [ ] Production-like startup does not rely on every API instance racing to apply migrations automatically unless an explicit safe strategy is approved.
- [ ] Migration command/process is documented and fails deployment visibly on error.
- [ ] API/worker event-contract compatibility during rolling deployment is documented.
- [ ] Required secrets/configuration are listed by name/purpose without embedding values.
- [ ] Rollback procedure distinguishes application rollback from database rollback.
- [ ] Post-deploy checks cover health/readiness, authentication, URL creation, redirect, worker processing, and Angular availability.
- [ ] Deployment notes include Redis/queue dependency prerequisites and custom-domain/TLS responsibilities.
- [ ] Runbook can be followed from a fresh environment without relying on a developer's machine state.
- [ ] No automated test files are added.

## Phase 15 Completion Gate

Phase 15 is complete when TASK-055 through TASK-058 are completed and the full platform has reproducible images, Compose/local orchestration, correct edge routing, and an explicit migration/deployment procedure.