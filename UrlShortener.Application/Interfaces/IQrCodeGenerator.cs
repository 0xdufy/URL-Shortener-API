using UrlShortener.Application.Dtos;

namespace UrlShortener.Application.Interfaces;

public interface IQrCodeGenerator
{
    QrCodeDocument Generate(string canonicalShortUrl, QrCodeRequest request);
}

public sealed record QrCodeDocument(byte[] Content, string ContentType, string FileExtension);
