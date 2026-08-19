using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrlShortener.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApiKeySecurityModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    KeyPrefix = table.Column<string>(type: "varchar(26)", nullable: false, collation: "Latin1_General_100_BIN2"),
                    SecretHash = table.Column<byte[]>(type: "binary(32)", nullable: false),
                    Scopes = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUsedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevocationReason = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ReplacedByApiKeyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                    table.CheckConstraint("CK_ApiKeys_Expiry", "[ExpiresAtUtc] IS NULL OR [ExpiresAtUtc] > [CreatedAtUtc]");
                    table.CheckConstraint("CK_ApiKeys_LastUsed", "[LastUsedAtUtc] IS NULL OR [LastUsedAtUtc] >= [CreatedAtUtc]");
                    table.CheckConstraint("CK_ApiKeys_Revocation", "[RevokedAtUtc] IS NULL OR [RevokedAtUtc] >= [CreatedAtUtc]");
                    table.CheckConstraint("CK_ApiKeys_Scopes", "[Scopes] > 0 AND ([Scopes] & ~15) = 0");
                    table.ForeignKey(
                        name: "FK_ApiKeys_ApiKeys_ReplacedByApiKeyId",
                        column: x => x.ReplacedByApiKeyId,
                        principalTable: "ApiKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApiKeys_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_OwnerId_CreatedAtUtc",
                table: "ApiKeys",
                columns: new[] { "OwnerId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_OwnerId_RevokedAtUtc_ExpiresAtUtc",
                table: "ApiKeys",
                columns: new[] { "OwnerId", "RevokedAtUtc", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_ReplacedByApiKeyId",
                table: "ApiKeys",
                column: "ReplacedByApiKeyId");

            migrationBuilder.CreateIndex(
                name: "UX_ApiKeys_KeyPrefix",
                table: "ApiKeys",
                column: "KeyPrefix",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiKeys");
        }
    }
}
