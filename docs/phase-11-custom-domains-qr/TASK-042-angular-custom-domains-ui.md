# TASK-042 — Angular Custom Domain Management

**Status:** Completed

**Phase:** 11 — Custom Domains & QR Codes

## Goal

Provide a clear Angular workflow for registering domains, following verification instructions, checking status, and selecting verified domains during link creation/editing.

## Dependencies

- TASK-041 completed.

## Scope

- Build `/app/domains` page.
- Register domain and display normalized value before confirmation when useful.
- Show DNS verification record/instructions from backend contract.
- Add explicit re-check verification action and status display.
- Support disable/remove behavior according to backend lifecycle rules.
- Add verified-domain selector to applicable link creation/edit forms.

## Acceptance Criteria

- [x] UI clearly distinguishes pending, verified, failed/invalid, and disabled states.
- [x] Verification record/token can be copied with feedback.
- [x] UI does not claim verification success until backend confirms it.
- [x] Unverified/disabled domains cannot be selected for link creation.
- [x] DNS guidance explains propagation can delay verification without promising a fixed time.
- [x] Domain errors are translated through shared API/error handling.
- [x] Removing/disabling a domain requires confirmation when it can affect active links.
- [x] Responsive and keyboard workflows remain usable.
- [x] Production Angular build succeeds.
- [x] No automated test files are added.

## Verification

Exercise registration, pending state, failed check, successful verification, link selection, and domain disable/removal using the backend's documented verification environment.

## Implementation and Verification Notes

- 2026-08-20: Replaced the `/app/domains` placeholder with a lazy-loaded, typed management page
  covering normalized-host preview, registration, owner-scoped claim listing, exact TXT instructions,
  token replacement, explicit verification checks, failure-specific DNS guidance, and retained-claim
  disable/re-verification behavior.
- Pending, verified, failed, and disabled resources have separate text, badge, border, guidance, and
  action treatments. Clipboard actions cover individual record fields and the complete record, use
  toast feedback, and do not treat a copied or published value as proof of ownership.
- Disable uses the shared native confirmation dialog and warns that assigned branded links stop
  resolving immediately. Disabled claims cannot be checked directly; starting verification rotates
  the token as required by the backend lifecycle.
- Create and edit link forms now load custom-domain eligibility through the typed domain client and
  submit `customDomainId`. Only resources whose backend representation is both `verified` and
  `canServeBrandedLinks` appear as selectable options. An existing assignment that becomes
  unavailable is rendered as an invalid disabled choice that requires an explicit platform or
  verified-domain selection.
- Responsive layouts, semantic forms/fieldsets/status regions, native select controls, focused
  validation feedback, and the shared keyboard-contained confirmation dialog cover mobile and
  keyboard workflows. DNS copy explains that propagation can delay visibility and separately calls
  out traffic routing and TLS requirements.
- `npm run lint`, Prettier verification, strict TypeScript compilation, and the production
  `npm run build` succeeded. The build reports component-style budget warnings below the configured
  error threshold. No automated test file was added.
