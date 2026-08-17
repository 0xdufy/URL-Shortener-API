namespace UrlShortener.Application.Interfaces;

public sealed record IdempotencySettings(int RetentionHours);
