# ADR 0002: .NET 10 Runtime Baseline

- Status: Accepted
- Date: 2026-08-11
- Decision owners: URL Shortener maintainers
- Related task: TASK-006

## Context

The backend began on .NET 8. As of this decision, .NET 8 is in maintenance support and reaches end of support on 2026-11-10. .NET 10 is the active Long Term Support release and is supported through 2028-11-14.

Phase 01 is the roadmap's designated platform-modernization point. Moving now avoids beginning identity, caching, worker, and observability work on a runtime nearing end of support.

## Decision

- Target `net10.0` consistently across all backend projects.
- Use the .NET 10 SDK, pinned by `global.json` to the 10.0.100 feature band with latest-patch roll-forward and prerelease SDKs disabled.
- Align EF Core runtime, SQL Server provider, and design tooling on version 10.0.10.
- Keep package versions explicit in project files.
- Preserve the existing controller routes, JSON shapes, redirect semantics, and persistence behavior during the upgrade.

## Package Review

- Removed AutoMapper and its DI integration. Two simple mappings are now explicit Application methods, eliminating redundant runtime/reflection infrastructure and the advisory reported for the former version.
- Removed `FluentValidation.AspNetCore`. The package is no longer supported and its MVC auto-validation pipeline is no longer recommended. The API already invokes validators explicitly and asynchronously, so `FluentValidation` and `FluentValidation.DependencyInjectionExtensions` retain the same validation contract.
- Upgraded Swashbuckle to its .NET 10-compatible line to preserve Swagger/OpenAPI UI behavior.
- Upgraded Serilog ASP.NET Core and file sink packages to supported compatible releases.
- Aligned all first-party EF Core packages to the same .NET 10 patch version.

## Alternatives Considered

### Remain on .NET 8

Rejected because its support window ends in 2026 and would force another platform migration during later product phases.

### Target .NET 11 preview

Rejected because preview runtimes are not an appropriate production baseline and are not covered by the selected LTS lifecycle.

### Replace Swagger or FluentValidation entirely

Rejected for this task. Both remain useful and replacing their public-facing behavior would combine a runtime upgrade with unrelated contract work.

## Consequences

- Developers and CI need a compatible .NET 10 SDK.
- Hosts need the .NET 10 ASP.NET Core runtime unless the application is published self-contained.
- The upgrade receives active LTS servicing through November 2028.
- Runtime/package changes do not intentionally alter the public API.

## References

- [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy)
- [`global.json` SDK selection](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json)
- [FluentValidation ASP.NET Core guidance](https://docs.fluentvalidation.net/en/latest/aspnet.html)
