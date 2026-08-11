namespace UrlShortener.Application.Dtos;

public sealed class AuthenticatedUserResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
}
