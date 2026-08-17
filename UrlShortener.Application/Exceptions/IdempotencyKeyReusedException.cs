namespace UrlShortener.Application.Exceptions;

public sealed class IdempotencyKeyReusedException : Exception
{
    public IdempotencyKeyReusedException()
        : base("The idempotency key was already used with different request content.")
    {
    }
}
