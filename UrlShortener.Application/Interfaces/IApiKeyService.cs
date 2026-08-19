using UrlShortener.Application.Dtos;

namespace UrlShortener.Application.Interfaces;

public interface IApiKeyService
{
    Task<ApiKeyCreationResponse> CreateAsync(CreateApiKeyRequest request, CancellationToken ct);
    Task<IReadOnlyList<ApiKeyResponse>> ListAsync(CancellationToken ct);
    Task RevokeAsync(Guid apiKeyId, CancellationToken ct);
    Task<ApiKeyCreationResponse> RotateAsync(Guid apiKeyId, CancellationToken ct);
}
