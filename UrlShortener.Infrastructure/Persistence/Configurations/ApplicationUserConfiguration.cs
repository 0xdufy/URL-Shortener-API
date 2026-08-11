using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Infrastructure.Identity;

namespace UrlShortener.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users");

        builder.Property(user => user.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(user => user.NormalizedEmail)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(user => user.UserName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(user => user.NormalizedUserName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .IsRequired();

        builder.Property(user => user.SecurityStamp)
            .IsRequired();

        builder.Property(user => user.Status)
            .HasConversion<int>();

        builder.HasIndex(user => user.NormalizedEmail)
            .HasDatabaseName("EmailIndex")
            .IsUnique();
    }
}
