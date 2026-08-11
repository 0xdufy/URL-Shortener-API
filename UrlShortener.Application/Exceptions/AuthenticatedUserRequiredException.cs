namespace UrlShortener.Application.Exceptions;

public sealed class AuthenticatedUserRequiredException : Exception
{
    public AuthenticatedUserRequiredException()
        : base("An authenticated user is required for this operation.")
    {
    }
}
