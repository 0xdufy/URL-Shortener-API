using System.Security.Cryptography;
using UrlShortener.Application.ApiKeys;
using UrlShortener.Application.Interfaces;

namespace UrlShortener.Infrastructure.Security;

public sealed class ApiKeyCredentialGenerator : IApiKeyCredentialGenerator
{
    public const int LookupIdentifierEntropyBytes = 16;
    public const int SecretEntropyBytes = 32;
    private const string PrefixMarker = "usk_";

    public GeneratedApiKeyCredential Generate()
    {
        var lookupIdentifier = Base64UrlEncode(RandomNumberGenerator.GetBytes(LookupIdentifierEntropyBytes));
        var secretBytes = RandomNumberGenerator.GetBytes(SecretEntropyBytes);
        var secret = Base64UrlEncode(secretBytes);
        var keyPrefix = PrefixMarker + lookupIdentifier;

        return new GeneratedApiKeyCredential(
            keyPrefix,
            SHA256.HashData(secretBytes),
            keyPrefix + "." + secret);
    }

    private static string Base64UrlEncode(byte[] value) =>
        Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
