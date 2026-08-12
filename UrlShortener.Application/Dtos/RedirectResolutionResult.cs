namespace UrlShortener.Application.Dtos;

public enum RedirectResolutionStatus
{
    Resolved,
    NotFound,
    Expired
}

public enum RedirectResolutionSource
{
    DistributedCache,
    Persistence
}

public sealed class RedirectResolutionResult
{
    private RedirectResolutionResult(
        RedirectResolutionStatus status,
        RedirectResolutionSource source,
        string? originalUrl)
    {
        Status = status;
        Source = source;
        OriginalUrl = originalUrl;
    }

    public RedirectResolutionStatus Status { get; }
    public RedirectResolutionSource Source { get; }
    public string? OriginalUrl { get; }

    public static RedirectResolutionResult Resolved(
        string originalUrl,
        RedirectResolutionSource source) =>
        new(RedirectResolutionStatus.Resolved, source, originalUrl);

    public static RedirectResolutionResult NotFound(RedirectResolutionSource source) =>
        new(RedirectResolutionStatus.NotFound, source, null);

    public static RedirectResolutionResult Expired(RedirectResolutionSource source) =>
        new(RedirectResolutionStatus.Expired, source, null);
}
