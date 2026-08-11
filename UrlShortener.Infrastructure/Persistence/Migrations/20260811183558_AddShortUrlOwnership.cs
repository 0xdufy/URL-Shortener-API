using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UrlShortener.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShortUrlOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "ShortUrls",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShortUrls_OwnerId_CreatedAtUtc",
                table: "ShortUrls",
                columns: new[] { "OwnerId", "CreatedAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_ShortUrls_Users_OwnerId",
                table: "ShortUrls",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShortUrls_Users_OwnerId",
                table: "ShortUrls");

            migrationBuilder.DropIndex(
                name: "IX_ShortUrls_OwnerId_CreatedAtUtc",
                table: "ShortUrls");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "ShortUrls");
        }
    }
}
