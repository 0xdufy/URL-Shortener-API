namespace UrlShortener.Application.Interfaces;

public interface IShortCodeGenerator
{
    string Generate(int length = 6);
}
