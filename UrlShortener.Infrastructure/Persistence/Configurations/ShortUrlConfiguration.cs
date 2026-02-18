using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Infrastructure.Persistence.Configurations;

public class ShortUrlConfiguration : IEntityTypeConfiguration<ShortUrl>
{
    public void Configure(EntityTypeBuilder<ShortUrl> builder)
    {
        builder.ToTable("ShortUrls");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OriginalUrl)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(x => x.ShortCode)
            .IsRequired()
            .HasMaxLength(20)
            .UseCollation("Latin1_General_CS_AS");

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(x => x.ExpiresAtUtc)
            .HasColumnType("datetime2");

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.DeletedAtUtc)
            .HasColumnType("datetime2");

        builder.Property(x => x.ClickCount)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(x => x.LastAccessedAtUtc)
            .HasColumnType("datetime2");

        builder.HasIndex(x => x.ShortCode)
            .IsUnique();

        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => x.ExpiresAtUtc);
    }
}
