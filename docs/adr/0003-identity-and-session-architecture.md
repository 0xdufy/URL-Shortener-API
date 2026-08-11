# ADR 0003: Identity and Session Architecture

- Status: Accepted
- Date: 2026-08-11
- Decision owners: URL Shortener maintainers
- Related task: TASK-008

## Context

The product needs authenticated Angular sessions now and scoped API-key access later. Password handling, lockout, identity normalization, unique account constraints, revocation, and multi-instance behavior are security boundaries. The Domain and Application projects must remain independent of ASP.NET Core HTTP identity types.

The current development-only in-memory short-link repository is not an acceptable identity store. Identity and refresh-session state require the configured SQL Server database so that uniqueness, rotation, revocation, and concurrent requests have one authoritative source.

## Decision

### Account model and password handling

- Use ASP.NET Core Identity with `ApplicationUser : IdentityUser<Guid>` and EF Core stores. Identity owns password hashing and verification through its versioned `PasswordHasher`; the application will never implement or persist plaintext/recoverable passwords.
- Use email as the only login identifier. `UserName` is set to the same canonical email for Identity compatibility; a separate public username is not introduced.
- Normalize email and username with the configured ASP.NET Core Identity normalizer before lookup or persistence. Both normalized values have authoritative unique database indexes. User-facing comparisons must not replace those constraints.
- Persist account `Status` (`Active`, `Suspended`, or `Disabled`) plus `CreatedAtUtc` and `UpdatedAtUtc`. Identity's lockout, failed-attempt, concurrency-stamp, security-stamp, email-confirmation, and phone/two-factor fields remain available. Public registration initially creates an active account without requiring email confirmation because no email-verification provider exists in this roadmap phase.
- Require passwords of 12 to 128 characters with upper-case, lower-case, digit, non-alphanumeric, and at least four unique characters. Lock a new account for 15 minutes after five failed attempts. These safe settings are configurable only within validated bounds.

### Access and refresh credentials

- Issue a signed JWT access token with a 10-minute lifetime. It contains the stable user ID, session ID, security version/stamp identifier, and minimal authorization claims; it must not contain email or other profile data unless a concrete client contract requires it.
- Sign JWTs with a deployment-supplied key of at least 256 bits. The key is never committed, persisted in application tables, returned by an endpoint, or logged. Token validation requires issuer, audience, signature, and exact UTC lifetime validation with a small documented clock skew.
- Treat an issued access token as non-revocable until its short expiry. Sign-out, refresh-token revocation, password change, account suspension, or reuse detection prevents future refresh, but an already issued access token can remain valid for at most 10 minutes. Operations requiring immediate account-state enforcement may add a database/security-stamp check deliberately rather than making every request perform an accidental database lookup.
- Create refresh tokens from 256 bits of cryptographically secure random data. Persist only `SHA-256(token)` as a fixed 32-byte value because the input is already high entropy; compare hashes in fixed time. The plaintext token exists only during issuance and in the client cookie.
- Give a refresh session a rolling 30-day expiry and a 90-day absolute family expiry. Every successful refresh rotates the token atomically, marks the prior session revoked, links its replacement, and preserves the family ID. Reuse of a rotated/revoked token revokes the remaining family. A row-version protects concurrent rotation.
- Sign-out revokes the presented refresh session/family server-side and expires its cookie. Password changes, account suspension/disablement, or an administrator security-stamp reset revoke all refresh sessions for the account. Expired and revoked sessions are rejected and retained only according to the later data-lifecycle policy.

### Browser transport and CSRF

- Return access tokens in the JSON sign-in/refresh response. The Angular client keeps an access token in memory and sends it in the `Authorization: Bearer` header; it must not persist access or refresh tokens in local storage or session storage.
- Send the refresh token only in a `Secure`, `HttpOnly`, `SameSite=Strict` cookie scoped to the refresh/sign-out endpoint path where practical. Development may relax `Secure` only through an explicit Development-only setting when HTTPS is unavailable; production must fail closed.
- Refresh and sign-out are cookie-authenticated state-changing requests, so SameSite is defense in depth rather than the sole CSRF control. TASK-009 must require the approved origin and an ASP.NET Core antiforgery token/header before consuming the refresh cookie. CORS must use explicit trusted origins and never combine wildcard origins with credentials.
- Programmatic consumers may use a cookie jar for this user-session flow. The dedicated, non-cookie API-key contract remains owned by Phase 10.

### Errors and logging

- Sign-in returns one generic invalid-credentials response for unknown email, wrong password, disabled/suspended account, and lockout where revealing the distinction would enable account enumeration. Server logs may record a stable user ID and coarse reason only after an account is resolved; they must not record email solely for authentication diagnostics.
- Registration returns a generic account-unavailable validation/conflict result for duplicate normalized identity values. It never returns internal Identity error codes that expose stored security state.
- Passwords, hashes, access/refresh tokens, token hashes, security/concurrency stamps, signing keys, `Authorization` headers, cookies, antiforgery secrets, and secret-bearing request bodies are prohibited from logs, traces, metrics, and error bodies. Request logging must not enrich events with these headers or bodies.
- Protected endpoints use `401` for missing/invalid/expired authentication. The ownership existence-concealment policy and `403`/`404` split are finalized in TASK-011.

### Dependency boundary

- Identity/EF types live in Infrastructure and are composed by the API. Domain contains only provider-neutral account state such as `UserAccountStatus`.
- TASK-009 introduces Application ports/use cases for registration and sessions. Application services accept stable `Guid` user/session identifiers and provider-neutral results; they do not consume `HttpContext`, `ClaimsPrincipal`, cookies, or Identity entities.
- TASK-010 introduces the current-user abstraction and owner IDs. HTTP claim extraction remains in API/Infrastructure adapters.

## Alternatives Considered

### Custom user table and custom password workflow

Rejected. Even with a standard password-hashing primitive, a custom workflow would duplicate normalization, lockout, password rehashing, security stamps, recovery extensibility, and concurrency behavior without a product requirement that justifies owning those security details.

### Long-lived self-contained JWTs without refresh-session persistence

Rejected. They make sign-out and credential theft difficult to contain, provide no reliable rotation/reuse detection, and force long exposure windows or frequent interactive login.

### Server-side authentication cookie for every API request

Rejected for the primary API contract. It fits same-origin browser use but makes CSRF protection apply to every mutation and is less natural for non-browser clients. A narrowly scoped refresh cookie retains browser secret protection while bearer access tokens keep normal API calls non-ambient.

### Plaintext database-backed reference tokens

Rejected. A database disclosure would immediately yield active credentials. High-entropy refresh tokens can be located and verified by their SHA-256 digest without retaining the bearer secret.

### Browser local-storage tokens

Rejected. Persistent JavaScript-readable bearer credentials unnecessarily increase the impact of an XSS compromise. The browser contract keeps access tokens in memory and refresh credentials in an HttpOnly cookie.

## Consequences

- Identity and refresh-session operations require SQL Server even while the development-only short-link repository can remain in memory. Authentication endpoints must return a controlled configuration/service-unavailable result if identity persistence is unavailable; they must never fall back to an in-memory production identity store.
- JWT signing-key configuration and token issuance/validation are introduced with TASK-009, before any endpoint is exposed.
- Database cleanup for expired/revoked refresh sessions is deferred to Phase 13, but the schema contains the timestamps and family metadata needed for safe retention.
- Access-token revocation has a documented maximum 10-minute delay. Lower latency would require reference-token validation, a distributed deny list, or per-request account-state reads and must be justified separately.
