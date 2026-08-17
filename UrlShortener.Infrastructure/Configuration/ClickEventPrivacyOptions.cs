namespace UrlShortener.Infrastructure.Configuration;

public sealed class ClickEventPrivacyOptions
{
    public const string SectionName = "ClickEvents";

    public string VisitorIdentityHmacKeyBase64 { get; set; } = string.Empty;
}
