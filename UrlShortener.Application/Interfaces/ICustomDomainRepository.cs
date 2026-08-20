using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Interfaces;

public interface ICustomDomainRepository
{
    Task<CustomDomainCreateOutcome> TryCreateAsync(CustomDomain customDomain, CancellationToken ct);
    Task<IReadOnlyList<CustomDomain>> ListOwnedAsync(Guid ownerId, CancellationToken ct);
    Task<CustomDomain?> GetOwnedAsync(Guid customDomainId, Guid ownerId, CancellationToken ct);
    Task<CustomDomainMutationOutcome> RequestVerificationAsync(
        Guid customDomainId,
        Guid ownerId,
        string verificationToken,
        DateTime requestedAtUtc,
        CancellationToken ct);
    Task<CustomDomainMutationOutcome> RecordVerificationAsync(
        Guid customDomainId,
        Guid ownerId,
        string expectedVerificationToken,
        CustomDomainVerificationEvidence evidence,
        DateTime attemptedAtUtc,
        CancellationToken ct);
    Task<CustomDomainMutationOutcome> DisableAsync(
        Guid customDomainId,
        Guid ownerId,
        DateTime disabledAtUtc,
        CancellationToken ct);
}

public enum CustomDomainCreateOutcome
{
    Created,
    HostConflict
}

public enum CustomDomainMutationOutcome
{
    Updated,
    NotFound,
    StateChanged
}
