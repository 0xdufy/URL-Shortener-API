using System.Drawing;
using System.Text;
using QRCoder;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Interfaces;

namespace UrlShortener.Infrastructure.Services;

public sealed class QrCodeGeneratorService : IQrCodeGenerator
{
    public QrCodeDocument Generate(string canonicalShortUrl, QrCodeRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalShortUrl);

        using var data = QRCodeGenerator.GenerateQrCode(
            canonicalShortUrl,
            ParseErrorCorrection(request.ErrorCorrection),
            forceUtf8: true);
        using var qrCode = new SvgQRCode(data);
        var dimensions = new Size(request.Size, request.Size);
        var svg = qrCode.GetGraphic(
            dimensions,
            request.Foreground,
            request.Background,
            drawQuietZones: true,
            SvgQRCode.SizingMode.WidthHeightAttribute);

        return new QrCodeDocument(Encoding.UTF8.GetBytes(svg), "image/svg+xml", "svg");
    }

    private static QRCodeGenerator.ECCLevel ParseErrorCorrection(string value) =>
        value.ToLowerInvariant() switch
        {
            "low" => QRCodeGenerator.ECCLevel.L,
            "medium" => QRCodeGenerator.ECCLevel.M,
            "quartile" => QRCodeGenerator.ECCLevel.Q,
            "high" => QRCodeGenerator.ECCLevel.H,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported error correction level.")
        };
}
