using FluentValidation;
using UrlShortener.Application.Dtos;

namespace UrlShortener.Application.Validators;

public sealed class AnalyticsSummaryQueryValidator : AbstractValidator<AnalyticsSummaryQuery>
{
    public AnalyticsSummaryQueryValidator()
    {
        RuleFor(x => x.TopReferrers)
            .InclusiveBetween(1, 20)
            .WithMessage("TopReferrers must be between 1 and 20.");

        Include(new AnalyticsUtcRangeValidator<AnalyticsSummaryQuery>(
            x => x.FromUtc,
            x => x.ToUtc,
            TimeSpan.FromDays(366),
            "UTC midnight"));
    }
}
