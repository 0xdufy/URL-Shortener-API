using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrlShortener.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomDomainRouting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CustomDomainId",
                table: "ShortUrls",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_CustomDomains_Id_OwnerId",
                table: "CustomDomains",
                columns: new[] { "Id", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_ShortUrls_CustomDomainId_OwnerId",
                table: "ShortUrls",
                columns: new[] { "CustomDomainId", "OwnerId" });

            migrationBuilder.CreateIndex(
                name: "IX_ShortUrls_CustomDomainId_ShortCode",
                table: "ShortUrls",
                columns: new[] { "CustomDomainId", "ShortCode" });

            migrationBuilder.AddForeignKey(
                name: "FK_ShortUrls_CustomDomains_CustomDomainId_OwnerId",
                table: "ShortUrls",
                columns: new[] { "CustomDomainId", "OwnerId" },
                principalTable: "CustomDomains",
                principalColumns: new[] { "Id", "OwnerId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShortUrls_CustomDomains_CustomDomainId_OwnerId",
                table: "ShortUrls");

            migrationBuilder.DropIndex(
                name: "IX_ShortUrls_CustomDomainId_OwnerId",
                table: "ShortUrls");

            migrationBuilder.DropIndex(
                name: "IX_ShortUrls_CustomDomainId_ShortCode",
                table: "ShortUrls");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_CustomDomains_Id_OwnerId",
                table: "CustomDomains");

            migrationBuilder.DropColumn(
                name: "CustomDomainId",
                table: "ShortUrls");
        }
    }
}
