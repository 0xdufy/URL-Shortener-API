using FluentValidation;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Interfaces;

namespace UrlShortener.Application.Validators;

public class CreateShortUrlRequestValidator : AbstractValidator<CreateShortUrlRequest>
{
    public CreateShortUrlRequestValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(x => x.OriginalUrl)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(BeValidUrl)
            .WithMessage("OriginalUrl must be an absolute http or https URL.");

        RuleFor(x => x.CustomAlias)
            .Length(4, 20)
            .Matches("^[A-Za-z0-9_-]+$")
            .When(x => !string.IsNullOrWhiteSpace(x.CustomAlias));

        RuleFor(x => x.ExpiresAtUtc)
            .Cascade(CascadeMode.Stop)
            .Must(x => x == null || x.Value.Kind == DateTimeKind.Utc)
            .WithMessage("ExpiresAtUtc must be a UTC timestamp ending in 'Z'.")
            .Must(x => x == null || x.Value > dateTimeProvider.UtcNow)
            .WithMessage("ExpiresAtUtc must be a future UTC timestamp.");
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
