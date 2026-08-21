# Authentication and Session API

Phase 02 uses ASP.NET Core Identity accounts, 10-minute signed JWT access tokens, and rotating SQL-backed refresh sessions. The architectural and security rationale is recorded in [ADR 0003](adr/0003-identity-and-session-architecture.md).

## Required configuration

Authentication persistence is available only when SQL Server storage is enabled. `Storage:UseInMemory=true` keeps the legacy development short-link repository available but makes authentication endpoints return `503 AUTHENTICATION_UNAVAILABLE`; identity never falls back to process memory.

Provide a random signing key of at least 32 bytes as base64 through a secret source. For a disposable PowerShell development session:

```powershell
$jwtKeyBytes = New-Object byte[] 32
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($jwtKeyBytes)
$rng.Dispose()
$env:Identity__JwtSigningKeyBase64 = [Convert]::ToBase64String($jwtKeyBytes)
$env:Storage__UseInMemory = "false"
```

Never commit or log this value. A stable deployment must supply the same protected key to every API instance until an intentional signing-key rotation procedure replaces it.

ASP.NET Core antiforgery also uses Data Protection. Multi-instance deployments must persist and share that protected key ring; the container/deployment phase owns the concrete key-ring provider. A machine-local development key ring is not a production topology.

Safe, non-secret settings live under `Identity`:

| Key | Default | Purpose |
|---|---:|---|
| `PublicRegistrationEnabled` | `true` | Enables public account registration. |
| `RequireSecureCookies` | `true` | Requires HTTPS transport for refresh and antiforgery cookies. May be set to `false` only in Development. |
| `JwtIssuer` | `UrlShortener.Api` | Required JWT issuer. |
| `JwtAudience` | `UrlShortener.Client` | Required JWT audience. |
| `JwtClockSkewSeconds` | `30` | Allowed validation skew, bounded to 0–120 seconds. |
| `AllowedOrigins` | local Angular origins | Exact origins accepted for credentialed CORS and refresh/sign-out CSRF checks; deployed origins must use HTTPS. |
| `AccessTokenLifetimeMinutes` | `10` | JWT lifetime, bounded to 5–30 minutes. |
| `RefreshTokenLifetimeDays` | `30` | Rolling refresh-session lifetime. |
| `RefreshTokenAbsoluteLifetimeDays` | `90` | Maximum refresh-family lifetime. |

Authentication endpoints use Redis-backed IP partitions shared by every API instance. Registration
and sign-in have independent sliding-window policies; refresh and sign-out share the session policy.
Bootstrap uses the broader anonymous fixed-window policy. Defaults, configuration bounds, direct-IP
semantics, and outage behavior are documented in [Distributed Rate Limiting](rate-limiting.md).

## Transport contract

- Registration, sign-in, and refresh return an access token in JSON. The future Angular client keeps it in memory and sends `Authorization: Bearer <token>`.
- The raw refresh token is never returned in JSON. It is delivered as the `urlshortener.refresh` cookie with `HttpOnly`, `SameSite=Strict`, a path of `/api/v1/auth`, and `Secure` outside an explicit Development-only HTTP run.
- The response contains `csrfToken`. Send it as `X-XSRF-TOKEN` together with the antiforgery cookie and an exact configured `Origin` header for refresh and sign-out.
- Authentication responses, including errors, use `Cache-Control: no-store`; credentialed CORS is
  restricted to the methods and headers listed in [HTTP, Authentication, and Session Security](http-auth-security.md).
- A successful refresh rotates the refresh token. Reuse of a rotated/revoked token revokes its active replacement family.
- Sign-out revokes the refresh family and deletes the browser cookie. A previously issued access token remains valid only until its documented short expiry, as defined by ADR 0003.
- Passwords, password hashes, security stamps, token hashes, raw refresh tokens, cookies, and signing keys are never included in logs or error bodies.

## Endpoints

### `GET /api/v1/auth/bootstrap`

Returns the antiforgery request token required to resume or end an HttpOnly refresh-cookie session,
plus safe public registration and password-policy metadata. The response stores the matching
HttpOnly antiforgery cookie. The Angular client keeps the returned token in memory; it does not
persist it in browser storage.

```json
{
  "csrfToken": "<antiforgery request token>",
  "publicRegistrationEnabled": true,
  "passwordRequiredLength": 12,
  "passwordRequiredUniqueChars": 4
}
```

### `POST /api/v1/auth/register`

Request:

```json
{
  "email": "user@example.com",
  "password": "Strong-Password-123!"
}
```

Returns `201` with the session response. Duplicate identity values return the generic `409 ACCOUNT_UNAVAILABLE`; password-policy and input failures return `400 VALIDATION_ERROR`.

### `POST /api/v1/auth/sign-in`

Uses the same request shape. Unknown accounts, wrong passwords, locked accounts, and inactive accounts all return the same `401 AUTHENTICATION_FAILED` contract.

### Session response

Registration, sign-in, and refresh return:

```json
{
  "accessToken": "<JWT>",
  "tokenType": "Bearer",
  "accessTokenExpiresAtUtc": "2026-08-11T21:30:00Z",
  "refreshSessionExpiresAtUtc": "2026-09-10T21:20:00Z",
  "csrfToken": "<antiforgery request token>",
  "user": {
    "id": "3c96eb91-9ef7-43e3-8ecb-da6231090052",
    "email": "user@example.com",
    "createdAtUtc": "2026-08-11T21:20:00Z"
  }
}
```

### `GET /api/v1/auth/me`

Requires the JWT bearer header. Returns safe user metadata plus the current refresh-session ID, creation/expiry timestamps, and revocation state. Missing, malformed, expired, or invalid bearer tokens return `401` using the common error envelope.

### `POST /api/v1/auth/refresh`

Requires the refresh cookie, antiforgery cookie, `X-XSRF-TOKEN`, and an approved `Origin`. Returns a new session response and replaces the refresh cookie. Missing, expired, revoked, reused, or security-stamp-invalidated refresh credentials return `401 INVALID_SESSION`.

### `POST /api/v1/auth/sign-out`

Requires the same CSRF/origin inputs as refresh. Revokes the presented refresh family, removes the refresh cookie, and returns `204`. The operation is idempotent when the refresh cookie no longer resolves to a session.

## Error shape

Authentication errors retain the platform contract:

```json
{
  "traceId": "0HNN...",
  "error": {
    "code": "AUTHENTICATION_FAILED",
    "message": "Invalid credentials.",
    "details": []
  }
}
```

Relevant codes are `VALIDATION_ERROR`, `ACCOUNT_UNAVAILABLE`, `AUTHENTICATION_FAILED`,
`AUTHENTICATION_REQUIRED`, `INVALID_SESSION`, `CSRF_VALIDATION_FAILED`, `RATE_LIMITED`,
`RATE_LIMITING_UNAVAILABLE`, and `AUTHENTICATION_UNAVAILABLE`.
