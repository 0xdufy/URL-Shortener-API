namespace UrlShortener.Application.Exceptions;

public class AliasConflictException : Exception
{
    public AliasConflictException(string message) : base(message)
    {
    }
}
