namespace UrlShortener.Application.Authentication;

public sealed record AuthenticatedUser(Guid Id, string Email, DateTime CreatedAtUtc);
