namespace UrlShortener.Application.Dtos;

public sealed class ShortUrlListResponse
{
    public IReadOnlyList<ShortUrlListItemResponse> Items { get; set; } = [];
    public PaginationMetadata Pagination { get; set; } = new();
    public ShortUrlFilterMetadata Filters { get; set; } = new();
}

public sealed class PaginationMetadata
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}

public sealed class ShortUrlFilterMetadata
{
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
    public string Expiration { get; set; } = "all";
    public bool IncludeDeleted { get; set; }
    public DateTime? CreatedFromUtc { get; set; }
    public DateTime? CreatedToUtc { get; set; }
    public string SortBy { get; set; } = "createdAt";
    public string SortDirection { get; set; } = "desc";
}
