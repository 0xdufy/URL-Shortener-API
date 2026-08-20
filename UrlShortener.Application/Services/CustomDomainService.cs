using UrlShortener.Application.CustomDomains;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Exceptions;
using UrlShortener.Application.Interfaces;
using UrlShortener.Domain.CustomDomains;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Services;

public sealed class CustomDomainService : ICustomDomainService
{
    private readonly ICustomDomainRepository _repository;
    private readonly ICustomDomainOwnershipVerifier _ownershipVerifier;
    private readonly IVerificationTokenGenerator _tokenGenerator;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly CustomDomainPolicySettings _policy;
    private readonly IShortUrlRepository _shortUrlRepository;
    private readonly IShortUrlCache _shortUrlCache;

    public CustomDomainService(
        ICustomDomainRepository repository,
        ICustomDomainOwnershipVerifier ownershipVerifier,
        IVerificationTokenGenerator tokenGenerator,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider,
        CustomDomainPolicySettings policy,
        IShortUrlRepository shortUrlRepository,
        IShortUrlCache shortUrlCache)
    {
        _repository = repository;
        _ownershipVerifier = ownershipVerifier;
        _tokenGenerator = tokenGenerator;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
        _policy = policy;
        _shortUrlRepository = shortUrlRepository;
        _shortUrlCache = shortUrlCache;
    }

    public async Task<CustomDomainResponse> RegisterAsync(RegisterCustomDomainRequest request, CancellationToken ct)
    {
        var host = NormalizeHost(request.Host);
        EnsureNotReserved(host);
        var nowUtc = _dateTimeProvider.UtcNow;
        var customDomain = new CustomDomain(Guid.NewGuid(), RequireOwnerId(), host, _tokenGenerator.Generate(), nowUtc);

        if (await _repository.TryCreateAsync(customDomain, ct) == CustomDomainCreateOutcome.HostConflict)
        {
            throw new CustomDomainConflictException();
        }

        return ToResponse(customDomain);
    }

    public async Task<IReadOnlyList<CustomDomainResponse>> ListAsync(CancellationToken ct) =>
        (await _repository.ListOwnedAsync(RequireOwnerId(), ct)).Select(ToResponse).ToList();

    public async Task<CustomDomainResponse> RequestVerificationAsync(Guid customDomainId, CancellationToken ct)
    {
        var customDomain = await RequireOwnedAsync(customDomainId, ct);
        var outcome = await _repository.RequestVerificationAsync(
            customDomainId,
            RequireOwnerId(),
            _tokenGenerator.Generate(),
            _dateTimeProvider.UtcNow,
            ct);
        EnsureMutationSucceeded(outcome);
        await InvalidateDomainLinksAsync(customDomain, ct);
        return ToResponse(await RequireOwnedAsync(customDomainId, ct));
    }

    public async Task<CustomDomainResponse> CheckVerificationAsync(Guid customDomainId, CancellationToken ct)
    {
        var ownerId = RequireOwnerId();
        var customDomain = await _repository.GetOwnedAsync(customDomainId, ownerId, ct)
            ?? throw new NotFoundException("Custom domain not found.");

        if (customDomain.Status == CustomDomainStatus.Disabled)
        {
            throw new CustomDomainStateConflictException();
        }

        if (customDomain.Status == CustomDomainStatus.Verified)
        {
            return ToResponse(customDomain);
        }

        var evidence = await _ownershipVerifier.VerifyTxtRecordAsync(
            GetVerificationRecordName(customDomain.NormalizedHost),
            GetVerificationRecordValue(customDomain.VerificationToken),
            ct);

        var outcome = await _repository.RecordVerificationAsync(
            customDomainId,
            ownerId,
            customDomain.VerificationToken,
            evidence,
            _dateTimeProvider.UtcNow,
            ct);
        EnsureMutationSucceeded(outcome);
        return ToResponse(await RequireOwnedAsync(customDomainId, ct));
    }

    public async Task<CustomDomainResponse> DisableAsync(Guid customDomainId, CancellationToken ct)
    {
        var customDomain = await RequireOwnedAsync(customDomainId, ct);
        var outcome = await _repository.DisableAsync(
            customDomainId,
            RequireOwnerId(),
            _dateTimeProvider.UtcNow,
            ct);
        EnsureMutationSucceeded(outcome);
        await InvalidateDomainLinksAsync(customDomain, ct);
        return ToResponse(await RequireOwnedAsync(customDomainId, ct));
    }

    private async Task InvalidateDomainLinksAsync(CustomDomain customDomain, CancellationToken ct)
    {
        var shortCodes = await _shortUrlRepository.ListShortCodesForCustomDomainAsync(customDomain.Id, ct);
        foreach (var shortCode in shortCodes)
        {
            await _shortUrlCache.RemoveAsync(customDomain.NormalizedHost, shortCode, ct);
        }
    }

    private async Task<CustomDomain> RequireOwnedAsync(Guid id, CancellationToken ct) =>
        await _repository.GetOwnedAsync(id, RequireOwnerId(), ct)
            ?? throw new NotFoundException("Custom domain not found.");

    private static void EnsureMutationSucceeded(CustomDomainMutationOutcome outcome)
    {
        if (outcome == CustomDomainMutationOutcome.NotFound)
        {
            throw new NotFoundException("Custom domain not found.");
        }

        if (outcome == CustomDomainMutationOutcome.StateChanged)
        {
            throw new CustomDomainStateConflictException();
        }
    }

    private Guid RequireOwnerId()
    {
        if (_currentUserContext.UserId is not { } ownerId || ownerId == Guid.Empty)
        {
            throw new AuthenticatedUserRequiredException();
        }

        return ownerId;
    }

    private string NormalizeHost(string host)
    {
        if (!CustomDomainHostNormalizer.TryNormalize(host, out var normalizedHost, out _) ||
            GetVerificationRecordName(normalizedHost).Length > 253)
        {
            throw new ArgumentException("The custom-domain host is invalid.", nameof(host));
        }

        return normalizedHost;
    }

    private void EnsureNotReserved(string normalizedHost)
    {
        if (_policy.IsReserved(normalizedHost))
        {
            throw new CustomDomainReservedException();
        }
    }

    private CustomDomainResponse ToResponse(CustomDomain customDomain) => new()
    {
        Id = customDomain.Id,
        Host = customDomain.NormalizedHost,
        Status = customDomain.Status.ToString().ToLowerInvariant(),
        VerificationMethod = "dns_txt",
        VerificationRecord = new CustomDomainVerificationRecordResponse
        {
            Name = GetVerificationRecordName(customDomain.NormalizedHost),
            Value = GetVerificationRecordValue(customDomain.VerificationToken)
        },
        CanServeBrandedLinks = customDomain.CanServeBrandedLinks,
        CreatedAtUtc = AsUtc(customDomain.CreatedAtUtc),
        UpdatedAtUtc = AsUtc(customDomain.UpdatedAtUtc),
        VerificationRequestedAtUtc = AsUtc(customDomain.VerificationRequestedAtUtc),
        LastVerificationAttemptAtUtc = AsUtc(customDomain.LastVerificationAttemptAtUtc),
        VerifiedAtUtc = AsUtc(customDomain.VerifiedAtUtc),
        DisabledAtUtc = AsUtc(customDomain.DisabledAtUtc),
        VerificationFailure = customDomain.FailureCode == null ? null : new CustomDomainVerificationFailureResponse
        {
            Code = customDomain.FailureCode,
            Message = customDomain.FailureMessage ?? "Domain verification failed."
        }
    };

    private string GetVerificationRecordName(string host) => $"{_policy.VerificationRecordLabel}.{host}";
    private string GetVerificationRecordValue(string token) => _policy.VerificationValuePrefix + token;

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static DateTime? AsUtc(DateTime? value) => value.HasValue ? AsUtc(value.Value) : null;
}
