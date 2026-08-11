using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrlShortener.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnedShortUrlQueryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShortUrls_OwnerId_CreatedAtUtc",
                table: "ShortUrls");

            migrationBuilder.CreateIndex(
                name: "IX_ShortUrls_OwnerId_IsDeleted_CreatedAtUtc_Id",
                table: "ShortUrls",
                columns: new[] { "OwnerId", "IsDeleted", "CreatedAtUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShortUrls_OwnerId_IsDeleted_CreatedAtUtc_Id",
                table: "ShortUrls");

            migrationBuilder.CreateIndex(
                name: "IX_ShortUrls_OwnerId_CreatedAtUtc",
                table: "ShortUrls",
                columns: new[] { "OwnerId", "CreatedAtUtc" });
        }
    }
}
