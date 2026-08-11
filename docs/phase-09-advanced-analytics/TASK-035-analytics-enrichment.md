# TASK-035 — Referrer and User-Agent Analytics Enrichment

**Status:** Planned  
**Phase:** 09 — Advanced Analytics & Analytics UI

## Goal

Convert raw event metadata into stable analytics dimensions without allowing parsing failures or high-cardinality strings to degrade the analytics pipeline.

## Dependencies

- TASK-034 completed.

## Scope

- Normalize referrers into `Direct`, known host/source, or `Other/Unknown` categories according to documented rules.
- Parse user-agent metadata into bounded browser family, OS family, and device-class categories.
- Decide whether enrichment occurs in the worker or another deterministic processing boundary.
- Bound stored/displayed source labels and reject/control malformed oversized metadata.
- If geo enrichment is pursued, create a separate ADR covering provider, accuracy, update process, privacy, and licensing; otherwise explicitly leave geography unsupported.

## Acceptance Criteria

- [ ] Malformed or unknown user agents do not fail event processing.
- [ ] High-cardinality full user-agent strings are not used directly as dashboard dimension keys.
- [ ] Referrer normalization strips unnecessary path/query detail unless a product requirement explicitly needs it.
- [ ] Direct/no-referrer traffic is represented consistently.
- [ ] Browser/OS/device categories are deterministic for a given parser/version.
- [ ] Parser/library choice and update implications are documented.
- [ ] Geographic analytics are not claimed unless an explicit implementation/ADR exists.
- [ ] Worker/backend build succeeds and representative metadata is manually processed.
- [ ] No automated test files are added.

## Verification

Process representative desktop/mobile/bot/unknown user agents plus direct, known-host, malformed, and oversized referrer values and inspect aggregate categories.