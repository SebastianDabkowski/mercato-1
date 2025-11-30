using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercato.Product.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoModeration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FlagReason",
                table: "ProductImages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FlaggedAt",
                table: "ProductImages",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFlagged",
                table: "ProductImages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModeratedAt",
                table: "ProductImages",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModeratedBy",
                table: "ProductImages",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModerationReason",
                table: "ProductImages",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModerationStatus",
                table: "ProductImages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PhotoModerationDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductImageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Decision = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PreviousStatus = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoModerationDecisions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ModerationStatus",
                table: "ProductImages",
                column: "ModerationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ModerationStatus_IsFlagged",
                table: "ProductImages",
                columns: new[] { "ModerationStatus", "IsFlagged" });

            migrationBuilder.CreateIndex(
                name: "IX_PhotoModerationDecisions_AdminUserId",
                table: "PhotoModerationDecisions",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoModerationDecisions_CreatedAt",
                table: "PhotoModerationDecisions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoModerationDecisions_ProductId",
                table: "PhotoModerationDecisions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoModerationDecisions_ProductImageId",
                table: "PhotoModerationDecisions",
                column: "ProductImageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhotoModerationDecisions");

            migrationBuilder.DropIndex(
                name: "IX_ProductImages_ModerationStatus",
                table: "ProductImages");

            migrationBuilder.DropIndex(
                name: "IX_ProductImages_ModerationStatus_IsFlagged",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "FlagReason",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "FlaggedAt",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "IsFlagged",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "ModeratedAt",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "ModeratedBy",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "ModerationReason",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "ModerationStatus",
                table: "ProductImages");
        }
    }
}
