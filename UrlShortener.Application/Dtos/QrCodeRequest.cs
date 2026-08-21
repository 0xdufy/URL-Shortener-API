namespace UrlShortener.Application.Dtos;

public class QrCodeRequest
{
    public int Size { get; set; } = 320;
    public string Format { get; set; } = "svg";
    public string ErrorCorrection { get; set; } = "medium";
    public string Foreground { get; set; } = "#111827";
    public string Background { get; set; } = "#ffffff";
}
