using UrlShortener.Application.Authentication;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Exceptions;
using UrlShortener.Application.Interfaces;

namespace UrlShortener.Infrastructure.Identity;

public sealed class UnavailableAuthenticationService : IAuthenticationService
{
    public Task<IssuedAuthenticationSession> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken) =>
        Task.FromException<IssuedAuthenticationSession>(new AuthenticationPersistenceUnavailableException());

    public Task<IssuedAuthenticationSession> SignInAsync(SignInRequest request, CancellationToken cancellationToken) =>
        Task.FromException<IssuedAuthenticationSession>(new AuthenticationPersistenceUnavailableException());

    public Task<IssuedAuthenticationSession> RefreshAsync(string refreshToken, CancellationToken cancellationToken) =>
        Task.FromException<IssuedAuthenticationSession>(new AuthenticationPersistenceUnavailableException());

    public Task SignOutAsync(string? refreshToken, CancellationToken cancellationToken) =>
        Task.FromException(new AuthenticationPersistenceUnavailableException());

    public Task<CurrentAuthenticationSession> GetCurrentAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken) =>
        Task.FromException<CurrentAuthenticationSession>(new AuthenticationPersistenceUnavailableException());
}
