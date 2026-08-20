using UrlShortener.Domain.CustomDomains;

namespace UrlShortener.Domain.Entities;

public sealed class CustomDomain
{
    private CustomDomain()
    {
    }

    public CustomDomain(
        Guid id,
        Guid ownerId,
        string normalizedHost,
        string verificationToken,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A custom domain requires a non-empty ID.", nameof(id));
        }

        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("A custom domain requires a non-empty owner ID.", nameof(ownerId));
        }

        ValidateHost(normalizedHost);
        ValidateToken(verificationToken);
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        Id = id;
        OwnerId = ownerId;
        NormalizedHost = normalizedHost;
        VerificationMethod = CustomDomainVerificationMethod.DnsTxt;
        VerificationToken = verificationToken;
        Status = CustomDomainStatus.Pending;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        VerificationRequestedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public string NormalizedHost { get; private set; } = string.Empty;
    public CustomDomainStatus Status { get; private set; }
    public CustomDomainVerificationMethod VerificationMethod { get; private set; }
    public string VerificationToken { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public DateTime? VerificationRequestedAtUtc { get; private set; }
    public DateTime? LastVerificationAttemptAtUtc { get; private set; }
    public DateTime? VerifiedAtUtc { get; private set; }
    public DateTime? DisabledAtUtc { get; private set; }
    public string? FailureCode { get; private set; }
    public string? FailureMessage { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public ICollection<ShortUrl> ShortUrls { get; private set; } = [];

    public bool CanServeBrandedLinks => Status == CustomDomainStatus.Verified;

    public void RequestVerification(string verificationToken, DateTime requestedAtUtc)
    {
        ValidateToken(verificationToken);
        EnsureUtc(requestedAtUtc, nameof(requestedAtUtc));

        VerificationToken = verificationToken;
        Status = CustomDomainStatus.Pending;
        VerificationRequestedAtUtc = requestedAtUtc;
        LastVerificationAttemptAtUtc = null;
        VerifiedAtUtc = null;
        DisabledAtUtc = null;
        FailureCode = null;
        FailureMessage = null;
        UpdatedAtUtc = requestedAtUtc;
    }

    public void RecordVerificationSuccess(DateTime verifiedAtUtc)
    {
        EnsureUtc(verifiedAtUtc, nameof(verifiedAtUtc));
        EnsureVerificationAllowed();

        Status = CustomDomainStatus.Verified;
        LastVerificationAttemptAtUtc = verifiedAtUtc;
        VerifiedAtUtc = verifiedAtUtc;
        FailureCode = null;
        FailureMessage = null;
        UpdatedAtUtc = verifiedAtUtc;
    }

    public void RecordVerificationFailure(DateTime attemptedAtUtc, string code, string message)
    {
        EnsureUtc(attemptedAtUtc, nameof(attemptedAtUtc));
        EnsureVerificationAllowed();
        if (string.IsNullOrWhiteSpace(code) || code.Length > 64 ||
            string.IsNullOrWhiteSpace(message) || message.Length > 256)
        {
            throw new ArgumentException("Verification failure metadata is invalid.", nameof(code));
        }

        Status = CustomDomainStatus.Failed;
        LastVerificationAttemptAtUtc = attemptedAtUtc;
        VerifiedAtUtc = null;
        FailureCode = code;
        FailureMessage = message;
        UpdatedAtUtc = attemptedAtUtc;
    }

    public void Disable(DateTime disabledAtUtc)
    {
        EnsureUtc(disabledAtUtc, nameof(disabledAtUtc));
        Status = CustomDomainStatus.Disabled;
        DisabledAtUtc = disabledAtUtc;
        UpdatedAtUtc = disabledAtUtc;
    }

    private void EnsureVerificationAllowed()
    {
        if (Status == CustomDomainStatus.Disabled)
        {
            throw new InvalidOperationException("A disabled domain cannot be verified until verification is requested again.");
        }
    }

    private static void ValidateHost(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 253 || value != value.ToLowerInvariant())
        {
            throw new ArgumentException("The normalized host is invalid.", nameof(value));
        }
    }

    private static void ValidateToken(string value)
    {
        if (value.Length != 43 || !value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_'))
        {
            throw new ArgumentException("The verification token is invalid.", nameof(value));
        }
    }

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Custom-domain timestamps must be UTC.", parameterName);
        }
    }
}
