using FluentValidation;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.Security;

namespace UrlShortener.Application.Validators;

public sealed class UpdateShortUrlRequestValidator : AbstractValidator<UpdateShortUrlRequest>
{
    public UpdateShortUrlRequestValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(x => x.OriginalUrl)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(ShortUrlInputPolicy.MaximumDestinationUrlLength)
            .Must(BeValidUrl)
            .WithMessage("OriginalUrl must be an absolute http or https URL without credentials or surrounding whitespace.");

        RuleFor(x => x.ExpiresAtUtc)
            .Cascade(CascadeMode.Stop)
            .Must(x => x == null || x.Value.Kind == DateTimeKind.Utc)
            .WithMessage("ExpiresAtUtc must be a UTC timestamp ending in 'Z'.")
            .Must(x => x == null || x.Value > dateTimeProvider.UtcNow)
            .WithMessage("ExpiresAtUtc must be a future UTC timestamp.");
    }

    private static bool BeValidUrl(string url)
    {
        return ShortUrlInputPolicy.TryNormalizeDestinationUrl(url, out _);
    }
}
