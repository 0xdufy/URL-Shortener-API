namespace UrlShortener.Application.Exceptions;

public sealed class RestoreNotDeletedException : Exception
{
    public RestoreNotDeletedException(string message) : base(message)
    {
    }
}
