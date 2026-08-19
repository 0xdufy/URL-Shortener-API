using UrlShortener.Application.ApiKeys;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Exceptions;
using UrlShortener.Application.Interfaces;
using UrlShortener.Domain.ApiKeys;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Services;

public sealed class ApiKeyService : IApiKeyService
{
    private readonly IApiKeyRepository _repository;
    private readonly IApiKeyCredentialGenerator _credentialGenerator;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ApiKeyManagementSettings _settings;

    public ApiKeyService(
        IApiKeyRepository repository,
        IApiKeyCredentialGenerator credentialGenerator,
        ICurrentUserContext currentUserContext,
        IDateTimeProvider dateTimeProvider,
        ApiKeyManagementSettings settings)
    {
        _repository = repository;
        _credentialGenerator = credentialGenerator;
        _currentUserContext = currentUserContext;
        _dateTimeProvider = dateTimeProvider;
        _settings = settings;
    }

    public async Task<ApiKeyCreationResponse> CreateAsync(CreateApiKeyRequest request, CancellationToken ct)
    {
        var ownerId = RequireCurrentUserId();
        var nowUtc = _dateTimeProvider.UtcNow;
        var credential = _credentialGenerator.Generate();
        var apiKey = new ApiKey(
            Guid.NewGuid(),
            ownerId,
            request.Name,
            credential.KeyPrefix,
            credential.SecretHash,
            ParseScopes(request.Scopes),
            nowUtc,
            request.ExpiresAtUtc);

        var outcome = await _repository.TryCreateAsync(
            apiKey,
            nowUtc,
            _settings.MaximumActiveKeysPerUser,
            ct);

        if (outcome == ApiKeyCreationOutcome.ActiveKeyLimitReached)
        {
            throw new ApiKeyLimitExceededException();
        }

        return ToCreationResponse(apiKey, credential.PlaintextKey, nowUtc);
    }

    public async Task<IReadOnlyList<ApiKeyResponse>> ListAsync(CancellationToken ct)
    {
        var apiKeys = await _repository.ListOwnedAsync(RequireCurrentUserId(), ct);
        var nowUtc = _dateTimeProvider.UtcNow;
        return apiKeys.Select(apiKey => ToResponse(apiKey, nowUtc)).ToList();
    }

    public async Task RevokeAsync(Guid apiKeyId, CancellationToken ct)
    {
        var outcome = await _repository.TryRevokeAsync(
            apiKeyId,
            RequireCurrentUserId(),
            _dateTimeProvider.UtcNow,
            ct);

        if (outcome == ApiKeyRevocationOutcome.NotFound)
        {
            throw new NotFoundException("API key not found.");
        }

        if (outcome == ApiKeyRevocationOutcome.AlreadyRevoked)
        {
            throw new ApiKeyStateConflictException();
        }
    }

    public async Task<ApiKeyCreationResponse> RotateAsync(Guid apiKeyId, CancellationToken ct)
    {
        var ownerId = RequireCurrentUserId();
        var existing = await _repository.GetOwnedAsync(apiKeyId, ownerId, ct);
        if (existing == null)
        {
            throw new NotFoundException("API key not found.");
        }

        var nowUtc = _dateTimeProvider.UtcNow;
        if (!existing.IsActiveAt(nowUtc))
        {
            throw new ApiKeyStateConflictException();
        }

        var credential = _credentialGenerator.Generate();
        var replacement = new ApiKey(
            Guid.NewGuid(),
            ownerId,
            existing.Name,
            credential.KeyPrefix,
            credential.SecretHash,
            existing.Scopes,
            nowUtc,
            AsUtc(existing.ExpiresAtUtc));

        var outcome = await _repository.TryRotateAsync(apiKeyId, ownerId, replacement, nowUtc, ct);
        if (outcome == ApiKeyRotationOutcome.NotFound)
        {
            throw new NotFoundException("API key not found.");
        }

        if (outcome == ApiKeyRotationOutcome.NotActive)
        {
            throw new ApiKeyStateConflictException();
        }

        return ToCreationResponse(replacement, credential.PlaintextKey, nowUtc);
    }

    private Guid RequireCurrentUserId()
    {
        var userId = _currentUserContext.UserId;
        if (!userId.HasValue || userId.Value == Guid.Empty)
        {
            throw new AuthenticatedUserRequiredException();
        }

        return userId.Value;
    }

    private static ApiKeyScope ParseScopes(IEnumerable<string> names)
    {
        var scopes = ApiKeyScope.None;
        foreach (var name in names)
        {
            if (!ApiKeyScopeNames.TryParse(name, out var scope))
            {
                throw new ArgumentException("The API-key scope collection contains an unsupported value.", nameof(names));
            }

            scopes |= scope;
        }

        return scopes;
    }

    private static ApiKeyCreationResponse ToCreationResponse(ApiKey apiKey, string plaintextKey, DateTime utcNow) =>
        new()
        {
            ApiKey = ToResponse(apiKey, utcNow),
            Key = plaintextKey
        };

    private static ApiKeyResponse ToResponse(ApiKey apiKey, DateTime utcNow) =>
        new()
        {
            Id = apiKey.Id,
            Name = apiKey.Name,
            Prefix = apiKey.KeyPrefix,
            Scopes = ApiKeyScopeNames.ToNames(apiKey.Scopes),
            CreatedAtUtc = AsUtc(apiKey.CreatedAtUtc),
            ExpiresAtUtc = AsUtc(apiKey.ExpiresAtUtc),
            LastUsedAtUtc = AsUtc(apiKey.LastUsedAtUtc),
            RevokedAtUtc = AsUtc(apiKey.RevokedAtUtc),
            State = GetState(apiKey, utcNow),
            ReplacedByApiKeyId = apiKey.ReplacedByApiKeyId
        };

    private static string GetState(ApiKey apiKey, DateTime utcNow)
    {
        if (apiKey.RevokedAtUtc.HasValue)
        {
            return "revoked";
        }

        return apiKey.ExpiresAtUtc.HasValue && apiKey.ExpiresAtUtc.Value <= utcNow
            ? "expired"
            : "active";
    }

    private static DateTime AsUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static DateTime? AsUtc(DateTime? value) => value.HasValue ? AsUtc(value.Value) : null;
}
