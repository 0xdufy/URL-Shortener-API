using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using UrlShortener.Application.Interfaces;

namespace UrlShortener.Api.Security;

public sealed class HttpCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(value, out var userId) && userId != Guid.Empty
                ? userId
                : null;
        }
    }
}
