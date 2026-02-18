namespace UrlShortener.Application.Dtos;

public class StatsResponse
{
    public string ShortCode { get; set; } = string.Empty;
    public long TotalClicks { get; set; }
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public List<DailyClicksItem> DailyClicks { get; set; } = new();
}
