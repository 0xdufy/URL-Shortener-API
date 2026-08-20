using System.Security.Cryptography;
using UrlShortener.Application.Interfaces;

namespace UrlShortener.Infrastructure.Security;

public sealed class VerificationTokenGenerator : IVerificationTokenGenerator
{
    public string Generate() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');
}
