using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Infrastructure.Identity;

namespace UrlShortener.Infrastructure.Persistence.Configurations;

public sealed class RefreshSessionConfiguration : IEntityTypeConfiguration<RefreshSession>
{
    public void Configure(EntityTypeBuilder<RefreshSession> builder)
    {
        builder.ToTable(
            "RefreshSessions",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_RefreshSessions_ExpiresAfterCreation",
                    "[ExpiresAtUtc] > [CreatedAtUtc]");
                table.HasCheckConstraint(
                    "CK_RefreshSessions_AbsoluteExpiry",
                    "[AbsoluteExpiresAtUtc] >= [ExpiresAtUtc]");
            });

        builder.HasKey(session => session.Id);

        builder.Property(session => session.TokenHash)
            .HasColumnType("binary(32)")
            .IsRequired();

        builder.Property(session => session.SecurityStampHash)
            .HasColumnType("binary(32)")
            .IsRequired();

        builder.Property(session => session.RevocationReason)
            .HasMaxLength(128);

        builder.Property(session => session.RowVersion)
            .IsRowVersion();

        builder.HasIndex(session => session.TokenHash)
            .IsUnique();

        builder.HasIndex(session => new { session.UserId, session.RevokedAtUtc, session.ExpiresAtUtc });
        builder.HasIndex(session => session.FamilyId);

        builder.HasOne(session => session.User)
            .WithMany(user => user.RefreshSessions)
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(session => session.ReplacedBySession)
            .WithMany()
            .HasForeignKey(session => session.ReplacedBySessionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
