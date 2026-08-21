using System.Globalization;
using FluentValidation;
using UrlShortener.Application.Dtos;

namespace UrlShortener.Application.Validators;

public class QrCodeRequestValidator : AbstractValidator<QrCodeRequest>
{
    private static readonly HashSet<string> ErrorCorrectionLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "low",
        "medium",
        "quartile",
        "high"
    };

    public QrCodeRequestValidator()
    {
        RuleFor(x => x.Size)
            .InclusiveBetween(128, 1024)
            .WithMessage("Size must be between 128 and 1024 pixels.");

        RuleFor(x => x.Format)
            .NotEmpty()
            .Must(value => string.Equals(value, "svg", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Format must be 'svg'.");

        RuleFor(x => x.ErrorCorrection)
            .NotEmpty()
            .Must(value => ErrorCorrectionLevels.Contains(value))
            .WithMessage("Error correction must be low, medium, quartile, or high.");

        RuleFor(x => x.Foreground)
            .NotEmpty()
            .Matches("^#[0-9a-fA-F]{6}$")
            .WithMessage("Foreground must be a six-digit hexadecimal color such as #111827.");

        RuleFor(x => x.Background)
            .NotEmpty()
            .Matches("^#[0-9a-fA-F]{6}$")
            .WithMessage("Background must be a six-digit hexadecimal color such as #ffffff.");

        RuleFor(x => x)
            .Must(HaveSufficientContrast)
            .When(x => IsHexColor(x.Foreground) && IsHexColor(x.Background))
            .WithName("colors")
            .WithMessage("Foreground and background colors must have a contrast ratio of at least 3:1.");
    }

    private static bool HaveSufficientContrast(QrCodeRequest request)
    {
        var foreground = RelativeLuminance(request.Foreground);
        var background = RelativeLuminance(request.Background);
        var lighter = Math.Max(foreground, background);
        var darker = Math.Min(foreground, background);
        return (lighter + 0.05) / (darker + 0.05) >= 3;
    }

    private static bool IsHexColor(string? value) =>
        value is { Length: 7 } &&
        value[0] == '#' &&
        value.AsSpan(1).ToArray().All(Uri.IsHexDigit);

    private static double RelativeLuminance(string color)
    {
        var red = Linearize(ParseChannel(color, 1));
        var green = Linearize(ParseChannel(color, 3));
        var blue = Linearize(ParseChannel(color, 5));
        return 0.2126 * red + 0.7152 * green + 0.0722 * blue;
    }

    private static double ParseChannel(string color, int start) =>
        int.Parse(color.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255d;

    private static double Linearize(double channel) =>
        channel <= 0.04045
            ? channel / 12.92
            : Math.Pow((channel + 0.055) / 1.055, 2.4);
}
