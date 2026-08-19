using UrlShortener.Application.Dtos;
using UrlShortener.Application.Exceptions;
using UrlShortener.Application.Interfaces;

namespace UrlShortener.Application.Services;

public sealed class UnavailableApiKeyService : IApiKeyService
{
    public Task<ApiKeyCreationResponse> CreateAsync(CreateApiKeyRequest request, CancellationToken ct) =>
        Task.FromException<ApiKeyCreationResponse>(new ApiKeyManagementUnavailableException());

    public Task<IReadOnlyList<ApiKeyResponse>> ListAsync(CancellationToken ct) =>
        Task.FromException<IReadOnlyList<ApiKeyResponse>>(new ApiKeyManagementUnavailableException());

    public Task RevokeAsync(Guid apiKeyId, CancellationToken ct) =>
        Task.FromException(new ApiKeyManagementUnavailableException());

    public Task<ApiKeyCreationResponse> RotateAsync(Guid apiKeyId, CancellationToken ct) =>
        Task.FromException<ApiKeyCreationResponse>(new ApiKeyManagementUnavailableException());
}
