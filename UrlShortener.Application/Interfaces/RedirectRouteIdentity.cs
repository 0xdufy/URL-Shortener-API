namespace UrlShortener.Application.Interfaces;

public sealed record RedirectRouteIdentity(
    string Host,
    string ShortCode,
    bool IsDefaultHost);
