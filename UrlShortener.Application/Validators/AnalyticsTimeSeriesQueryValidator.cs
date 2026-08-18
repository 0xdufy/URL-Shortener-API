using FluentValidation;
using UrlShortener.Application.Dtos;

namespace UrlShortener.Application.Validators;

public sealed class AnalyticsTimeSeriesQueryValidator : AbstractValidator<AnalyticsTimeSeriesQuery>
{
    public AnalyticsTimeSeriesQueryValidator()
    {
        RuleFor(x => x.Granularity)
            .Must(value => value is not null &&
                (value.Equals("hour", StringComparison.OrdinalIgnoreCase) ||
                 value.Equals("day", StringComparison.OrdinalIgnoreCase)))
            .WithMessage("Granularity must be one of: hour, day.");

        RuleFor(x => x.FromUtc)
            .Must(value => !value.HasValue || value.Value.Offset == TimeSpan.Zero)
            .WithMessage("FromUtc must use the UTC offset Z or +00:00.");

        RuleFor(x => x.ToUtc)
            .Must(value => !value.HasValue || value.Value.Offset == TimeSpan.Zero)
            .WithMessage("ToUtc must use the UTC offset Z or +00:00.");

        RuleFor(x => x)
            .Must(x => x.FromUtc.HasValue == x.ToUtc.HasValue)
            .WithName("toUtc")
            .WithMessage("FromUtc and ToUtc must either both be supplied or both be omitted.");

        RuleFor(x => x)
            .Must(x => !x.FromUtc.HasValue || !x.ToUtc.HasValue || x.FromUtc < x.ToUtc)
            .WithName("toUtc")
            .WithMessage("ToUtc must be greater than FromUtc.");

        RuleFor(x => x)
            .Must(HasValidAlignment)
            .WithName("fromUtc")
            .WithMessage("FromUtc and ToUtc must align to whole UTC hours for hour granularity or UTC midnight for day granularity.");

        RuleFor(x => x)
            .Must(HasValidRangeLength)
            .WithName("fromUtc")
            .WithMessage("Hourly ranges cannot exceed 31 days and daily ranges cannot exceed 731 days.");
    }

    private static bool HasValidAlignment(AnalyticsTimeSeriesQuery query)
    {
        if (!query.FromUtc.HasValue || !query.ToUtc.HasValue)
        {
            return true;
        }

        return IsAligned(query.FromUtc.Value, query.Granularity) &&
            IsAligned(query.ToUtc.Value, query.Granularity);
    }

    private static bool IsAligned(DateTimeOffset value, string granularity) =>
        string.Equals(granularity, "hour", StringComparison.OrdinalIgnoreCase)
            ? value.Minute == 0 && value.Second == 0 && value.Millisecond == 0 && value.Ticks % TimeSpan.TicksPerSecond == 0
            : value.TimeOfDay == TimeSpan.Zero;

    private static bool HasValidRangeLength(AnalyticsTimeSeriesQuery query)
    {
        if (!query.FromUtc.HasValue || !query.ToUtc.HasValue || query.FromUtc >= query.ToUtc)
        {
            return true;
        }

        var maximum = string.Equals(query.Granularity, "hour", StringComparison.OrdinalIgnoreCase)
            ? TimeSpan.FromDays(31)
            : TimeSpan.FromDays(731);
        return query.ToUtc - query.FromUtc <= maximum;
    }
}
