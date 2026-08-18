# TASK-036 — Angular Analytics Dashboard

**Status:** Completed
**Phase:** 09 — Advanced Analytics & Analytics UI

## Goal

Turn the analytics API into a clear, responsive link-analytics experience that communicates trends and limitations without overstating data precision.

## Dependencies

- TASK-035 completed.

## Scope

- Build `/app/links/:shortCode/analytics` or approved equivalent.
- Add range selection using only ranges supported by the API.
- Show total clicks, unique-visitor estimate if implemented, trend chart, top referrers/sources, device, browser, and OS breakdowns.
- Show analytics freshness/eventual-consistency notice where appropriate.
- Implement loading, empty, partial, error, unauthorized, and rate-limited states.
- Select one charting approach and avoid redundant chart libraries.

## Acceptance Criteria

- [x] Analytics page cannot display data for links the user cannot access.
- [x] Range changes re-query the backend and do not fabricate client-side totals.
- [x] Charts have text labels/tooltips and remain understandable without relying on color alone.
- [x] Empty analytics is presented as a valid state with guidance, not as an error.
- [x] Long referrer/source labels do not destroy layout.
- [x] Freshness/eventual-consistency expectations are visible where needed.
- [x] Mobile layout remains usable even when charts collapse to simpler representations.
- [x] No raw IP or unnecessary sensitive metadata is displayed.
- [x] Production Angular build succeeds.
- [x] No automated test files are added.

## Phase 09 Completion Gate

Phase 09 is complete when TASK-033 through TASK-036 are completed and users can inspect efficient, privacy-aware aggregate analytics through both API and Angular.

## Implementation and Verification Notes

- 2026-08-18: Replaced the per-link analytics placeholder with a lazy standalone page at
  `/app/links/:shortCode/analytics`. The typed client now exposes the owner-scoped summary and
  time-series routes without constructing API paths in feature code.
- Reporting presets cover 7, 30, 90, and 365 UTC days, all within the API's supported daily range.
  Every selection issues new summary and time-series requests and renders only server-provided
  totals and buckets.
- The page includes total clicks, the qualified daily unique-visitor estimate, a native SVG trend
  with visible axes and per-point tooltips, source/device/browser/OS bars with numeric labels,
  eventual-consistency metadata, privacy limitations, and a scrollable text-and-bar trend on small
  screens. Long source labels use bounded truncation with their full value available as a title.
- Loading, valid-empty, transient partial, general error, authentication, ownership-safe not-found,
  and rate-limited states are distinct. Any authentication, authorization, or not-found response
  discards the other request's result so an access race cannot leave aggregate data visible.
- `npm run lint`, targeted Prettier validation, `git diff --check`, and the production
  `npm run build` completed successfully. The build retains the workspace's existing component-style
  budget warnings and adds the analytics component warning at 6.19 kB, below the configured 8 kB
  error threshold. No chart dependency or automated test file was added.
