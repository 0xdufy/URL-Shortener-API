namespace UrlShortener.Application.ApiKeys;

public sealed record ApiKeyManagementSettings(int MaximumActiveKeysPerUser = 10);
