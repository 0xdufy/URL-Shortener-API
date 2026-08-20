using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Domain.CustomDomains;
using UrlShortener.Domain.Entities;
using UrlShortener.Infrastructure.Identity;

namespace UrlShortener.Infrastructure.Persistence.Configurations;

public sealed class CustomDomainConfiguration : IEntityTypeConfiguration<CustomDomain>
{
    public const string NormalizedHostUniqueIndexName = "UX_CustomDomains_NormalizedHost";

    public void Configure(EntityTypeBuilder<CustomDomain> builder)
    {
        builder.ToTable(
            "CustomDomains",
            table =>
            {
                table.HasCheckConstraint("CK_CustomDomains_Status", "[Status] BETWEEN 1 AND 4");
                table.HasCheckConstraint("CK_CustomDomains_VerificationMethod", "[VerificationMethod] = 1");
                table.HasCheckConstraint(
                    "CK_CustomDomains_NormalizedHost",
                    "[NormalizedHost] = LOWER([NormalizedHost]) AND " +
                    "RIGHT([NormalizedHost], 1) <> '.' AND " +
                    "CHARINDEX('..', [NormalizedHost]) = 0 AND " +
                    "[NormalizedHost] NOT LIKE '%[^a-z0-9.-]%'");
                table.HasCheckConstraint(
                    "CK_CustomDomains_VerifiedState",
                    "[Status] <> 2 OR [VerifiedAtUtc] IS NOT NULL");
                table.HasCheckConstraint(
                    "CK_CustomDomains_DisabledState",
                    "[Status] <> 4 OR [DisabledAtUtc] IS NOT NULL");
                table.HasCheckConstraint(
                    "CK_CustomDomains_FailureState",
                    "[Status] <> 3 OR ([FailureCode] IS NOT NULL AND [FailureMessage] IS NOT NULL)");
                table.HasCheckConstraint(
                    "CK_CustomDomains_Timestamps",
                    "[UpdatedAtUtc] >= [CreatedAtUtc] AND " +
                    "([VerificationRequestedAtUtc] IS NULL OR [VerificationRequestedAtUtc] >= [CreatedAtUtc]) AND " +
                    "([LastVerificationAttemptAtUtc] IS NULL OR [LastVerificationAttemptAtUtc] >= [CreatedAtUtc])");
            });

        builder.HasKey(customDomain => customDomain.Id);

        builder.Property(customDomain => customDomain.NormalizedHost)
            .HasColumnType("varchar(253)")
            .UseCollation("Latin1_General_100_BIN2")
            .IsRequired();

        builder.Property(customDomain => customDomain.Status)
            .HasConversion<int>();

        builder.Property(customDomain => customDomain.VerificationMethod)
            .HasConversion<int>();

        builder.Property(customDomain => customDomain.VerificationToken)
            .HasColumnType("varchar(43)")
            .UseCollation("Latin1_General_100_BIN2")
            .IsRequired();

        builder.Property(customDomain => customDomain.FailureCode)
            .HasMaxLength(64);

        builder.Property(customDomain => customDomain.FailureMessage)
            .HasMaxLength(256);

        builder.Property(customDomain => customDomain.RowVersion)
            .IsRowVersion();

        builder.HasIndex(customDomain => customDomain.NormalizedHost)
            .HasDatabaseName(NormalizedHostUniqueIndexName)
            .IsUnique();

        builder.HasIndex(customDomain => new { customDomain.OwnerId, customDomain.CreatedAtUtc });
        builder.HasIndex(customDomain => new { customDomain.Status, customDomain.UpdatedAtUtc });

        builder.HasOne<ApplicationUser>()
            .WithMany(user => user.CustomDomains)
            .HasForeignKey(customDomain => customDomain.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
