using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence.Configurations;

public sealed class ShortUrlAnalyticsDailyVisitorConfiguration : IEntityTypeConfiguration<ShortUrlAnalyticsDailyVisitor>
{
    public void Configure(EntityTypeBuilder<ShortUrlAnalyticsDailyVisitor> builder)
    {
        builder.ToTable("ShortUrlAnalyticsDailyVisitors");

        builder.HasKey(x => new
        {
            x.ShortUrlId,
            x.IdentityPeriodUtc,
            x.PseudonymousVisitorId
        });

        builder.Property(x => x.IdentityPeriodUtc)
            .HasColumnType("date");

        builder.Property(x => x.PseudonymousVisitorId)
            .HasMaxLength(64)
            .IsUnicode(false);

        builder.Property(x => x.IdentityScheme)
            .HasMaxLength(64)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.FirstSeenAtUtc)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasOne(x => x.ShortUrl)
            .WithMany()
            .HasForeignKey(x => x.ShortUrlId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.IdentityPeriodUtc, x.ShortUrlId })
            .HasDatabaseName("IX_AnalyticsDailyVisitors_Period_Link");
    }
}
