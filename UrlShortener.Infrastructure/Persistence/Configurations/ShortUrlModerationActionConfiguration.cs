using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Domain.Entities;
using UrlShortener.Infrastructure.Identity;

namespace UrlShortener.Infrastructure.Persistence.Configurations;

public sealed class ShortUrlModerationActionConfiguration : IEntityTypeConfiguration<ShortUrlModerationAction>
{
    public void Configure(EntityTypeBuilder<ShortUrlModerationAction> builder)
    {
        builder.ToTable("ShortUrlModerationActions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasMaxLength(20).IsRequired();
        builder.Property(x => x.PublicReasonCode).HasMaxLength(50);
        builder.Property(x => x.InternalReason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.OccurredAtUtc).HasColumnType("datetime2").IsRequired();
        builder.HasIndex(x => new { x.ShortUrlId, x.OccurredAtUtc });
        builder.HasIndex(x => new { x.ActorUserId, x.OccurredAtUtc });
        builder.HasOne<ShortUrl>()
            .WithMany()
            .HasForeignKey(x => x.ShortUrlId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
