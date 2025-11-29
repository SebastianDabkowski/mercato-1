using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercato.Orders.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseTypeAndCaseItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FailedAt",
                table: "SellerSubOrders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancelledAt",
                table: "SellerSubOrderItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeliveredAt",
                table: "SellerSubOrderItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastUpdatedAt",
                table: "SellerSubOrderItems",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ShippedAt",
                table: "SellerSubOrderItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "SellerSubOrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FailedAt",
                table: "Orders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReturnRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CaseType = table.Column<int>(type: "int", nullable: false),
                    SellerSubOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BuyerId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SellerNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnRequests_SellerSubOrders_SellerSubOrderId",
                        column: x => x.SellerSubOrderId,
                        principalTable: "SellerSubOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShippingStatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SellerSubOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousStatus = table.Column<int>(type: "int", nullable: true),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TrackingNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ShippingCarrier = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShippingStatusHistories_SellerSubOrders_SellerSubOrderId",
                        column: x => x.SellerSubOrderId,
                        principalTable: "SellerSubOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SellerSubOrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseItems_ReturnRequests_ReturnRequestId",
                        column: x => x.ReturnRequestId,
                        principalTable: "ReturnRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseItems_SellerSubOrderItems_SellerSubOrderItemId",
                        column: x => x.SellerSubOrderItemId,
                        principalTable: "SellerSubOrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SellerSubOrderItems_SubOrderId_Status",
                table: "SellerSubOrderItems",
                columns: new[] { "SellerSubOrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseItems_ItemId_RequestId",
                table: "CaseItems",
                columns: new[] { "SellerSubOrderItemId", "ReturnRequestId" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseItems_ReturnRequestId",
                table: "CaseItems",
                column: "ReturnRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseItems_SellerSubOrderItemId",
                table: "CaseItems",
                column: "SellerSubOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_BuyerId",
                table: "ReturnRequests",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_CaseNumber",
                table: "ReturnRequests",
                column: "CaseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_SellerSubOrderId",
                table: "ReturnRequests",
                column: "SellerSubOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingStatusHistories_ChangedAt",
                table: "ShippingStatusHistories",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingStatusHistories_SellerSubOrderId",
                table: "ShippingStatusHistories",
                column: "SellerSubOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseItems");

            migrationBuilder.DropTable(
                name: "ShippingStatusHistories");

            migrationBuilder.DropTable(
                name: "ReturnRequests");

            migrationBuilder.DropIndex(
                name: "IX_SellerSubOrderItems_SubOrderId_Status",
                table: "SellerSubOrderItems");

            migrationBuilder.DropColumn(
                name: "FailedAt",
                table: "SellerSubOrders");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "SellerSubOrderItems");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "SellerSubOrderItems");

            migrationBuilder.DropColumn(
                name: "LastUpdatedAt",
                table: "SellerSubOrderItems");

            migrationBuilder.DropColumn(
                name: "ShippedAt",
                table: "SellerSubOrderItems");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "SellerSubOrderItems");

            migrationBuilder.DropColumn(
                name: "FailedAt",
                table: "Orders");
        }
    }
}
