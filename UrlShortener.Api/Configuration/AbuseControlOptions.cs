namespace UrlShortener.Api.Configuration;

public sealed class AbuseControlOptions
{
    public const string SectionName = "AbuseControls";

    public string[] BlockedDestinationHosts { get; init; } = [];
}
