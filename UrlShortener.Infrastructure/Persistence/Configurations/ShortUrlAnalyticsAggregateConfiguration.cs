using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence.Configurations;

public sealed class ShortUrlAnalyticsAggregateConfiguration : IEntityTypeConfiguration<ShortUrlAnalyticsAggregate>
{
    public void Configure(EntityTypeBuilder<ShortUrlAnalyticsAggregate> builder)
    {
        builder.ToTable("ShortUrlAnalyticsAggregates");

        builder.HasKey(x => new
        {
            x.ShortUrlId,
            x.Granularity,
            x.Dimension,
            x.DimensionSchemaVersion,
            x.BucketStartUtc,
            x.DimensionValue
        });

        builder.Property(x => x.Granularity)
            .HasConversion<byte>();

        builder.Property(x => x.Dimension)
            .HasConversion<byte>();

        builder.Property(x => x.DimensionSchemaVersion)
            .IsRequired();

        builder.Property(x => x.BucketStartUtc)
            .HasColumnType("datetime2(0)")
            .IsRequired();

        builder.Property(x => x.DimensionValue)
            .HasMaxLength(253)
            .IsUnicode(false)
            .UseCollation("Latin1_General_100_BIN2")
            .IsRequired();

        builder.Property(x => x.ClickCount)
            .IsRequired();

        builder.Property(x => x.UniqueVisitorCount)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasOne(x => x.ShortUrl)
            .WithMany()
            .HasForeignKey(x => x.ShortUrlId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new
            {
                x.ShortUrlId,
                x.Granularity,
                x.Dimension,
                x.DimensionSchemaVersion,
                x.BucketStartUtc
            })
            .HasDatabaseName("IX_AnalyticsAggregates_Link_Query");
    }
}
