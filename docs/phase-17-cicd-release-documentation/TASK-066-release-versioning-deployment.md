# TASK-066 — Release Versioning and Deployment Automation

**Status:** Planned  
**Phase:** 17 — CI/CD, Release Engineering & Final Documentation

## Goal

Create a controlled release pipeline that versions artifacts consistently, publishes immutable images/builds, applies the Phase 15 deployment procedure, and prevents unreviewed revisions from becoming releases.

## Dependencies

- TASK-065 completed.

## Scope

- Choose and document semantic/versioning strategy for the repository and API compatibility.
- Build immutable versioned API/worker/web images or deployment artifacts from one source revision.
- Publish artifacts only after required Phase 16/17 quality gates pass.
- Define environment promotion and required approvals/secrets.
- Integrate the migration step from TASK-058 with explicit failure behavior.
- Add post-deploy smoke/health validation and stop/rollback behavior on failure.
- Generate release notes/change summary from reviewed source, not from untrusted raw commit text alone when that is a risk.

## Acceptance Criteria

- [ ] One release identifier maps unambiguously to API, worker, and Angular artifacts from the same compatible source revision.
- [ ] Release artifacts are immutable/versioned; deployment does not depend only on a mutable `latest` tag.
- [ ] Required test/build/security gates must pass before production-like release publication.
- [ ] Deployment credentials are stored in environment/CI secret management.
- [ ] Database migration failure prevents continuing with an incompatible application rollout.
- [ ] Post-deploy health and core smoke checks run automatically or through a documented required gate.
- [ ] Rollback behavior follows the Phase 15 migration limitations and does not promise automatic schema reversal when unsafe.
- [ ] Release workflow can also produce an artifact without automatically deploying when configured for manual promotion.

## Verification

Create a non-production release candidate, confirm artifact version alignment, migration gate, deployment, smoke checks, and documented rollback path.