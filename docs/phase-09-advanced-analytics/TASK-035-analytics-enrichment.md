# TASK-035 — Referrer and User-Agent Analytics Enrichment

**Status:** Completed
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

- [x] Malformed or unknown user agents do not fail event processing.
- [x] High-cardinality full user-agent strings are not used directly as dashboard dimension keys.
- [x] Referrer normalization strips unnecessary path/query detail unless a product requirement explicitly needs it.
- [x] Direct/no-referrer traffic is represented consistently.
- [x] Browser/OS/device categories are deterministic for a given parser/version.
- [x] Parser/library choice and update implications are documented.
- [x] Geographic analytics are not claimed unless an explicit implementation/ADR exists.
- [x] Worker/backend build succeeds and representative metadata is manually processed.
- [x] No automated test files are added.

## Verification

Process representative desktop/mobile/bot/unknown user agents plus direct, known-host, malformed, and oversized referrer values and inspect aggregate categories.

## Implementation and Verification Notes

- 2026-08-18: Added backward-compatible `referrerKind` metadata so the publisher and worker retain
  the distinction between direct and rejected referrers without carrying the rejected value.
  Valid HTTP(S) referrers remain host-only; the version-2 worker classifier maps recognized domain
  boundaries to ten fixed source labels and all other valid hosts to `Other`.
- The repository-owned user-agent signature table remains intentionally small and deterministic.
  Raw values are capped at 256 characters and can be retained only in the bounded access-log
  window; device/browser/OS aggregate keys are fixed categories. Parser and source-map changes
  require a new dimension schema version and explicit migration/cutover. Geography is explicitly
  unsupported pending a dedicated ADR.
- Migration `AddAnalyticsEnrichmentV2` backfilled access-log referrer kinds, copied version-1
  totals and bounded UA dimensions, and coalesced historical referrer hosts into version-2 labels.
  A disposable LocalDB upgrade preserved `10` clicks and `7` daily uniques while producing
  `Google=5`, `Other=4`, and `Direct=1`; the verification database was removed.
- Manual classifier processing covered Windows Chrome desktop, iOS Safari mobile, Android Chrome
  mobile, a bot, an unsupported client, control-character metadata, direct, known, unrecognized,
  malformed, and oversized referrers. Results stayed within the documented categories and no
  parser exception occurred.
- `dotnet build UrlShortener.sln --no-restore` succeeded with zero warnings and errors. No
  automated test files were added.
