using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UrlShortener.Application.Authentication;
using UrlShortener.Application.Interfaces;
using UrlShortener.Infrastructure.Configuration;

namespace UrlShortener.Api.Security;

public sealed class JwtAccessTokenIssuer : IAccessTokenIssuer
{
    public const string SessionIdClaim = "sid";
    public const string SecurityVersionClaim = "sst";

    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IdentitySecurityOptions _options;
    private readonly SigningCredentials _signingCredentials;

    public JwtAccessTokenIssuer(
        IDateTimeProvider dateTimeProvider,
        IOptions<IdentitySecurityOptions> options)
    {
        _dateTimeProvider = dateTimeProvider;
        _options = options.Value;
        var signingKey = Convert.FromBase64String(_options.JwtSigningKeyBase64);
        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(signingKey),
            SecurityAlgorithms.HmacSha256);
    }

    public IssuedAccessToken Issue(Guid userId, Guid sessionId, string securityStamp)
    {
        var nowUtc = _dateTimeProvider.UtcNow;
        var expiresAtUtc = nowUtc.AddMinutes(_options.AccessTokenLifetimeMinutes);
        var securityVersion = Convert.ToBase64String(
                SHA256.HashData(Encoding.UTF8.GetBytes(securityStamp)))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var token = new JwtSecurityToken(
            issuer: _options.JwtIssuer,
            audience: _options.JwtAudience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(SessionIdClaim, sessionId.ToString()),
                new Claim(SecurityVersionClaim, securityVersion),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ],
            notBefore: nowUtc,
            expires: expiresAtUtc,
            signingCredentials: _signingCredentials);

        return new IssuedAccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
