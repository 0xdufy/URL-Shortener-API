using FluentValidation;
using UrlShortener.Application.Dtos;

namespace UrlShortener.Application.Validators;

public sealed class ModerateShortUrlRequestValidator : AbstractValidator<ModerateShortUrlRequest>
{
    private static readonly string[] OwnerVisibleReasonCodes =
    [
        "policy_violation",
        "unsafe_destination",
        "abuse"
    ];

    public ModerateShortUrlRequestValidator()
    {
        RuleFor(x => x.InternalReason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.PublicReasonCode)
            .Must(value => value is null || OwnerVisibleReasonCodes.Contains(value, StringComparer.Ordinal))
            .WithMessage("PublicReasonCode is not an allowed owner-visible reason code.");
        RuleFor(x => x.PublicReasonCode)
            .NotEmpty()
            .When(x => x.IsBlocked)
            .WithMessage("PublicReasonCode is required when blocking a link.");
        RuleFor(x => x.PublicReasonCode)
            .Empty()
            .When(x => !x.IsBlocked)
            .WithMessage("PublicReasonCode must be omitted when clearing a link.");
    }
}
