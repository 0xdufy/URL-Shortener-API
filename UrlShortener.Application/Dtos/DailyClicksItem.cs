namespace UrlShortener.Application.Dtos;

public class DailyClicksItem
{
    public string DateUtc { get; set; } = string.Empty;
    public int Clicks { get; set; }
}
