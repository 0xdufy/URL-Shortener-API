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

## Source boundaries

- `src/app/core/` owns application-wide configuration and infrastructure boundaries.
- `src/app/features/` owns product areas and their lazy route definitions.
- Shared visual controls belong in a narrowly scoped shared UI location when TASK-017 introduces the
  design system; `shared/` must not become a catch-all.

The `/auth/*` and `/app/*` route trees are separate lazy boundaries. Authentication screens and the
guard protecting `/app/*` are intentionally deferred to the tasks that own those behaviors.
