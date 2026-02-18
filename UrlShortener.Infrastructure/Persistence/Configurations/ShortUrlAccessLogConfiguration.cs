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

        builder.Property(x => x.IpAddress)
            .HasMaxLength(64);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(256);

        builder.Property(x => x.Referer)
            .HasMaxLength(512);

        builder.HasOne(x => x.ShortUrl)
            .WithMany(x => x.AccessLogs)
            .HasForeignKey(x => x.ShortUrlId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ShortUrlId, x.AccessedAtUtc });
    }
}
