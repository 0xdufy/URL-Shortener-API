# HTTP, Authentication, and Session Security

This document records the Phase 12 review of the browser, API, authentication, and deployed HTTP
boundary. The session design remains the one selected in
[ADR 0003](adr/0003-identity-and-session-architecture.md): short-lived bearer access tokens are
kept in Angular memory, while rotating refresh credentials are held in a narrowly scoped
HttpOnly cookie.

## Browser origins and CORS

`Identity:AllowedOrigins` is the only credentialed CORS allowlist. Every entry must be an exact,
absolute origin with no path, query, fragment, trailing slash, or wildcard. Duplicate entries are
rejected. Outside Development every entry must use HTTPS, and startup fails when the list is empty
or invalid. Because the policy is built only with `WithOrigins`, a wildcard-and-credentials policy
cannot be produced from configuration.

The trusted Angular origin receives credentials only for these product requirements:

| Category | Allowed values |
|---|---|
| Methods | `GET`, `POST`, `PUT`, `PATCH`, `DELETE` |
| Request headers | `Authorization`, `Content-Type`, `Idempotency-Key`, `X-Client-Request-ID`, `X-XSRF-TOKEN` |
| Response headers exposed to JavaScript | `Retry-After` |

Browser-safelisted headers do not need to be repeated in the configured preflight allowlist.
Untrusted origins receive no CORS allow-origin or credential response headers. CORS is not an
authentication control: protected operations still require a valid bearer token or API key.

## CSRF and credential transport

Normal API requests use an explicit `Authorization: Bearer` header and therefore do not use an
ambient browser credential. Refresh and sign-out are the two cookie-authenticated mutations. They
must pass all of the following checks before the refresh cookie is consumed:

1. an exact `Origin` match in `Identity:AllowedOrigins`;
2. the HttpOnly `urlshortener.csrf` antiforgery cookie;
3. the matching request token in `X-XSRF-TOKEN`.

The refresh cookie is `HttpOnly`, `SameSite=Strict`, host-only, and scoped to `/api/v1/auth`. It is
also `Secure` in every non-Development environment; startup rejects attempts to relax that rule.
The raw refresh token is never returned in JSON. Authentication endpoints send `Cache-Control:
no-store` and `Pragma: no-cache`, including error and preflight responses, so access tokens,
antiforgery tokens, and session metadata are not stored by shared or browser caches.

Successful refresh rotates the persisted SHA-256-backed refresh session. Reuse and concurrent
rotation detection revoke the active family. Sign-out revokes the family server-side before
deleting the browser cookie. An already issued access token remains usable only for its bounded
10-minute lifetime, as explicitly accepted by ADR 0003.

## HTTPS and reverse proxies

Outside Development, `PublicUrls:BaseUrl`, `PublicUrls:CustomDomainScheme`, and every browser
origin must use HTTPS. The API redirects direct HTTP requests to HTTPS with status `308` and emits
HSTS for 180 days on HTTPS responses. `includeSubDomains` and preload are deliberately disabled
because the API can answer on customer-owned custom domains.

When `ProxyTrust:Enabled=true`, forwarded processing accepts `X-Forwarded-For` and
`X-Forwarded-Proto` only through explicitly configured known proxies/networks and the bounded
forward limit. Processing runs before HTTPS redirection. The edge must strip client-supplied
forwarding headers and write the verified client address and public scheme. `X-Forwarded-Host`
remains untrusted: platform canonical URLs come only from `PublicUrls:BaseUrl`, and branded URLs
come from persisted verified domains plus the startup-validated HTTPS scheme.

Swagger UI and its JSON document are available only in Development. Production deployments expose
the Angular application and supported API/redirect routes, not an interactive credential-entry UI.
The API does not serve the Angular build. In the documented same-origin production topology, the
edge routes `/api` and `/r` to this application and serves Angular separately. That edge must set
an Angular-specific CSP that permits its compiled scripts/styles and API connections; it must not
copy the API's `default-src 'none'` policy onto the Angular document. A split-origin deployment
must add only the Angular app's exact HTTPS origin to `Identity:AllowedOrigins`.

## Response and logging policy

All API responses receive these compatible baseline headers:

- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: no-referrer`
- `Permissions-Policy: camera=(), geolocation=(), microphone=()`
- `X-Permitted-Cross-Domain-Policies: none`
- `Content-Security-Policy: default-src 'none'; base-uri 'none'; form-action 'none'; frame-ancestors 'none'`

Development Swagger is excluded from the API CSP because its interactive UI requires browser
assets; the remaining headers still apply. Kestrel does not emit its `Server` identification
header.

Serilog request completion events contain the method, route path, status code, and duration. The
application does not enrich them with request/response bodies, `Authorization`, `Cookie`,
antiforgery, or secret configuration values. Unexpected exceptions are logged by type and trace ID
without the exception message, stack trace, or attached data because dependency exceptions can
contain connection strings or request values. Public error bodies retain only the stable envelope
and never expose exception details.

Passwords, password hashes, bearer/refresh tokens, API-key credentials, verification values,
signing keys, cookies, and authorization headers must never be added to logs, traces, metrics, or
error details. A future observability change must preserve this denylist.

## Manual security configuration review — 2026-08-21

The Phase 12 review covered the committed configuration, middleware order, Angular interceptors,
authentication controller, token issuer, refresh-session service, API-key handler, exception
mapping, and request logging configuration. It recorded these results:

- a trusted-origin preflight returned only the documented methods/headers plus credential support;
  an untrusted origin received no CORS allow-origin/credential headers, and unsupported requested
  methods/headers were absent from the allowlists;
- authentication `401` and controlled `500` responses returned the no-store and baseline security
  headers, no `Server` header, and the stable public error envelope without a stack trace;
- Development Swagger remained usable without the API CSP;
- production startup rejected HTTP canonical URL and browser-origin configuration;
- source review confirmed origin-plus-antiforgery validation precedes refresh-cookie consumption,
  refresh rotation/reuse handling and server-side family revocation remain intact, and generic
  sign-in/registration errors do not distinguish account state;
- request logs from failed authentication/bootstrap requests contained route and outcome metadata
  but no authorization header, cookie, request body, token, or supplied identity value.

Full SQL-backed register/sign-in/refresh/logout exercise still requires the documented SQL Server,
Redis, and shared Data Protection topology. Repeat that deployment check whenever proxy trust,
origins, TLS termination, token lifetimes, or the key-ring provider changes.
