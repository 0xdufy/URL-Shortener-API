namespace UrlShortener.Application.Interfaces;

public interface ICustomDomainOwnershipVerifier
{
    Task<CustomDomainVerificationEvidence> VerifyTxtRecordAsync(
        string recordName,
        string expectedValue,
        CancellationToken ct);
}

public sealed record CustomDomainVerificationEvidence(
    CustomDomainVerificationEvidenceStatus Status,
    string? FailureCode = null,
    string? FailureMessage = null)
{
    public static CustomDomainVerificationEvidence Verified { get; } =
        new(CustomDomainVerificationEvidenceStatus.Verified);
}

public enum CustomDomainVerificationEvidenceStatus
{
    Verified,
    Failed
}
