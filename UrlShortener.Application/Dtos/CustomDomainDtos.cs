namespace UrlShortener.Application.Dtos;

public sealed class RegisterCustomDomainRequest
{
    public string Host { get; set; } = string.Empty;
}

public sealed class CustomDomainResponse
{
    public Guid Id { get; init; }
    public string Host { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string VerificationMethod { get; init; } = string.Empty;
    public CustomDomainVerificationRecordResponse VerificationRecord { get; init; } = new();
    public bool CanServeBrandedLinks { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    public DateTime? VerificationRequestedAtUtc { get; init; }
    public DateTime? LastVerificationAttemptAtUtc { get; init; }
    public DateTime? VerifiedAtUtc { get; init; }
    public DateTime? DisabledAtUtc { get; init; }
    public CustomDomainVerificationFailureResponse? VerificationFailure { get; init; }
}

public sealed class CustomDomainVerificationRecordResponse
{
    public string Type { get; init; } = "TXT";
    public string Name { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}

public sealed class CustomDomainVerificationFailureResponse
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
