using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Domain.Entities;
using UrlShortener.Infrastructure.Identity;

namespace UrlShortener.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ShortUrl> ShortUrls => Set<ShortUrl>();
    public DbSet<ShortUrlAccessLog> ShortUrlAccessLogs => Set<ShortUrlAccessLog>();
    public DbSet<ShortUrlAnalyticsAggregate> ShortUrlAnalyticsAggregates => Set<ShortUrlAnalyticsAggregate>();
    public DbSet<ShortUrlAnalyticsDailyVisitor> ShortUrlAnalyticsDailyVisitors => Set<ShortUrlAnalyticsDailyVisitor>();
    public DbSet<ShortUrlCreationIdempotencyRecord> ShortUrlCreationIdempotencyRecords =>
        Set<ShortUrlCreationIdempotencyRecord>();
    public DbSet<RefreshSession> RefreshSessions => Set<RefreshSession>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
