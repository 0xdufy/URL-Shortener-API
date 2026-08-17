using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UrlShortener.Application.Dtos;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.Messaging;
using UrlShortener.Infrastructure.Configuration;

namespace UrlShortener.Infrastructure.Messaging;

public sealed class PrivacyAwareRedirectClickEventPublisher : IRedirectClickEventPublisher
{
    private const int MaximumUserAgentLength = 256;
    private const int MaximumReferrerHostLength = 253;

    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<PrivacyAwareRedirectClickEventPublisher> _logger;
    private readonly byte[] _visitorIdentityKey;

    public PrivacyAwareRedirectClickEventPublisher(
        IEventPublisher eventPublisher,
        IOptions<ClickEventPrivacyOptions> options,
        ILogger<PrivacyAwareRedirectClickEventPublisher> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
        _visitorIdentityKey = Convert.FromBase64String(options.Value.VisitorIdentityHmacKeyBase64);
    }

    public async Task PublishBestEffortAsync(
        RedirectClickEventRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var eventId = Guid.NewGuid();
        try
        {
            if (request.ShortUrlId == Guid.Empty)
            {
                throw new ArgumentException("The short URL ID cannot be empty.", nameof(request));
            }

            if (request.AccessedAtUtc.Offset != TimeSpan.Zero)
            {
                throw new ArgumentException("The access timestamp must use the UTC offset.", nameof(request));
            }

            var visitorIdentityPeriodUtc = DateOnly.FromDateTime(request.AccessedAtUtc.UtcDateTime);
            var payload = new ClickEventV1(
                request.ShortUrlId,
                request.AccessedAtUtc,
                GetReferrerHost(request.Referer),
                Truncate(request.UserAgent, MaximumUserAgentLength),
                CreatePseudonymousVisitorId(
                    request.ClientIpAddress,
                    visitorIdentityPeriodUtc),
                visitorIdentityPeriodUtc,
                ClickEventContract.VisitorIdentityScheme);
            var integrationEvent = new IntegrationEvent<ClickEventV1>(
                eventId,
                ClickEventContract.EventName,
                ClickEventContract.Version,
                request.AccessedAtUtc,
                payload);

            await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);
            _logger.LogDebug(
                "Published click event {EventId} for short URL {ShortUrlId}.",
                eventId,
                request.ShortUrlId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Click event {EventId} for short URL {ShortUrlId} could not be published or confirmed. The redirect remains available; analytics for this click may be missing or duplicated.",
                eventId,
                request.ShortUrlId);
        }
    }

    private string CreatePseudonymousVisitorId(
        string clientIpAddress,
        DateOnly visitorIdentityPeriodUtc)
    {
        var input = string.Concat(
            visitorIdentityPeriodUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            "\n",
            clientIpAddress);
        var digest = HMACSHA256.HashData(
            _visitorIdentityKey,
            Encoding.UTF8.GetBytes(input));

        return Convert.ToBase64String(digest)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string? GetReferrerHost(string? referer)
    {
        if (string.IsNullOrWhiteSpace(referer) ||
            !Uri.TryCreate(referer, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return null;
        }

        var host = uri.IdnHost.ToLowerInvariant();
        return host.Length <= MaximumReferrerHostLength
            ? host
            : null;
    }

    private static string? Truncate(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maximumLength
            ? trimmed
            : trimmed[..maximumLength];
    }
}
