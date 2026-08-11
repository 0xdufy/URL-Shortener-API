# Engineering Standards

These standards apply to the API, backend libraries, future Angular application, workers, infrastructure, migrations, and documentation in this repository. Task-specific requirements may be stricter but must not silently contradict this document.

## Repository and naming

- Backend projects use `UrlShortener.<LayerOrProcess>` and matching root namespaces. Project folders sit at repository root until an approved architecture task changes the layout.
- C# types and public members use PascalCase; parameters and locals use camelCase; private fields use `_camelCase`; interfaces use the `I` prefix.
- One primary public type per C# file; the filename matches the type. Keep transport DTOs, domain models, and persistence configuration distinguishable by folder and role.
- Angular feature folders use kebab-case. Components/directives/pipes use Angular suffixes; services use `.service.ts`; tests use the toolchain-standard `.spec.ts` when Phase 16 introduces them.
- Do not add a project, abstraction, or top-level folder without a concrete ownership/dependency reason. Placeholder `web/` and `workers/` folders may be replaced only by their roadmap tasks.

## C# correctness and formatting

- Nullable reference types and implicit usings remain enabled. Model absence explicitly; do not suppress nullable warnings with `!` unless an invariant is clear at the use site.
- Follow `.editorconfig`. Before review, run `dotnet format UrlShortener.sln --verify-no-changes --no-restore` and `dotnet build UrlShortener.sln --no-restore` after restore.
- Use async APIs for I/O. Async methods end in `Async`, avoid sync-over-async, and accept/propagate `CancellationToken` when cancellation remains meaningful. Boundary code must not replace a caller token with `CancellationToken.None`.
- Use UTC at persistence and API boundaries. UTC member names end in `Utc`; create instants through the application clock abstraction when behavior depends on time. A deliberate local-time or offset-preserving contract requires documentation.
- Avoid culture-sensitive parsing, formatting, comparison, and casing in identifiers. Short-code comparison must remain explicit and consistent with the database collation.
- Keep methods cohesive and prefer clear control flow over hidden side effects. Comments explain why or invariants, not syntax.

### Warning policy

Compiler warnings are not promoted globally to errors in Phase 00. The current baseline must build with zero warnings, and new warnings are not acceptable. Global `TreatWarningsAsErrors` may be enabled in Phase 01 after SDK/package policy is stabilized; dependency advisory/network warnings need an explicit CI policy rather than local suppression. Do not add broad `NoWarn` entries.

## Angular and TypeScript

- The Angular workspace does not yet exist. When Phase 04 creates it, strict TypeScript and strict Angular template checking are required.
- Organize by product feature, with narrowly scoped shared UI/utilities. Do not create a global dumping-ground module or mirror backend layers mechanically.
- Components own presentation and user interaction; injectable services/facades own API and cross-component state. Business authorization and invariant enforcement remain server-side.
- Prefer standalone components unless the selected Angular baseline documents another choice. Use typed reactive forms for non-trivial forms and avoid `any`; use `unknown` plus narrowing at untrusted boundaries.
- API calls go through a generated or maintained typed client and centralized HTTP/error handling. Components must not assemble API URLs or duplicate the backend error envelope.
- Observables/signals must have explicit ownership and teardown. Do not nest subscriptions when composition operators express the flow.
- The future `web/package.json` must expose repository-standard scripts: `format`, `format:check`, `lint`, and `build`. Contributors run them with `npm run <script>` after `npm ci`; tool versions live in the lock file/package manifest, not global installations.

## Dependency direction and business logic

- Domain has no project dependency and contains core domain state/rules without ASP.NET Core or EF Core concerns.
- Application may depend on Domain. It owns use-case orchestration, DTOs/ports, and validation that is independent of HTTP/persistence mechanics.
- Infrastructure may depend on Application and Domain to implement ports. Database/provider/cache/queue details stay here.
- API and worker processes are composition roots. Controllers translate HTTP only; they do not contain reusable business rules. Workers must call application use cases rather than controllers.
- Angular is a client of documented HTTP contracts and must not be treated as an enforcement boundary.
- Add abstractions at real replaceable boundaries or to isolate external effects, not merely to wrap every class.

## Configuration and secrets

- Bind related settings to named options, validate required/ranged values on startup, and document every required production key.
- Commit safe defaults and examples only. Use environment variables, .NET user secrets, CI secret stores, or deployment secret stores for credentials.
- Never commit passwords, connection strings containing credentials, session/refresh tokens, private keys, API-key secrets, or provider secrets. Local overrides use ignored `.env*` or `appsettings.*.Local.json` files; sanitized `.env.example` files may be committed.
- Production behavior must fail clearly for missing required secrets. Do not silently fall back to insecure or in-memory behavior outside Development.

## Database and migrations

- EF entity configurations and migrations live in Infrastructure. `dotnet-ef` is invoked through the repository tool manifest.
- Make a model change and its migration reviewable together. Inspect generated operations, indexes, nullability, collation, cascade behavior, and data-loss implications.
- Never edit a migration already applied to a shared/production database without an explicit migration strategy. Correct it with a new migration or document an approved reset for disposable environments.
- Database uniqueness and constraints are authoritative. Application pre-checks may improve messages but cannot be the only concurrency control.
- Document upgrade, rollback/forward-fix, and required configuration for deployment-affecting migrations. Do not run database updates automatically at application startup unless an ADR approves it.

## API contracts

- Product APIs are versioned under `/api/v1`; the redirect route is intentionally unversioned. A new breaking contract requires an explicit task, documentation update, and reviewer acknowledgement; use a new API version when compatibility cannot be maintained.
- Preserve the structured error envelope: `traceId` and `error.code`, `error.message`, `error.details[]` with `field` and `message`. Do not leak exception text or internals.
- Maintain accurate status semantics, OpenAPI metadata, request/response examples, and `README.md`/`docs/baseline-contracts.md` when public behavior changes.
- Validate at the boundary and enforce invariants in the relevant application/domain/persistence layer. Use `401` for absent/invalid identity and `403` for an authenticated caller lacking permission once identity exists.
- I/O endpoints propagate request cancellation. Collection endpoints added later require bounded deterministic pagination.

## Logging and sensitive data

- Use structured message templates and stable property names. Include correlation/trace IDs through framework enrichment; do not concatenate structured values into message text.
- Log operational events at intentional levels. Expected validation/not-found outcomes should not generate noisy error logs; unexpected failures must retain diagnostic context without exposing private data.
- Never log passwords, password hashes, session tokens, refresh tokens, API-key secrets, `Authorization` headers, cookies, private keys, or secret-bearing connection strings.
- Treat raw URLs, query strings, IP addresses, user agents, referrers, email addresses, and user-supplied text as potentially sensitive. Log or retain them only when a documented operational/product need and privacy policy permit it.

## Generated and intentionally committed files

Generated build output, IDE state, logs, frontend dependencies/output, coverage, temporary files, and local secret overrides are ignored and must not be committed.

The following generated or tool-maintained categories are intentionally committed because they are reproducibility or source artifacts:

- EF Core migrations and model snapshots.
- `.config/dotnet-tools.json`, `global.json`, project/solution files, and future dependency lock files.
- Angular CLI workspace/configuration and package lock files when the frontend is introduced.
- Generated typed API clients only if their owning task documents the generator, exact regeneration command, and review policy.

## Task, review, and documentation workflow

- Work on the lowest-numbered incomplete task in the active phase. Use exactly `Planned`, `In Progress`, `Blocked`, or `Completed` in task files.
- Mark a task `Completed` only after every acceptance criterion and verification step is satisfied. Add a concise dated implementation/verification record; document a blocker with evidence rather than bypassing it.
- Keep commits and reviews scoped to one coherent task. Separate mechanical generation/formatting from behavior changes when that improves reviewability. Do not mix opportunistic later-phase features into the active task.
- Review public contracts, security/privacy, concurrency, migrations, dependency direction, cancellation, configuration, and observability in proportion to risk—not only formatting.
- Update nearby documentation with behavior/configuration changes. Record material, durable decisions and alternatives in `docs/adr/` using the numbered ADR convention; do not create ADRs for routine implementation details.

## Package and tool policy

Central package management is deferred to Phase 01, where the package/runtime baseline is owned. Until then, package versions remain explicit in each project and must be kept compatible across projects.

Backend commands are run from repository root using the pinned SDK/tool manifest:

```powershell
dotnet tool restore
dotnet restore UrlShortener.sln
dotnet format UrlShortener.sln --verify-no-changes --no-restore
dotnet build UrlShortener.sln --no-restore
```

Frontend commands, once `web/package.json` exists, are run from `web/`:

```powershell
npm ci
npm run format:check
npm run lint
npm run build
```

