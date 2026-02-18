using Microsoft.EntityFrameworkCore;
using UrlShortener.Application.Interfaces;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence.Repositories;

public class ShortUrlRepository : IShortUrlRepository
{
    private const string CaseSensitiveCollation = "Latin1_General_CS_AS";
    private readonly AppDbContext _dbContext;

    public ShortUrlRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ShortCodeExistsAsync(string shortCode, CancellationToken ct)
    {
        return _dbContext.ShortUrls
            .AnyAsync(x => EF.Functions.Collate(x.ShortCode, CaseSensitiveCollation) == shortCode, ct);
    }

    public Task AddShortUrlAsync(ShortUrl entity, CancellationToken ct)
    {
        return _dbContext.ShortUrls.AddAsync(entity, ct).AsTask();
    }

    public Task<ShortUrl?> GetByShortCodeNotDeletedAsync(string shortCode, CancellationToken ct)
    {
        return _dbContext.ShortUrls
            .FirstOrDefaultAsync(x => EF.Functions.Collate(x.ShortCode, CaseSensitiveCollation) == shortCode && !x.IsDeleted, ct);
    }

    public Task<ShortUrl?> GetByShortCodeAnyAsync(string shortCode, CancellationToken ct)
    {
        return _dbContext.ShortUrls
            .FirstOrDefaultAsync(x => EF.Functions.Collate(x.ShortCode, CaseSensitiveCollation) == shortCode, ct);
    }

    public Task<List<ShortUrlAccessLog>> GetAccessLogsAsync(Guid shortUrlId, DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        return _dbContext.ShortUrlAccessLogs
            .AsNoTracking()
            .Where(x => x.ShortUrlId == shortUrlId && x.AccessedAtUtc >= fromUtc && x.AccessedAtUtc <= toUtc)
            .OrderBy(x => x.AccessedAtUtc)
            .ToListAsync(ct);
    }

    public Task AddAccessLogAsync(ShortUrlAccessLog log, CancellationToken ct)
    {
        return _dbContext.ShortUrlAccessLogs.AddAsync(log, ct).AsTask();
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        return _dbContext.SaveChangesAsync(ct);
    }
}
