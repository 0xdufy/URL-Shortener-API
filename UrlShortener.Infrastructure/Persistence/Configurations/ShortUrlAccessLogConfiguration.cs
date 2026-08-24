using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence.Configurations;

public class ShortUrlAccessLogConfiguration : IEntityTypeConfiguration<ShortUrlAccessLog>
{
    public void Configure(EntityTypeBuilder<ShortUrlAccessLog> builder)
    {
        builder.ToTable("ShortUrlAccessLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ShortUrlId)
            .IsRequired();

        builder.Property(x => x.AccessedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(x => x.UserAgent)
            .HasMaxLength(256);

        builder.Property(x => x.ReferrerHost)
            .HasMaxLength(253);

        builder.Property(x => x.ReferrerKind)
            .HasMaxLength(16)
            .IsUnicode(false);

        builder.Property(x => x.PseudonymousVisitorId)
            .HasMaxLength(64)
            .IsUnicode(false);

        builder.Property(x => x.VisitorIdentityPeriodUtc)
            .HasColumnType("date");

        builder.Property(x => x.VisitorIdentityScheme)
            .HasMaxLength(64)
            .IsUnicode(false);

        builder.HasOne(x => x.ShortUrl)
            .WithMany(x => x.AccessLogs)
            .HasForeignKey(x => x.ShortUrlId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ShortUrlId, x.AccessedAtUtc, x.Id })
            .HasDatabaseName("IX_ShortUrlAccessLogs_ShortUrlId_AccessedAtUtc_Id");

        builder.HasIndex(x => new
        {
            x.ShortUrlId,
            x.VisitorIdentityPeriodUtc,
            x.PseudonymousVisitorId
        })
            .HasDatabaseName("IX_ShortUrlAccessLogs_ShortUrlId_VisitorPeriod_VisitorId")
            .HasFilter("[VisitorIdentityPeriodUtc] IS NOT NULL AND [PseudonymousVisitorId] IS NOT NULL");
    }
}
