using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using UrlShortener.Api.Models;
using UrlShortener.Application.ApiKeys;
using UrlShortener.Application.Interfaces;
using UrlShortener.Domain.Identity;

namespace UrlShortener.Api.Security;

public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const int KeyPrefixLength = 26;
    private const int EncodedSecretLength = 43;
    private static readonly byte[] MissingKeyHash = new byte[32];
    private static readonly TimeSpan LastUsedWriteInterval = TimeSpan.FromMinutes(5);

    private readonly IApiKeyAuthenticationRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyAuthenticationRepository repository,
        IDateTimeProvider dateTimeProvider)
        : base(options, logger, encoder)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!TryReadCredential(Request, out var keyPrefix, out var suppliedSecretHash))
        {
            return AuthenticateResult.NoResult();
        }

        var record = await _repository.FindByPrefixAsync(keyPrefix, Context.RequestAborted);
        var expectedSecretHash = record?.SecretHash is { Length: 32 }
            ? record.SecretHash
            : MissingKeyHash;
        var secretMatches = CryptographicOperations.FixedTimeEquals(suppliedSecretHash, expectedSecretHash);
        CryptographicOperations.ZeroMemory(suppliedSecretHash);

        var utcNow = _dateTimeProvider.UtcNow;
        if (!secretMatches ||
            record == null ||
            record.RevokedAtUtc.HasValue ||
            (record.ExpiresAtUtc.HasValue && record.ExpiresAtUtc.Value <= utcNow) ||
            record.OwnerStatus != UserAccountStatus.Active)
        {
            return AuthenticateResult.Fail("The API-key credential is invalid.");
        }

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, record.OwnerId.ToString("D")),
            new(ApiKeyAuthenticationDefaults.ApiKeyIdClaim, record.Id.ToString("D"))
        };
        claims.AddRange(ApiKeyScopeNames.ToNames(record.Scopes)
            .Select(scope => new Claim(ApiKeyAuthenticationDefaults.ScopeClaim, scope)));

        var identity = new ClaimsIdentity(claims, Scheme.Name, JwtRegisteredClaimNames.Sub, null);
        var principal = new ClaimsPrincipal(identity);

        if (!record.LastUsedAtUtc.HasValue || record.LastUsedAtUtc.Value <= utcNow.Subtract(LastUsedWriteInterval))
        {
            await _repository.RecordUseIfStaleAsync(
                record.Id,
                utcNow,
                LastUsedWriteInterval,
                Context.RequestAborted);
        }

        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties) =>
        WriteErrorAsync(
            StatusCodes.Status401Unauthorized,
            "AUTHENTICATION_REQUIRED",
            "A valid API key is required.");

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties) =>
        WriteErrorAsync(
            StatusCodes.Status403Forbidden,
            "FORBIDDEN",
            "The API key does not grant the scope required for this operation.");

    private Task WriteErrorAsync(int statusCode, string code, string message)
    {
        if (Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        Response.StatusCode = statusCode;
        Response.ContentType = "application/json";
        Response.Headers.CacheControl = "no-store";
        return Response.WriteAsJsonAsync(
            ApiErrorFactory.Create(Context, code, message),
            Context.RequestAborted);
    }

    private static bool TryReadCredential(
        HttpRequest request,
        out string keyPrefix,
        out byte[] suppliedSecretHash)
    {
        keyPrefix = string.Empty;
        suppliedSecretHash = [];

        var authorization = request.Headers.Authorization;
        if (authorization.Count != 1)
        {
            return false;
        }

        var value = authorization[0];
        if (value == null ||
            !value.StartsWith(ApiKeyAuthenticationDefaults.AuthorizationHeaderPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var credential = value[ApiKeyAuthenticationDefaults.AuthorizationHeaderPrefix.Length..];
        if (credential.Length != KeyPrefixLength + 1 + EncodedSecretLength ||
            credential[KeyPrefixLength] != '.')
        {
            return false;
        }

        var prefix = credential[..KeyPrefixLength];
        if (!prefix.StartsWith("usk_", StringComparison.Ordinal) ||
            !prefix[4..].All(IsBase64UrlCharacter))
        {
            return false;
        }

        try
        {
            var secretBytes = WebEncoders.Base64UrlDecode(credential[(KeyPrefixLength + 1)..]);
            if (secretBytes.Length != 32)
            {
                CryptographicOperations.ZeroMemory(secretBytes);
                return false;
            }

            suppliedSecretHash = SHA256.HashData(secretBytes);
            CryptographicOperations.ZeroMemory(secretBytes);
            keyPrefix = prefix;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool IsBase64UrlCharacter(char value) =>
        char.IsAsciiLetterOrDigit(value) || value is '-' or '_';
}
