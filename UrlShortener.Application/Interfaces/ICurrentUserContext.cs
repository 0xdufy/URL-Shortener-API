namespace UrlShortener.Application.Interfaces;

public interface ICurrentUserContext
{
    Guid? UserId { get; }
}
