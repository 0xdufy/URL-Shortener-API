using UrlShortener.Application.Interfaces;
using UrlShortener.Domain.CustomDomains;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence.Repositories;

public sealed class InMemoryCustomDomainRepository : ICustomDomainRepository
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, CustomDomain> _domains = [];

    public Task<CustomDomainCreateOutcome> TryCreateAsync(CustomDomain customDomain, CancellationToken ct)
    {
        lock (_gate)
        {
            if (_domains.Values.Any(candidate =>
                candidate.NormalizedHost.Equals(customDomain.NormalizedHost, StringComparison.Ordinal)))
            {
                return Task.FromResult(CustomDomainCreateOutcome.HostConflict);
            }

            _domains.Add(customDomain.Id, customDomain);
            return Task.FromResult(CustomDomainCreateOutcome.Created);
        }
    }

    public Task<IReadOnlyList<CustomDomain>> ListOwnedAsync(Guid ownerId, CancellationToken ct)
    {
        lock (_gate)
        {
            IReadOnlyList<CustomDomain> domains = _domains.Values
                .Where(customDomain => customDomain.OwnerId == ownerId)
                .OrderByDescending(customDomain => customDomain.CreatedAtUtc)
                .ThenByDescending(customDomain => customDomain.Id)
                .ToList();
            return Task.FromResult(domains);
        }
    }

    public Task<CustomDomain?> GetOwnedAsync(Guid customDomainId, Guid ownerId, CancellationToken ct)
    {
        lock (_gate)
        {
            _domains.TryGetValue(customDomainId, out var customDomain);
            return Task.FromResult(customDomain?.OwnerId == ownerId ? customDomain : null);
        }
    }

    public Task<CustomDomainMutationOutcome> RequestVerificationAsync(
        Guid customDomainId,
        Guid ownerId,
        string verificationToken,
        DateTime requestedAtUtc,
        CancellationToken ct)
    {
        lock (_gate)
        {
            if (!TryGetOwned(customDomainId, ownerId, out var customDomain))
            {
                return Task.FromResult(CustomDomainMutationOutcome.NotFound);
            }

            customDomain.RequestVerification(verificationToken, requestedAtUtc);
            return Task.FromResult(CustomDomainMutationOutcome.Updated);
        }
    }

    public Task<CustomDomainMutationOutcome> RecordVerificationAsync(
        Guid customDomainId,
        Guid ownerId,
        string expectedVerificationToken,
        CustomDomainVerificationEvidence evidence,
        DateTime attemptedAtUtc,
        CancellationToken ct)
    {
        lock (_gate)
        {
            if (!TryGetOwned(customDomainId, ownerId, out var customDomain))
            {
                return Task.FromResult(CustomDomainMutationOutcome.NotFound);
            }

            if (!customDomain.VerificationToken.Equals(expectedVerificationToken, StringComparison.Ordinal) ||
                customDomain.Status is CustomDomainStatus.Disabled or CustomDomainStatus.Verified)
            {
                return Task.FromResult(CustomDomainMutationOutcome.StateChanged);
            }

            if (evidence.Status == CustomDomainVerificationEvidenceStatus.Verified)
            {
                customDomain.RecordVerificationSuccess(attemptedAtUtc);
            }
            else
            {
                customDomain.RecordVerificationFailure(
                    attemptedAtUtc,
                    evidence.FailureCode ?? "DNS_VERIFICATION_FAILED",
                    evidence.FailureMessage ?? "DNS ownership verification failed. Review the TXT record and try again.");
            }

            return Task.FromResult(CustomDomainMutationOutcome.Updated);
        }
    }

    public Task<CustomDomainMutationOutcome> DisableAsync(
        Guid customDomainId,
        Guid ownerId,
        DateTime disabledAtUtc,
        CancellationToken ct)
    {
        lock (_gate)
        {
            if (!TryGetOwned(customDomainId, ownerId, out var customDomain))
            {
                return Task.FromResult(CustomDomainMutationOutcome.NotFound);
            }

            customDomain.Disable(disabledAtUtc);
            return Task.FromResult(CustomDomainMutationOutcome.Updated);
        }
    }

    private bool TryGetOwned(Guid id, Guid ownerId, out CustomDomain customDomain)
    {
        if (_domains.TryGetValue(id, out var candidate) && candidate.OwnerId == ownerId)
        {
            customDomain = candidate;
            return true;
        }

        customDomain = null!;
        return false;
    }
}
