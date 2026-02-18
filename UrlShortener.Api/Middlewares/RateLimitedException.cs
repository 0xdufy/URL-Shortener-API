namespace UrlShortener.Api.Middlewares;

public class RateLimitedException : Exception
{
    public int RetryAfterSeconds { get; }

    public RateLimitedException(string message, int retryAfterSeconds) : base(message)
    {
        RetryAfterSeconds = retryAfterSeconds;
    }
}
