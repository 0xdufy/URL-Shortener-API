using FluentValidation;
using UrlShortener.Application.Dtos;

namespace UrlShortener.Application.Validators;

public class CreateShortUrlRequestValidator : AbstractValidator<CreateShortUrlRequest>
{
    public CreateShortUrlRequestValidator()
    {
        RuleFor(x => x.OriginalUrl)
            .NotEmpty()
            .Must(BeValidUrl)
            .WithMessage("OriginalUrl must be an absolute http or https URL.");

        RuleFor(x => x.CustomAlias)
            .Length(4, 20)
            .Matches("^[A-Za-z0-9_-]+$")
            .When(x => !string.IsNullOrWhiteSpace(x.CustomAlias));

        RuleFor(x => x.ExpiresAtUtc)
            .Must(x => x == null || x.Value > DateTime.UtcNow)
            .WithMessage("ExpiresAtUtc must be greater than DateTime.UtcNow.");
    }

    private static bool BeValidUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }
}
