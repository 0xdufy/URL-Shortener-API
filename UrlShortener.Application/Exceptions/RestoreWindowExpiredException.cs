namespace UrlShortener.Application.Exceptions;

public sealed class RestoreWindowExpiredException : Exception
{
    public RestoreWindowExpiredException(string message) : base(message)
    {
    }
}
