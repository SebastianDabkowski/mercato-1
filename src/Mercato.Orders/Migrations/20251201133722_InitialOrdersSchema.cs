using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercato.Orders.Migrations
{
    /// <inheritdoc />
    public partial class InitialOrdersSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CaseItems_ReturnRequests_ReturnRequestId",
                table: "CaseItems");

            migrationBuilder.DropForeignKey(
                name: "FK_CaseItems_SellerSubOrderItems_SellerSubOrderItemId",
                table: "CaseItems");

            migrationBuilder.DropForeignKey(
                name: "FK_CaseMessages_ReturnRequests_ReturnRequestId",
                table: "CaseMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_CaseStatusHistories_ReturnRequests_ReturnRequestId",
                table: "CaseStatusHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Orders_OrderId",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductReviews_SellerSubOrderItems_SellerSubOrderItemId",
                table: "ProductReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductReviews_SellerSubOrders_SellerSubOrderId",
                table: "ProductReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_ReturnRequests_SellerSubOrders_SellerSubOrderId",
                table: "ReturnRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_SellerRatings_SellerSubOrders_SellerSubOrderId",
                table: "SellerRatings");

            migrationBuilder.DropForeignKey(
                name: "FK_SellerSubOrderItems_SellerSubOrders_SellerSubOrderId",
                table: "SellerSubOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SellerSubOrders_Orders_OrderId",
                table: "SellerSubOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ShippingStatusHistories_SellerSubOrders_SellerSubOrderId",
                table: "ShippingStatusHistories");

            migrationBuilder.AddColumn<string>(
                name: "BuyerDisplayName",
                table: "ProductReviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CaseItems_ReturnRequests_ReturnRequestId",
                table: "CaseItems",
                column: "ReturnRequestId",
                principalTable: "ReturnRequests",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CaseItems_SellerSubOrderItems_SellerSubOrderItemId",
                table: "CaseItems",
                column: "SellerSubOrderItemId",
                principalTable: "SellerSubOrderItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CaseMessages_ReturnRequests_ReturnRequestId",
                table: "CaseMessages",
                column: "ReturnRequestId",
                principalTable: "ReturnRequests",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CaseStatusHistories_ReturnRequests_ReturnRequestId",
                table: "CaseStatusHistories",
                column: "ReturnRequestId",
                principalTable: "ReturnRequests",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Orders_OrderId",
                table: "OrderItems",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductReviews_SellerSubOrderItems_SellerSubOrderItemId",
                table: "ProductReviews",
                column: "SellerSubOrderItemId",
                principalTable: "SellerSubOrderItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductReviews_SellerSubOrders_SellerSubOrderId",
                table: "ProductReviews",
                column: "SellerSubOrderId",
                principalTable: "SellerSubOrders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnRequests_SellerSubOrders_SellerSubOrderId",
                table: "ReturnRequests",
                column: "SellerSubOrderId",
                principalTable: "SellerSubOrders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SellerRatings_SellerSubOrders_SellerSubOrderId",
                table: "SellerRatings",
                column: "SellerSubOrderId",
                principalTable: "SellerSubOrders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SellerSubOrderItems_SellerSubOrders_SellerSubOrderId",
                table: "SellerSubOrderItems",
                column: "SellerSubOrderId",
                principalTable: "SellerSubOrders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SellerSubOrders_Orders_OrderId",
                table: "SellerSubOrders",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShippingStatusHistories_SellerSubOrders_SellerSubOrderId",
                table: "ShippingStatusHistories",
                column: "SellerSubOrderId",
                principalTable: "SellerSubOrders",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CaseItems_ReturnRequests_ReturnRequestId",
                table: "CaseItems");

            migrationBuilder.DropForeignKey(
                name: "FK_CaseItems_SellerSubOrderItems_SellerSubOrderItemId",
                table: "CaseItems");

            migrationBuilder.DropForeignKey(
                name: "FK_CaseMessages_ReturnRequests_ReturnRequestId",
                table: "CaseMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_CaseStatusHistories_ReturnRequests_ReturnRequestId",
                table: "CaseStatusHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Orders_OrderId",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductReviews_SellerSubOrderItems_SellerSubOrderItemId",
                table: "ProductReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductReviews_SellerSubOrders_SellerSubOrderId",
                table: "ProductReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_ReturnRequests_SellerSubOrders_SellerSubOrderId",
                table: "ReturnRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_SellerRatings_SellerSubOrders_SellerSubOrderId",
                table: "SellerRatings");

            migrationBuilder.DropForeignKey(
                name: "FK_SellerSubOrderItems_SellerSubOrders_SellerSubOrderId",
                table: "SellerSubOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SellerSubOrders_Orders_OrderId",
                table: "SellerSubOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ShippingStatusHistories_SellerSubOrders_SellerSubOrderId",
                table: "ShippingStatusHistories");

            migrationBuilder.DropColumn(
                name: "BuyerDisplayName",
                table: "ProductReviews");

            migrationBuilder.AddForeignKey(
                name: "FK_CaseItems_ReturnRequests_ReturnRequestId",
                table: "CaseItems",
                column: "ReturnRequestId",
                principalTable: "ReturnRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CaseItems_SellerSubOrderItems_SellerSubOrderItemId",
                table: "CaseItems",
                column: "SellerSubOrderItemId",
                principalTable: "SellerSubOrderItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CaseMessages_ReturnRequests_ReturnRequestId",
                table: "CaseMessages",
                column: "ReturnRequestId",
                principalTable: "ReturnRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CaseStatusHistories_ReturnRequests_ReturnRequestId",
                table: "CaseStatusHistories",
                column: "ReturnRequestId",
                principalTable: "ReturnRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Orders_OrderId",
                table: "OrderItems",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductReviews_SellerSubOrderItems_SellerSubOrderItemId",
                table: "ProductReviews",
                column: "SellerSubOrderItemId",
                principalTable: "SellerSubOrderItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductReviews_SellerSubOrders_SellerSubOrderId",
                table: "ProductReviews",
                column: "SellerSubOrderId",
                principalTable: "SellerSubOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnRequests_SellerSubOrders_SellerSubOrderId",
                table: "ReturnRequests",
                column: "SellerSubOrderId",
                principalTable: "SellerSubOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SellerRatings_SellerSubOrders_SellerSubOrderId",
                table: "SellerRatings",
                column: "SellerSubOrderId",
                principalTable: "SellerSubOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SellerSubOrderItems_SellerSubOrders_SellerSubOrderId",
                table: "SellerSubOrderItems",
                column: "SellerSubOrderId",
                principalTable: "SellerSubOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SellerSubOrders_Orders_OrderId",
                table: "SellerSubOrders",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShippingStatusHistories_SellerSubOrders_SellerSubOrderId",
                table: "ShippingStatusHistories",
                column: "SellerSubOrderId",
                principalTable: "SellerSubOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
