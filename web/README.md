# URL Shortener Web

The Angular SPA lives in this directory. It uses standalone components, lazy route boundaries,
strict TypeScript/template compilation, ESLint, and Prettier. Automated test setup is intentionally
deferred to Phase 16.

## Prerequisites

- Node.js `^20.19.0`, `^22.12.0`, or `>=24.0.0`
- npm `>=10.0.0` (the workspace records npm `11.6.2` as its package-manager baseline)

## Install and verify

Run all frontend commands from `web/`:

```powershell
npm ci
npm run format:check
npm run lint
npm run build
```

Package versions are exact in `package.json` and reproducible through `package-lock.json`.

## Local development

Start the API from the repository root:

```powershell
dotnet run --project UrlShortener.Api
```

Then start Angular from `web/`:

```powershell
npm start
```

Open `http://localhost:4200`. The development environment points the centralized API base URL to
`http://localhost:5034/api/v1`, matching the API's HTTP development profile.

## API base URL strategy

Application code injects `API_BASE_URL`; feature components must not contain API origins. Angular's
development build replaces `environment.ts` with `environment.development.ts`. Production uses the
same-origin relative base `/api/v1`, allowing the deployment reverse proxy or static host to choose
the public origin without rebuilding the SPA.

These files contain public client configuration only. API credentials, signing keys, connection
strings, and other secrets must never be placed in Angular environment files because browser assets
are readable by every user.

Change a deployment's API origin by supplying the appropriate environment file at build time or by
keeping the production same-origin `/api` routing contract. A fully runtime-loaded configuration can
be introduced with the hosting topology in Phase 15 if deployments need one immutable image across
multiple origins.

## Typed API, authentication state, and errors

Application code calls `AuthenticationApiClient`, `ShortUrlsApiClient`, and feature-specific typed
clients such as `CustomDomainsApiClient` from `src/app/core/api/`.
Feature components must not construct API paths, attach authentication headers, enable cookie
credentials, or duplicate transport DTOs. Contract timestamps remain ISO-8601 strings at the HTTP
boundary so feature code can choose when and how to create `Date` values without hiding timezone
conversion.

`AuthenticationStateService` is the single in-memory owner of the access token, CSRF request token,
expiry metadata, and authenticated user. It does not use local storage or session storage. The auth
client stores successful register, sign-in, and refresh responses in that service. The HTTP
interceptor sends the access token only to configured API URLs and sends browser credentials plus
`X-XSRF-TOKEN` only for the cookie-authenticated auth operations that request them. Sign-out clears
local state even if the remote request fails.

Every API request receives an `X-Client-Request-ID` for client-side diagnostics. Backend failures are
translated to `ApiError`, which retains the backend `traceId` separately because it is the
authoritative server correlation value. `ApiError.kind` distinguishes authentication,
authorization, validation, not-found, conflict, gone, rate-limited, connectivity, service, and
unexpected failures; `isUserActionable` lets UI code choose specific recovery feedback instead of
presenting a generic service message. Validation details are available through
`validationMessages(field)` or `validationErrors()`, and a valid `Retry-After` header is exposed as
`retryAfterSeconds`.

A `401` from an access-token request, or `INVALID_SESSION` from refresh, clears auth state through an
idempotent transition to `unauthorized`. Interceptors never navigate or retry authentication. The
Phase 05 authentication experience owns refresh/bootstrap policy, safe return URLs, guards, and the
single navigation response to that state, which prevents interceptor/guard redirect loops.

The authentication experience is available at `/auth/sign-in` and, when the server reports public
registration enabled, `/auth/register`. The `/app/*` guard first requests `/auth/bootstrap` to obtain
a fresh in-memory antiforgery request token and safe public policy metadata, then attempts refresh
and current-user reconciliation. This makes full-page reload recovery possible without placing an
access token, refresh token, or antiforgery token in local or session storage. Only return URLs under
`/app` are honored after authentication.

The application shell derives its account label from the safe current-user response and owns the
explicit sign-out action. An expired or revoked session clears protected state and performs one
replace-navigation to sign-in with the current safe application URL. The HTTP interceptor remains
navigation-free and does not retry requests, so authorization failures cannot create refresh or
redirect loops.

### Client maintenance decision

The client is manually maintained for the current compact API surface. This avoids committing a
generator runtime, templates, and generated churn before the OpenAPI document is published as a
stable build artifact. The trade-off is that contract drift is caught during review and integration
verification rather than by regeneration. Reconsider generation when the API surface or number of
consumers makes manual review unreliable.

When a backend contract changes:

1. Review the Swagger schema and the owning contract document in `docs/`.
2. Update `api.models.ts` first, preserving server JSON names and nullability, then update the owning
   typed client method.
3. Update error classification only when the platform adds a new status or recovery category; keep
   feature-specific error codes available through `ApiError.code`.
4. Run `npm run format:check`, `npm run lint`, and `npm run build` from `web/`, then exercise the
   changed endpoint against the real API. Commit no generated client artifacts.

## Source boundaries

- `src/app/core/` owns application-wide configuration, typed API, authentication state, and HTTP
  infrastructure boundaries.
- `src/app/features/` owns product areas and their lazy route definitions.
- `src/app/shared/ui/` owns presentation-only controls that are intentionally reused across feature
  areas; `shared/` must not become a catch-all for business or API logic.

The `/auth/*` and `/app/*` route trees are separate lazy boundaries. Authentication screens and the
guard protecting `/app/*` are intentionally deferred to the tasks that own those behaviors.

## Design system and application shell

The UI foundation uses small standalone Angular components plus design tokens and structural classes
in `src/styles.scss`. This approach keeps the initial bundle and dependency surface small, works with
Angular's native accessibility semantics, and avoids committing the product to a general-purpose
component library before feature requirements are known. Do not add an overlapping UI library
without first documenting the missing capability and migration impact.

Reusable primitives currently cover buttons, fields, badges, page headers, loading/empty/error
states, native-modal destructive confirmation, toast feedback, and the shared icon set. Controls use
the global color, spacing, radius, and focus tokens; status patterns pair visual color with text or an
icon. The native `dialog` element supplies modal keyboard containment and Escape behavior, while the
responsive application shell supplies a skip link and an Escape-closeable mobile navigation drawer.

Feature routes beneath `/app` render inside the common shell. Dashboard, Links, Analytics, API Keys,
Domains, and Account are present as navigation destinations, but their product behavior remains
deferred to the tasks that own those features.
