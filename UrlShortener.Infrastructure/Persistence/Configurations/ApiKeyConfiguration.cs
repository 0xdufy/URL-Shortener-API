using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Domain.Entities;
using UrlShortener.Infrastructure.Identity;

namespace UrlShortener.Infrastructure.Persistence.Configurations;

public sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public const string KeyPrefixUniqueIndexName = "UX_ApiKeys_KeyPrefix";

    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable(
            "ApiKeys",
            table =>
            {
                table.HasCheckConstraint("CK_ApiKeys_Scopes", "[Scopes] > 0 AND ([Scopes] & ~15) = 0");
                table.HasCheckConstraint(
                    "CK_ApiKeys_Expiry",
                    "[ExpiresAtUtc] IS NULL OR [ExpiresAtUtc] > [CreatedAtUtc]");
                table.HasCheckConstraint(
                    "CK_ApiKeys_LastUsed",
                    "[LastUsedAtUtc] IS NULL OR [LastUsedAtUtc] >= [CreatedAtUtc]");
                table.HasCheckConstraint(
                    "CK_ApiKeys_Revocation",
                    "[RevokedAtUtc] IS NULL OR [RevokedAtUtc] >= [CreatedAtUtc]");
            });

        builder.HasKey(apiKey => apiKey.Id);

        builder.Property(apiKey => apiKey.Name)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(apiKey => apiKey.KeyPrefix)
            .HasColumnType("varchar(26)")
            .UseCollation("Latin1_General_100_BIN2")
            .IsRequired();

        builder.Property(apiKey => apiKey.SecretHash)
            .HasColumnType("binary(32)")
            .IsRequired();

        builder.Property(apiKey => apiKey.Scopes)
            .HasConversion<int>();

        builder.Property(apiKey => apiKey.RevocationReason)
            .HasMaxLength(128);

        builder.Property(apiKey => apiKey.RowVersion)
            .IsRowVersion();

        builder.HasIndex(apiKey => apiKey.KeyPrefix)
            .HasDatabaseName(KeyPrefixUniqueIndexName)
            .IsUnique();

        builder.HasIndex(apiKey => new { apiKey.OwnerId, apiKey.CreatedAtUtc });
        builder.HasIndex(apiKey => new { apiKey.OwnerId, apiKey.RevokedAtUtc, apiKey.ExpiresAtUtc });

        builder.HasOne<ApplicationUser>()
            .WithMany(user => user.ApiKeys)
            .HasForeignKey(apiKey => apiKey.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApiKey>()
            .WithMany()
            .HasForeignKey(apiKey => apiKey.ReplacedByApiKeyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
