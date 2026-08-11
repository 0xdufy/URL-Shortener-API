using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrlShortener.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ShortUrls",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OriginalUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                ShortCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, collation: "Latin1_General_CS_AS"),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                ClickCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                LastAccessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ShortUrls", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ShortUrlAccessLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ShortUrlId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                AccessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                UserAgent = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                Referer = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ShortUrlAccessLogs", x => x.Id);
                table.ForeignKey(
                    name: "FK_ShortUrlAccessLogs_ShortUrls_ShortUrlId",
                    column: x => x.ShortUrlId,
                    principalTable: "ShortUrls",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ShortUrlAccessLogs_ShortUrlId_AccessedAtUtc",
            table: "ShortUrlAccessLogs",
            columns: new[] { "ShortUrlId", "AccessedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_ShortUrls_ExpiresAtUtc",
            table: "ShortUrls",
            column: "ExpiresAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_ShortUrls_IsDeleted",
            table: "ShortUrls",
            column: "IsDeleted");

        migrationBuilder.CreateIndex(
            name: "IX_ShortUrls_ShortCode",
            table: "ShortUrls",
            column: "ShortCode",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ShortUrlAccessLogs");
        migrationBuilder.DropTable(name: "ShortUrls");
    }
}
