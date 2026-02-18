namespace UrlShortener.Application.Exceptions;

public class ShortCodeGenerationFailedException : Exception
{
    public ShortCodeGenerationFailedException(string message) : base(message)
    {
    }
}
