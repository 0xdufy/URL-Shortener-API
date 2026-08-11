using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UrlShortener.Application.Authentication;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Exceptions;
using UrlShortener.Application.Interfaces;
using UrlShortener.Domain.Identity;
using UrlShortener.Infrastructure.Configuration;
using UrlShortener.Infrastructure.Persistence;

namespace UrlShortener.Infrastructure.Identity;

public sealed class IdentityAuthenticationService : IAuthenticationService
{
    private const string RotatedReason = "Rotated";
    private const string ReuseDetectedReason = "Refresh token reuse detected";
    private const string SignedOutReason = "Signed out";
    private const string ExpiredReason = "Expired";
    private const string AccountStateReason = "Account security state changed";

    private static readonly ApplicationUser DummyUser = new();
    private static readonly string DummyPasswordHash =
        new PasswordHasher<ApplicationUser>().HashPassword(DummyUser, "Dummy-password-never-used-7!");

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly AppDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAccessTokenIssuer _accessTokenIssuer;
    private readonly IdentitySecurityOptions _options;

    public IdentityAuthenticationService(
        UserManager<ApplicationUser> userManager,
        IPasswordHasher<ApplicationUser> passwordHasher,
        AppDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        IAccessTokenIssuer accessTokenIssuer,
        IOptions<IdentitySecurityOptions> options)
    {
        _userManager = userManager;
        _passwordHasher = passwordHasher;
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
        _accessTokenIssuer = accessTokenIssuer;
        _options = options.Value;
    }

    public async Task<IssuedAuthenticationSession> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (!_options.PublicRegistrationEnabled)
        {
            throw new AccountUnavailableException();
        }

        var nowUtc = _dateTimeProvider.UtcNow;
        var email = request.Email.Trim();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            Status = UserAccountStatus.Active,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };

        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(error =>
                        error.Code.StartsWith("Password", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new PasswordPolicyException();
                }

                throw new AccountUnavailableException();
            }

            var session = await CreateSessionAsync(user, Guid.NewGuid(), nowUtc, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return session;
        });
    }

    public async Task<IssuedAuthenticationSession> SignInAsync(
        SignInRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            _ = _passwordHasher.VerifyHashedPassword(DummyUser, DummyPasswordHash, request.Password);
            throw new InvalidCredentialsException();
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            throw new InvalidCredentialsException();
        }

        var passwordIsValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordIsValid)
        {
            if (_userManager.SupportsUserLockout && await _userManager.GetLockoutEnabledAsync(user))
            {
                await _userManager.AccessFailedAsync(user);
            }

            throw new InvalidCredentialsException();
        }

        if (user.Status != UserAccountStatus.Active)
        {
            throw new InvalidCredentialsException();
        }

        if (_userManager.SupportsUserLockout && await _userManager.GetAccessFailedCountAsync(user) > 0)
        {
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        return await CreateSessionAsync(
            user,
            Guid.NewGuid(),
            _dateTimeProvider.UtcNow,
            cancellationToken);
    }

    public async Task<IssuedAuthenticationSession> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken) || refreshToken.Length > 256)
        {
            throw new InvalidSessionException();
        }

        var tokenHash = Hash(refreshToken);
        var session = await _dbContext.RefreshSessions
            .Include(item => item.User)
            .SingleOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);

        if (session is null)
        {
            throw new InvalidSessionException();
        }

        var nowUtc = _dateTimeProvider.UtcNow;
        if (session.RevokedAtUtc.HasValue)
        {
            await RevokeFamilyAsync(session.FamilyId, ReuseDetectedReason, nowUtc, cancellationToken);
            throw new InvalidSessionException();
        }

        if (session.ExpiresAtUtc <= nowUtc || session.AbsoluteExpiresAtUtc <= nowUtc)
        {
            session.RevokedAtUtc = nowUtc;
            session.RevocationReason = ExpiredReason;
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new InvalidSessionException();
        }

        var currentSecurityStamp = await _userManager.GetSecurityStampAsync(session.User);
        var currentSecurityStampHash = Hash(currentSecurityStamp);
        if (session.User.Status != UserAccountStatus.Active ||
            !CryptographicOperations.FixedTimeEquals(session.SecurityStampHash, currentSecurityStampHash))
        {
            await RevokeFamilyAsync(session.FamilyId, AccountStateReason, nowUtc, cancellationToken);
            throw new InvalidSessionException();
        }

        var replacementToken = CreateRefreshToken();
        var replacementExpiry = nowUtc.AddDays(_options.RefreshTokenLifetimeDays);
        if (replacementExpiry > session.AbsoluteExpiresAtUtc)
        {
            replacementExpiry = session.AbsoluteExpiresAtUtc;
        }

        var replacement = new RefreshSession
        {
            Id = Guid.NewGuid(),
            UserId = session.UserId,
            FamilyId = session.FamilyId,
            TokenHash = Hash(replacementToken),
            SecurityStampHash = currentSecurityStampHash,
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = replacementExpiry,
            AbsoluteExpiresAtUtc = session.AbsoluteExpiresAtUtc
        };

        session.LastUsedAtUtc = nowUtc;
        session.RevokedAtUtc = nowUtc;
        session.RevocationReason = RotatedReason;
        session.ReplacedBySessionId = replacement.Id;
        _dbContext.RefreshSessions.Add(replacement);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _dbContext.ChangeTracker.Clear();
            await RevokeFamilyAsync(session.FamilyId, ReuseDetectedReason, nowUtc, cancellationToken);
            throw new InvalidSessionException();
        }

        return IssueSession(session.User, replacement, replacementToken, currentSecurityStamp);
    }

    public async Task SignOutAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken) || refreshToken.Length > 256)
        {
            return;
        }

        var tokenHash = Hash(refreshToken);
        var session = await _dbContext.RefreshSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);

        if (session is not null)
        {
            await RevokeFamilyAsync(
                session.FamilyId,
                SignedOutReason,
                _dateTimeProvider.UtcNow,
                cancellationToken);
        }
    }

    public async Task<CurrentAuthenticationSession> GetCurrentAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        var session = await _dbContext.RefreshSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == sessionId && item.UserId == userId, cancellationToken);

        if (user is null || session is null || user.Status != UserAccountStatus.Active)
        {
            throw new InvalidSessionException();
        }

        return new CurrentAuthenticationSession(
            session.Id,
            session.CreatedAtUtc,
            session.ExpiresAtUtc,
            session.RevokedAtUtc.HasValue,
            ToAuthenticatedUser(user));
    }

    private async Task<IssuedAuthenticationSession> CreateSessionAsync(
        ApplicationUser user,
        Guid familyId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var refreshToken = CreateRefreshToken();
        var securityStamp = await _userManager.GetSecurityStampAsync(user);
        var session = new RefreshSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FamilyId = familyId,
            TokenHash = Hash(refreshToken),
            SecurityStampHash = Hash(securityStamp),
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = nowUtc.AddDays(_options.RefreshTokenLifetimeDays),
            AbsoluteExpiresAtUtc = nowUtc.AddDays(_options.RefreshTokenAbsoluteLifetimeDays)
        };

        _dbContext.RefreshSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return IssueSession(user, session, refreshToken, securityStamp);
    }

    private IssuedAuthenticationSession IssueSession(
        ApplicationUser user,
        RefreshSession session,
        string refreshToken,
        string securityStamp)
    {
        var accessToken = _accessTokenIssuer.Issue(user.Id, session.Id, securityStamp);
        return new IssuedAuthenticationSession(
            accessToken.Value,
            accessToken.ExpiresAtUtc,
            refreshToken,
            session.ExpiresAtUtc,
            ToAuthenticatedUser(user));
    }

    private async Task RevokeFamilyAsync(
        Guid familyId,
        string reason,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        await _dbContext.RefreshSessions
            .Where(session => session.FamilyId == familyId && session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(session => session.RevokedAtUtc, nowUtc)
                    .SetProperty(session => session.RevocationReason, reason),
                cancellationToken);
    }

    private static AuthenticatedUser ToAuthenticatedUser(ApplicationUser user) =>
        new(user.Id, user.Email ?? string.Empty, user.CreatedAtUtc);

    private static byte[] Hash(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));

    private static string CreateRefreshToken()
    {
        var value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return value.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
