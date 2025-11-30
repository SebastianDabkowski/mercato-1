using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercato.Product.Migrations
{
    /// <inheritdoc />
    public partial class AddProductModeration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasVariants",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModeratedAt",
                table: "Products",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModeratedBy",
                table: "Products",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModerationReason",
                table: "Products",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModerationStatus",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ProductModerationDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Decision = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PreviousStatus = table.Column<int>(type: "int", nullable: false),
                    PreviousProductStatus = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductModerationDecisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariantAttributes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantAttributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariantAttributes_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Stock = table.Column<int>(type: "int", nullable: false),
                    Images = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AttributeCombination = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariants_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariantAttributeValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantAttributeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariantAttributeValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariantAttributeValues_ProductVariantAttributes_VariantAttributeId",
                        column: x => x.VariantAttributeId,
                        principalTable: "ProductVariantAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_ModerationStatus",
                table: "Products",
                column: "ModerationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ModerationStatus_Category",
                table: "Products",
                columns: new[] { "ModerationStatus", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductModerationDecisions_AdminUserId",
                table: "ProductModerationDecisions",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductModerationDecisions_CreatedAt",
                table: "ProductModerationDecisions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProductModerationDecisions_ProductId",
                table: "ProductModerationDecisions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantAttributes_ProductId",
                table: "ProductVariantAttributes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantAttributes_ProductId_Name",
                table: "ProductVariantAttributes",
                columns: new[] { "ProductId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantAttributeValues_VariantAttributeId",
                table: "ProductVariantAttributeValues",
                column: "VariantAttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariantAttributeValues_VariantAttributeId_Value",
                table: "ProductVariantAttributeValues",
                columns: new[] { "VariantAttributeId", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId",
                table: "ProductVariants",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_IsActive",
                table: "ProductVariants",
                columns: new[] { "ProductId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductModerationDecisions");

            migrationBuilder.DropTable(
                name: "ProductVariantAttributeValues");

            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropTable(
                name: "ProductVariantAttributes");

            migrationBuilder.DropIndex(
                name: "IX_Products_ModerationStatus",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_ModerationStatus_Category",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "HasVariants",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ModeratedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ModeratedBy",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ModerationReason",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ModerationStatus",
                table: "Products");
        }
    }
}
