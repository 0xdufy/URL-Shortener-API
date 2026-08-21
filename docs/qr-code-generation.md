# QR Code Generation

## Endpoint

`GET /api/v1/short-urls/{shortCode}/qr-code` generates an SVG QR code for an authenticated
owner's retained, non-deleted link. It requires the `shorturls:read` scope when API-key
authentication is used.

The endpoint never accepts a URL payload. It resolves the owned link through the same service as
the management detail endpoint and encodes that response's exact `shortUrl`. Platform links use
the configured public base URL; links assigned to a verified custom domain use their canonical
custom-domain URL.

## Query options

| Option | Default | Accepted values |
|---|---:|---|
| `size` | `320` | Integer from `128` through `1024` pixels |
| `format` | `svg` | `svg` |
| `errorCorrection` | `medium` | `low`, `medium`, `quartile`, `high` |
| `foreground` | `#111827` | Six-digit hexadecimal color |
| `background` | `#ffffff` | Six-digit hexadecimal color |

Foreground and background must retain at least a 3:1 contrast ratio. Unknown query options are
rejected with `400 VALIDATION_ERROR`; this includes options such as `url`, `logo`, or unbounded
customization data.

## Response and storage behavior

A successful response has `Content-Type: image/svg+xml`, an attachment filename of
`{shortCode}-qr.svg`, `Cache-Control: private, no-store`, and `X-Content-Type-Options: nosniff`.
Generation is in memory and the SVG is not stored in the database, object storage, or cache.

Missing or cross-owner links return the same concealed `404 NOT_FOUND`. Deleted links are not
eligible for QR generation because they are excluded from normal owned-link detail resolution.

## Angular workflow

The link details page requests a 320-pixel preview after loading a non-deleted link and offers a
640-pixel SVG download. Loading and failure states are announced through the shared state panel,
and failed previews can be retried. The canonical short URL remains visible as a normal text link
beside the image so the destination is available without interpreting the QR graphic.
