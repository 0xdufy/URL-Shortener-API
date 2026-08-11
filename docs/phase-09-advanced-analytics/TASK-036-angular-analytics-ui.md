# TASK-036 — Angular Analytics Dashboard

**Status:** Planned  
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

- [ ] Analytics page cannot display data for links the user cannot access.
- [ ] Range changes re-query the backend and do not fabricate client-side totals.
- [ ] Charts have text labels/tooltips and remain understandable without relying on color alone.
- [ ] Empty analytics is presented as a valid state with guidance, not as an error.
- [ ] Long referrer/source labels do not destroy layout.
- [ ] Freshness/eventual-consistency expectations are visible where needed.
- [ ] Mobile layout remains usable even when charts collapse to simpler representations.
- [ ] No raw IP or unnecessary sensitive metadata is displayed.
- [ ] Production Angular build succeeds.
- [ ] No automated test files are added.

## Phase 09 Completion Gate

Phase 09 is complete when TASK-033 through TASK-036 are completed and users can inspect efficient, privacy-aware aggregate analytics through both API and Angular.