namespace UrlShortener.Application.Interfaces;

public sealed record ShortUrlContractSettings(
    string PublicBaseUrl,
    string DefaultHost,
    string CustomDomainScheme);
