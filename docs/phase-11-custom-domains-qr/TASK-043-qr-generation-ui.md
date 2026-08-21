# TASK-043 — QR Code Generation and Angular Workflow

**Status:** Completed
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

- [x] QR encodes the exact canonical short URL returned by the backend.
- [x] QR generation requires ownership for management/download endpoint even though the encoded redirect remains public.
- [x] Unsupported size/format/customization values are validated and bounded.
- [x] Response content type and filename are correct for download.
- [x] QR generation does not accept an arbitrary URL payload that bypasses link ownership.
- [x] Angular preview handles loading/failure and provides an accessible textual link alongside the image.
- [x] Custom-domain links encode the correct verified domain.
- [x] Backend and production Angular builds succeed.
- [x] No automated test files are added.

## Phase 11 Completion Gate

Phase 11 is complete when TASK-040 through TASK-043 are completed and users can verify/manage custom domains, create branded links where deployment supports them, and generate/download QR codes for owned canonical short URLs.

## Implementation Notes

- Added the owner-scoped `GET /api/v1/short-urls/{shortCode}/qr-code` endpoint. It resolves the
  canonical URL through the existing link service, then generates the SVG in memory.
- The endpoint accepts bounded size, error-correction, foreground, and background options. SVG is
  the only supported format; unknown options, including a caller-supplied URL, are rejected.
- The Angular details page loads an SVG blob preview, exposes retry and download states, revokes
  object URLs, and shows the canonical URL as an accessible text link.
- See `docs/qr-code-generation.md` for the HTTP contract and operational behavior.
