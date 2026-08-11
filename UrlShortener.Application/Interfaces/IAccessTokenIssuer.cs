using UrlShortener.Application.Authentication;

namespace UrlShortener.Application.Interfaces;

public interface IAccessTokenIssuer
{
    IssuedAccessToken Issue(Guid userId, Guid sessionId, string securityStamp);
}
