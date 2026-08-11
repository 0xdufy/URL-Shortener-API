using UrlShortener.Application.Dtos;

namespace UrlShortener.Application.Interfaces;

public enum ShortUrlExpirationFilter
{
    All,
    Expired,
    NotExpired
}

public enum ShortUrlSortField
{
    CreatedAt,
    ShortCode,
    ClickCount,
    ExpiresAt
}

public enum SortDirection
{
    Ascending,
    Descending
}

public sealed record ShortUrlListCriteria(
    Guid OwnerId,
    int Page,
    int PageSize,
    string? Search,
    bool? IsActive,
    ShortUrlExpirationFilter Expiration,
    bool IncludeDeleted,
    DateTime? CreatedFromUtc,
    DateTime? CreatedToUtc,
    ShortUrlSortField SortBy,
    SortDirection SortDirection,
    DateTime NowUtc,
    int RestoreRetentionDays);

public sealed record ShortUrlListResult(
    IReadOnlyList<ShortUrlListItemResponse> Items,
    int TotalItems);
