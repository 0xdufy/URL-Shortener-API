namespace UrlShortener.Application.Dtos;

public sealed class ShortUrlListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
    public string Expiration { get; set; } = "all";
    public bool IncludeDeleted { get; set; }
    public DateTimeOffset? CreatedFromUtc { get; set; }
    public DateTimeOffset? CreatedToUtc { get; set; }
    public string SortBy { get; set; } = "createdAt";
    public string SortDirection { get; set; } = "desc";
}
