# API-Key Security and Management

API-key management is available only with SQL-backed persistence. Every route is under
`/api/v1/api-keys`, requires the current user's Bearer access token, and is owner-scoped. API-key
request authentication is introduced separately by TASK-038; creating a credential in this task
does not make the credential an accepted authentication scheme yet.

## Credential format and storage

The full credential has this shape:

```text
usk_<22-character lookup identifier>.<43-character secret>
```

`usk_` is a versioned product marker. The lookup identifier is 16 cryptographically random bytes
(128 bits) encoded with unpadded base64url and is not secret. The secret is a separately generated
32-byte value (256 bits) encoded the same way. Both random values come from
`RandomNumberGenerator`.

SQL Server stores the complete non-secret `usk_<identifier>` prefix and a fixed 32-byte SHA-256
digest of the decoded random secret. SHA-256 is appropriate here because the input is a uniformly
random 256-bit credential rather than a human password. The plaintext secret, its encoded form,
and the assembled full credential are never entity properties or database columns. TASK-038 must
decode the supplied secret, hash it, and compare the result with `CryptographicOperations.FixedTimeEquals`.

The full `key` value appears only in a successful create or rotate response. Those responses send
`Cache-Control: no-store`. List responses expose only the public prefix and safe metadata. Request
and response payload logging must not be enabled for these routes, and credentials must never be
included in application log properties, exception messages, URLs, or query strings.

## Names, limits, and scopes

- A name is 1-64 characters, begins with an ASCII letter or digit, has no surrounding whitespace,
  and otherwise contains only ASCII letters, digits, spaces, `.`, `_`, or `-`.
- A user may own at most 10 simultaneously active (not revoked and not expired) keys. Creation
  enforces the limit in a serializable transaction over the owner/state index.
- Expiry is optional. When present, it must be an explicit UTC value later than creation.
- At least one scope is required. The only persisted flags and accepted request names are
  `shorturls:create`, `shorturls:read`, `shorturls:write`, and `analytics:read`.
- The database check constraint rejects zero or unknown scope bits, so scopes cannot become an
  unchecked string bag even if a non-HTTP writer bypasses request validation.

## HTTP contract

Create a key with `POST /api/v1/api-keys`:

```json
{
  "name": "deployment",
  "scopes": ["shorturls:create", "shorturls:read"],
  "expiresAtUtc": "2026-12-31T00:00:00Z"
}
```

The `201 Created` body contains safe metadata plus the one-time credential:

```json
{
  "apiKey": {
    "id": "be408662-211d-4dca-b45a-b70915167bb5",
    "name": "deployment",
    "prefix": "usk_Qf7mZ2Lk9Yp3Vx8Nc4RtAw",
    "scopes": ["shorturls:create", "shorturls:read"],
    "createdAtUtc": "2026-08-19T14:00:00Z",
    "expiresAtUtc": "2026-12-31T00:00:00Z",
    "lastUsedAtUtc": null,
    "revokedAtUtc": null,
    "state": "active",
    "replacedByApiKeyId": null
  },
  "key": "usk_Qf7mZ2Lk9Yp3Vx8Nc4RtAw.<secret-shown-only-once>"
}
```

Copy `key` immediately; it cannot be retrieved later. Other operations are:

| Operation | Result |
|---|---|
| `GET /api/v1/api-keys` | `200` safe metadata for active, expired, and revoked owned keys |
| `DELETE /api/v1/api-keys/{id}` | `204`; marks the row revoked without deleting it |
| `POST /api/v1/api-keys/{id}/rotate` | `201`; atomically revokes the active predecessor and returns a one-time replacement credential |

Rotation preserves the predecessor's name, exact scope flags, and expiry. The predecessor records
`revokedAtUtc`, the internal reason `rotated`, and `replacedByApiKeyId`. Expired or already-revoked
keys cannot be rotated. A missing or unowned ID is always the same `404 NOT_FOUND`; invalid state
and repeated revocation return `409 API_KEY_STATE_CONFLICT`. Exceeding the active-key limit returns
`409 API_KEY_LIMIT_REACHED`.

The list `state` is derived as `revoked`, `expired`, or `active`, in that precedence order.
`lastUsedAtUtc` is persisted for the bounded update strategy introduced with API-key
authentication in TASK-038.

## Persistence and operations

Migration `AddApiKeySecurityModel` creates `ApiKeys`, its owner and self-replacement foreign keys,
fixed-length secret-hash column, row version, state/time/scope constraints, unique binary-collated
prefix lookup index, and owner listing/active-count indexes. Revocation intentionally retains the
row. Apply or roll forward this append-only migration through the normal operator-managed process
described in [Persistence and Migrations](persistence.md).
