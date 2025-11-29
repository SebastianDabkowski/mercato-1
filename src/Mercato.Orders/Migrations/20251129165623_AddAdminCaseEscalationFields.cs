using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercato.Orders.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminCaseEscalationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminDecision",
                table: "ReturnRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AdminDecisionAt",
                table: "ReturnRequests",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminDecisionByUserId",
                table: "ReturnRequests",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminDecisionReason",
                table: "ReturnRequests",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EscalatedAt",
                table: "ReturnRequests",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EscalatedByUserId",
                table: "ReturnRequests",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EscalationReason",
                table: "ReturnRequests",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CaseStatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldStatus = table.Column<int>(type: "int", nullable: false),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ChangedByRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseStatusHistories_ReturnRequests_ReturnRequestId",
                        column: x => x.ReturnRequestId,
                        principalTable: "ReturnRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRequests_Status",
                table: "ReturnRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CaseStatusHistories_RequestId_ChangedAt",
                table: "CaseStatusHistories",
                columns: new[] { "ReturnRequestId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseStatusHistories_ReturnRequestId",
                table: "CaseStatusHistories",
                column: "ReturnRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_ReturnRequests_Status",
                table: "ReturnRequests");

            migrationBuilder.DropColumn(
                name: "AdminDecision",
                table: "ReturnRequests");

            migrationBuilder.DropColumn(
                name: "AdminDecisionAt",
                table: "ReturnRequests");

            migrationBuilder.DropColumn(
                name: "AdminDecisionByUserId",
                table: "ReturnRequests");

            migrationBuilder.DropColumn(
                name: "AdminDecisionReason",
                table: "ReturnRequests");

            migrationBuilder.DropColumn(
                name: "EscalatedAt",
                table: "ReturnRequests");

            migrationBuilder.DropColumn(
                name: "EscalatedByUserId",
                table: "ReturnRequests");

            migrationBuilder.DropColumn(
                name: "EscalationReason",
                table: "ReturnRequests");
        }
    }
}
