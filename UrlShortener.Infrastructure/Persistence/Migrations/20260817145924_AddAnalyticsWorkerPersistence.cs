using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrlShortener.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsWorkerPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShortUrlAccessLogs_ShortUrlId_AccessedAtUtc",
                table: "ShortUrlAccessLogs");

            migrationBuilder.AddColumn<string>(
                name: "PseudonymousVisitorId",
                table: "ShortUrlAccessLogs",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "VisitorIdentityPeriodUtc",
                table: "ShortUrlAccessLogs",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VisitorIdentityScheme",
                table: "ShortUrlAccessLogs",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShortUrlAccessLogs_ShortUrlId_AccessedAtUtc_Id",
                table: "ShortUrlAccessLogs",
                columns: new[] { "ShortUrlId", "AccessedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ShortUrlAccessLogs_ShortUrlId_VisitorPeriod_VisitorId",
                table: "ShortUrlAccessLogs",
                columns: new[] { "ShortUrlId", "VisitorIdentityPeriodUtc", "PseudonymousVisitorId" },
                filter: "[VisitorIdentityPeriodUtc] IS NOT NULL AND [PseudonymousVisitorId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShortUrlAccessLogs_ShortUrlId_AccessedAtUtc_Id",
                table: "ShortUrlAccessLogs");

            migrationBuilder.DropIndex(
                name: "IX_ShortUrlAccessLogs_ShortUrlId_VisitorPeriod_VisitorId",
                table: "ShortUrlAccessLogs");

            migrationBuilder.DropColumn(
                name: "PseudonymousVisitorId",
                table: "ShortUrlAccessLogs");

            migrationBuilder.DropColumn(
                name: "VisitorIdentityPeriodUtc",
                table: "ShortUrlAccessLogs");

            migrationBuilder.DropColumn(
                name: "VisitorIdentityScheme",
                table: "ShortUrlAccessLogs");

            migrationBuilder.CreateIndex(
                name: "IX_ShortUrlAccessLogs_ShortUrlId_AccessedAtUtc",
                table: "ShortUrlAccessLogs",
                columns: new[] { "ShortUrlId", "AccessedAtUtc" });
        }
    }
}
