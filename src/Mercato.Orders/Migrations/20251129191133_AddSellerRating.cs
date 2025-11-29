using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercato.Orders.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SellerSubOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SellerSubOrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuyerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    ReviewText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductReviews_SellerSubOrderItems_SellerSubOrderItemId",
                        column: x => x.SellerSubOrderItemId,
                        principalTable: "SellerSubOrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductReviews_SellerSubOrders_SellerSubOrderId",
                        column: x => x.SellerSubOrderId,
                        principalTable: "SellerSubOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SellerRatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SellerSubOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuyerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellerRatings_SellerSubOrders_SellerSubOrderId",
                        column: x => x.SellerSubOrderId,
                        principalTable: "SellerSubOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_BuyerId",
                table: "ProductReviews",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_OrderId",
                table: "ProductReviews",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_ProductId",
                table: "ProductReviews",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_ProductId_Status",
                table: "ProductReviews",
                columns: new[] { "ProductId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_SellerSubOrderId",
                table: "ProductReviews",
                column: "SellerSubOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_SellerSubOrderItemId",
                table: "ProductReviews",
                column: "SellerSubOrderItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_Status",
                table: "ProductReviews",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_StoreId",
                table: "ProductReviews",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerRatings_BuyerId",
                table: "SellerRatings",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerRatings_OrderId",
                table: "SellerRatings",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerRatings_SellerSubOrderId",
                table: "SellerRatings",
                column: "SellerSubOrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SellerRatings_StoreId",
                table: "SellerRatings",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerRatings_StoreId_CreatedAt",
                table: "SellerRatings",
                columns: new[] { "StoreId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductReviews");

            migrationBuilder.DropTable(
                name: "SellerRatings");
        }
    }
}
