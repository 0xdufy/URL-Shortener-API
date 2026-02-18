using System.Security.Cryptography;
using UrlShortener.Application.Interfaces;

namespace UrlShortener.Infrastructure.Services;

public class ShortCodeGenerator : IShortCodeGenerator
{
    private const string Charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public string Generate(int length = 6)
    {
        var bytes = RandomNumberGenerator.GetBytes(length);
        var chars = new char[length];

        for (var i = 0; i < length; i++)
        {
            chars[i] = Charset[bytes[i] % Charset.Length];
        }

        return new string(chars);
    }
}
