using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Domain.Entities;
using UrlShortener.Infrastructure.Identity;
using UrlShortener.Domain.Moderation;

namespace UrlShortener.Infrastructure.Persistence.Configurations;

public class ShortUrlConfiguration : IEntityTypeConfiguration<ShortUrl>
{
    public void Configure(EntityTypeBuilder<ShortUrl> builder)
    {
        builder.ToTable("ShortUrls");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OwnerId);
        builder.Property(x => x.CustomDomainId);

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

        builder.Property(x => x.ModerationStatus)
            .HasConversion<int>()
            .HasDefaultValue(ShortUrlModerationStatus.Unreviewed)
            .IsRequired();
        builder.Property(x => x.ModerationPublicReasonCode)
            .HasMaxLength(50);
        builder.Property(x => x.ModeratedAtUtc)
            .HasColumnType("datetime2");
        builder.Property(x => x.ModeratedByUserId);

        builder.HasIndex(x => x.ShortCode)
            .IsUnique()
            .HasDatabaseName("IX_ShortUrls_ShortCode");

        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => x.ExpiresAtUtc);
        builder.HasIndex(x => x.ModerationStatus);
        builder.HasIndex(x => new { x.OwnerId, x.IsDeleted, x.CreatedAtUtc, x.Id })
            .HasDatabaseName("IX_ShortUrls_OwnerId_IsDeleted_CreatedAtUtc_Id");
        builder.HasIndex(x => new { x.CustomDomainId, x.ShortCode })
            .HasDatabaseName("IX_ShortUrls_CustomDomainId_ShortCode");

        builder.HasOne<ApplicationUser>()
            .WithMany(x => x.ShortUrls)
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CustomDomain)
            .WithMany(x => x.ShortUrls)
            .HasForeignKey(x => new { x.CustomDomainId, x.OwnerId })
            .HasPrincipalKey(x => new { x.Id, x.OwnerId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.ModeratedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
