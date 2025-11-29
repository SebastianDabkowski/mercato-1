using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercato.Orders.Migrations
{
    /// <inheritdoc />
    public partial class AddCaseResolutionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LinkedRefundId",
                table: "ReturnRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundAmount",
                table: "ReturnRequests",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionReason",
                table: "ReturnRequests",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResolutionType",
                table: "ReturnRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResolvedAt",
                table: "ReturnRequests",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_LinkedRefundId",
                table: "ReturnRequests",
                column: "LinkedRefundId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReturnRequests_LinkedRefundId",
                table: "ReturnRequests");

            migrationBuilder.DropColumn(
                name: "LinkedRefundId",
                table: "ReturnRequests");

            migrationBuilder.DropColumn(
                name: "RefundAmount",
                table: "ReturnRequests");

            migrationBuilder.DropColumn(
                name: "ResolutionReason",
                table: "ReturnRequests");

            migrationBuilder.DropColumn(
                name: "ResolutionType",
                table: "ReturnRequests");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "ReturnRequests");
        }
    }
}
