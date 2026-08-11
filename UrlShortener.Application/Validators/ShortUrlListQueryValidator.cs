using FluentValidation;
using UrlShortener.Application.Dtos;

namespace UrlShortener.Application.Validators;

public sealed class ShortUrlListQueryValidator : AbstractValidator<ShortUrlListQuery>
{
    private static readonly string[] ExpirationValues = ["all", "expired", "notExpired"];
    private static readonly string[] SortFields = ["createdAt", "shortCode", "clickCount", "expiresAt"];
    private static readonly string[] SortDirections = ["asc", "desc"];

    public ShortUrlListQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x)
            .Must(x => x.Page < 1 || x.PageSize < 1 || (long)(x.Page - 1) * x.PageSize <= int.MaxValue)
            .WithName(nameof(ShortUrlListQuery.Page))
            .WithMessage("Page is too large for the selected PageSize.");

        When(x => x.Search is not null, () =>
        {
            RuleFor(x => x.Search!)
                .Must(value => !string.IsNullOrWhiteSpace(value))
                .WithMessage("Search must not be empty when provided.")
                .MaximumLength(200)
                .WithMessage("Search must not exceed 200 characters.");
        });

        RuleFor(x => x.Expiration)
            .Must(value => Contains(ExpirationValues, value))
            .WithMessage("Expiration must be one of: all, expired, notExpired.");

        RuleFor(x => x.SortBy)
            .Must(value => Contains(SortFields, value))
            .WithMessage("SortBy must be one of: createdAt, shortCode, clickCount, expiresAt.");

        RuleFor(x => x.SortDirection)
            .Must(value => Contains(SortDirections, value))
            .WithMessage("SortDirection must be one of: asc, desc.");

        When(x => x.CreatedFromUtc.HasValue, () =>
        {
            RuleFor(x => x.CreatedFromUtc)
                .Must(value => value!.Value.Offset == TimeSpan.Zero)
                .WithMessage("CreatedFromUtc must use the UTC offset Z or +00:00.");
        });

        When(x => x.CreatedToUtc.HasValue, () =>
        {
            RuleFor(x => x.CreatedToUtc)
                .Must(value => value!.Value.Offset == TimeSpan.Zero)
                .WithMessage("CreatedToUtc must use the UTC offset Z or +00:00.");
        });

        RuleFor(x => x)
            .Must(x => !x.CreatedFromUtc.HasValue || !x.CreatedToUtc.HasValue || x.CreatedFromUtc <= x.CreatedToUtc)
            .WithName(nameof(ShortUrlListQuery.CreatedToUtc))
            .WithMessage("CreatedToUtc must be greater than or equal to CreatedFromUtc.");
    }

    private static bool Contains(IEnumerable<string> values, string? candidate) =>
        candidate is not null && values.Contains(candidate, StringComparer.OrdinalIgnoreCase);
}
