using UrlShortener.Application.Authentication;
using UrlShortener.Application.Dtos;

namespace UrlShortener.Application.Interfaces;

public interface IAuthenticationService
{
    Task<IssuedAuthenticationSession> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<IssuedAuthenticationSession> SignInAsync(SignInRequest request, CancellationToken cancellationToken);
    Task<IssuedAuthenticationSession> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    Task SignOutAsync(string? refreshToken, CancellationToken cancellationToken);
    Task<CurrentAuthenticationSession> GetCurrentAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);
}
