using UrlShortener.Application.Dtos;

namespace UrlShortener.Application.Interfaces;

public interface ICustomDomainService
{
    Task<CustomDomainResponse> RegisterAsync(RegisterCustomDomainRequest request, CancellationToken ct);
    Task<IReadOnlyList<CustomDomainResponse>> ListAsync(CancellationToken ct);
    Task<CustomDomainResponse> RequestVerificationAsync(Guid customDomainId, CancellationToken ct);
    Task<CustomDomainResponse> CheckVerificationAsync(Guid customDomainId, CancellationToken ct);
    Task<CustomDomainResponse> DisableAsync(Guid customDomainId, CancellationToken ct);
}
