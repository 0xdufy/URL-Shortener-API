using UrlShortener.Application.Interfaces;

namespace UrlShortener.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
