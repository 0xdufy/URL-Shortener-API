using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Domain.Entities;
using UrlShortener.Infrastructure.Identity;

namespace UrlShortener.Infrastructure.Persistence.Configurations;

public sealed class ShortUrlCreationIdempotencyRecordConfiguration
    : IEntityTypeConfiguration<ShortUrlCreationIdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<ShortUrlCreationIdempotencyRecord> builder)
    {
        builder.ToTable("ShortUrlCreationIdempotencyRecords");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.KeyHash)
            .IsRequired()
            .HasMaxLength(64)
            .IsUnicode(false);

        builder.Property(x => x.RequestHash)
            .IsRequired()
            .HasMaxLength(64)
            .IsUnicode(false);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(x => x.ExpiresAtUtc)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.HasIndex(x => new { x.OwnerId, x.KeyHash })
            .IsUnique()
            .HasDatabaseName("IX_ShortUrlCreationIdempotencyRecords_OwnerId_KeyHash");

        builder.HasIndex(x => x.ExpiresAtUtc)
            .HasDatabaseName("IX_ShortUrlCreationIdempotencyRecords_ExpiresAtUtc");

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ShortUrl)
            .WithMany()
            .HasForeignKey(x => x.ShortUrlId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
