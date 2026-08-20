using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrlShortener.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomDomainVerificationModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomDomains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NormalizedHost = table.Column<string>(type: "varchar(253)", nullable: false, collation: "Latin1_General_100_BIN2"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    VerificationMethod = table.Column<int>(type: "int", nullable: false),
                    VerificationToken = table.Column<string>(type: "varchar(43)", nullable: false, collation: "Latin1_General_100_BIN2"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VerificationRequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastVerificationAttemptAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DisabledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FailureMessage = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomDomains", x => x.Id);
                    table.CheckConstraint("CK_CustomDomains_DisabledState", "[Status] <> 4 OR [DisabledAtUtc] IS NOT NULL");
                    table.CheckConstraint("CK_CustomDomains_FailureState", "[Status] <> 3 OR ([FailureCode] IS NOT NULL AND [FailureMessage] IS NOT NULL)");
                    table.CheckConstraint("CK_CustomDomains_NormalizedHost", "[NormalizedHost] = LOWER([NormalizedHost]) AND RIGHT([NormalizedHost], 1) <> '.' AND CHARINDEX('..', [NormalizedHost]) = 0 AND [NormalizedHost] NOT LIKE '%[^a-z0-9.-]%'");
                    table.CheckConstraint("CK_CustomDomains_Status", "[Status] BETWEEN 1 AND 4");
                    table.CheckConstraint("CK_CustomDomains_Timestamps", "[UpdatedAtUtc] >= [CreatedAtUtc] AND ([VerificationRequestedAtUtc] IS NULL OR [VerificationRequestedAtUtc] >= [CreatedAtUtc]) AND ([LastVerificationAttemptAtUtc] IS NULL OR [LastVerificationAttemptAtUtc] >= [CreatedAtUtc])");
                    table.CheckConstraint("CK_CustomDomains_VerificationMethod", "[VerificationMethod] = 1");
                    table.CheckConstraint("CK_CustomDomains_VerifiedState", "[Status] <> 2 OR [VerifiedAtUtc] IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_CustomDomains_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomDomains_OwnerId_CreatedAtUtc",
                table: "CustomDomains",
                columns: new[] { "OwnerId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomDomains_Status_UpdatedAtUtc",
                table: "CustomDomains",
                columns: new[] { "Status", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_CustomDomains_NormalizedHost",
                table: "CustomDomains",
                column: "NormalizedHost",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomDomains");
        }
    }
}
