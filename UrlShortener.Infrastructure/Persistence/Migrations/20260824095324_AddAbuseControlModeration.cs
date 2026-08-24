using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrlShortener.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAbuseControlModeration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ModeratedAtUtc",
                table: "ShortUrls",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModeratedByUserId",
                table: "ShortUrls",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModerationPublicReasonCode",
                table: "ShortUrls",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModerationStatus",
                table: "ShortUrls",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "ShortUrlModerationActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShortUrlId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PublicReasonCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    InternalReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShortUrlModerationActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShortUrlModerationActions_ShortUrls_ShortUrlId",
                        column: x => x.ShortUrlId,
                        principalTable: "ShortUrls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShortUrlModerationActions_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShortUrls_ModeratedByUserId",
                table: "ShortUrls",
                column: "ModeratedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShortUrls_ModerationStatus",
                table: "ShortUrls",
                column: "ModerationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ShortUrlModerationActions_ActorUserId_OccurredAtUtc",
                table: "ShortUrlModerationActions",
                columns: new[] { "ActorUserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ShortUrlModerationActions_ShortUrlId_OccurredAtUtc",
                table: "ShortUrlModerationActions",
                columns: new[] { "ShortUrlId", "OccurredAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_ShortUrls_Users_ModeratedByUserId",
                table: "ShortUrls",
                column: "ModeratedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShortUrls_Users_ModeratedByUserId",
                table: "ShortUrls");

            migrationBuilder.DropTable(
                name: "ShortUrlModerationActions");

            migrationBuilder.DropIndex(
                name: "IX_ShortUrls_ModeratedByUserId",
                table: "ShortUrls");

            migrationBuilder.DropIndex(
                name: "IX_ShortUrls_ModerationStatus",
                table: "ShortUrls");

            migrationBuilder.DropColumn(
                name: "ModeratedAtUtc",
                table: "ShortUrls");

            migrationBuilder.DropColumn(
                name: "ModeratedByUserId",
                table: "ShortUrls");

            migrationBuilder.DropColumn(
                name: "ModerationPublicReasonCode",
                table: "ShortUrls");

            migrationBuilder.DropColumn(
                name: "ModerationStatus",
                table: "ShortUrls");
        }
    }
}
