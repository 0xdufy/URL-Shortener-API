namespace UrlShortener.Api.Models;

public class ErrorResponse
{
    public string TraceId { get; set; } = string.Empty;
    public ErrorBody Error { get; set; } = new();
}

public class ErrorBody
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<ErrorDetail> Details { get; set; } = new();
}

public class ErrorDetail
{
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
