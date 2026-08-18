using System.Linq.Expressions;
using FluentValidation;

namespace UrlShortener.Application.Validators;

internal sealed class AnalyticsUtcRangeValidator<T> : AbstractValidator<T>
{
    public AnalyticsUtcRangeValidator(
        Expression<Func<T, DateTimeOffset?>> fromExpression,
        Expression<Func<T, DateTimeOffset?>> toExpression,
        TimeSpan maximumRange,
        string requiredBoundary)
    {
        var from = fromExpression.Compile();
        var to = toExpression.Compile();

        RuleFor(fromExpression)
            .Must(value => !value.HasValue || value.Value.Offset == TimeSpan.Zero)
            .WithMessage("FromUtc must use the UTC offset Z or +00:00.")
            .Must(value => !value.HasValue || IsAligned(value.Value, requiredBoundary))
            .WithMessage($"FromUtc must be aligned to a {requiredBoundary} boundary.");

        RuleFor(toExpression)
            .Must(value => !value.HasValue || value.Value.Offset == TimeSpan.Zero)
            .WithMessage("ToUtc must use the UTC offset Z or +00:00.")
            .Must(value => !value.HasValue || IsAligned(value.Value, requiredBoundary))
            .WithMessage($"ToUtc must be aligned to a {requiredBoundary} boundary.");

        RuleFor(x => x)
            .Must(x => from(x).HasValue == to(x).HasValue)
            .WithName("toUtc")
            .WithMessage("FromUtc and ToUtc must either both be supplied or both be omitted.");

        RuleFor(x => x)
            .Must(x => !from(x).HasValue || !to(x).HasValue || from(x) < to(x))
            .WithName("toUtc")
            .WithMessage("ToUtc must be greater than FromUtc.");

        RuleFor(x => x)
            .Must(x => !from(x).HasValue || !to(x).HasValue || to(x) - from(x) <= maximumRange)
            .WithName("fromUtc")
            .WithMessage($"The requested range cannot exceed {maximumRange.TotalDays:0.##} days.");
    }

    private static bool IsAligned(DateTimeOffset value, string requiredBoundary) =>
        requiredBoundary == "whole UTC hour"
            ? value.Minute == 0 && value.Second == 0 && value.Millisecond == 0 && value.Ticks % TimeSpan.TicksPerSecond == 0
            : value.TimeOfDay == TimeSpan.Zero;
}
