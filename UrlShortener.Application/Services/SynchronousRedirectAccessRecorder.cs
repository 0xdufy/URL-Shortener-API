using UrlShortener.Application.Dtos;
using UrlShortener.Application.Interfaces;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Services;

public sealed class SynchronousRedirectAccessRecorder : IRedirectAccessRecorder
{
    private readonly IShortUrlRepository _repository;

    public SynchronousRedirectAccessRecorder(IShortUrlRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> TryRecordAsync(RedirectAccessRequest request, CancellationToken ct)
    {
        var updated = await _repository.IncrementClickCountAsync(
            request.ShortUrlId,
            request.OriginalUrl,
            request.ExpiresAtUtc,
            request.AccessedAtUtc,
            ct);
        if (!updated)
        {
            return false;
        }

        var log = new ShortUrlAccessLog
        {
            Id = Guid.NewGuid(),
            ShortUrlId = request.ShortUrlId,
            AccessedAtUtc = request.AccessedAtUtc,
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent,
            Referer = request.Referer
        };

        await _repository.AddAccessLogAsync(log, ct);
        await _repository.SaveChangesAsync(ct);

        return true;
    }
}
