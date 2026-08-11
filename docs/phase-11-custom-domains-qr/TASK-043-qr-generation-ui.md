# TASK-043 — QR Code Generation and Angular Workflow

**Status:** Planned  
**Phase:** 11 — Custom Domains & QR Codes

## Goal

Generate QR codes from the canonical owned short URL and expose a simple Angular preview/download workflow without introducing a second source of link truth.

## Dependencies

- TASK-042 completed.

## Scope

- Add backend QR endpoint/service for an owned link using the final canonical short URL, including verified custom domain when selected.
- Support at least one raster/vector format selected for maintainability; PNG and SVG are preferred candidates.
- Bound image size/error-correction/customization options to prevent resource abuse.
- Add QR preview and download controls to the Angular link details page.
- Do not store generated QR binary data permanently unless an explicit caching/storage benefit is demonstrated.

## Acceptance Criteria

- [ ] QR encodes the exact canonical short URL returned by the backend.
- [ ] QR generation requires ownership for management/download endpoint even though the encoded redirect remains public.
- [ ] Unsupported size/format/customization values are validated and bounded.
- [ ] Response content type and filename are correct for download.
- [ ] QR generation does not accept an arbitrary URL payload that bypasses link ownership.
- [ ] Angular preview handles loading/failure and provides an accessible textual link alongside the image.
- [ ] Custom-domain links encode the correct verified domain.
- [ ] Backend and production Angular builds succeed.
- [ ] No automated test files are added.

## Phase 11 Completion Gate

Phase 11 is complete when TASK-040 through TASK-043 are completed and users can verify/manage custom domains, create branded links where deployment supports them, and generate/download QR codes for owned canonical short URLs.