namespace UrlShortener.Infrastructure.Configuration;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public bool UseInMemory { get; set; }
}
