using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrlShortener.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MinimizeAnalyticsClientMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE [ShortUrlAccessLogs]
                SET [Referer] = CASE
                    WHEN LEN([Referer]) <= 253
                         AND [Referer] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-Za-z.-]%'
                        THEN LOWER([Referer])
                    ELSE NULL
                END
                WHERE [Referer] IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "ShortUrlAccessLogs");

            migrationBuilder.RenameColumn(
                name: "Referer",
                table: "ShortUrlAccessLogs",
                newName: "ReferrerHost");

            migrationBuilder.AlterColumn<string>(
                name: "ReferrerHost",
                table: "ShortUrlAccessLogs",
                type: "nvarchar(253)",
                maxLength: 253,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ReferrerHost",
                table: "ShortUrlAccessLogs",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(253)",
                oldMaxLength: 253);

            migrationBuilder.RenameColumn(
                name: "ReferrerHost",
                table: "ShortUrlAccessLogs",
                newName: "Referer");

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "ShortUrlAccessLogs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }
    }
}
