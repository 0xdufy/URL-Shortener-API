namespace UrlShortener.Application.Dtos;

public sealed class SignInRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
