using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Interfaces;

public sealed record ShortUrlIdempotencyContext(
    Guid OwnerId,
    string KeyHash,
    string RequestHash,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc);

public enum IdempotentShortUrlCreationOutcome
{
    Created,
    Existing,
    ShortCodeConflict,
    RequestConflict
}

public sealed record IdempotentShortUrlCreationResult(
    IdempotentShortUrlCreationOutcome Outcome,
    ShortUrl? ShortUrl = null);
