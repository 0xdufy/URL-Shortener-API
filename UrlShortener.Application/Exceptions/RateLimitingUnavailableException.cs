namespace UrlShortener.Application.Exceptions;

public sealed class RateLimitingUnavailableException : Exception
{
    public RateLimitingUnavailableException(Exception innerException)
        : base("Distributed rate limiting is unavailable.", innerException)
    {
    }
}
