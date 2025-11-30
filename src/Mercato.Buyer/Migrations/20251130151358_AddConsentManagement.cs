using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercato.Buyer.Migrations
{
    /// <inheritdoc />
    public partial class AddConsentManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConsentTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConsentVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsentTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    ConsentText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsentVersions_ConsentTypes_ConsentTypeId",
                        column: x => x.ConsentTypeId,
                        principalTable: "ConsentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserConsents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ConsentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsGranted = table.Column<bool>(type: "bit", nullable: false),
                    ConsentedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConsents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserConsents_ConsentVersions_ConsentVersionId",
                        column: x => x.ConsentVersionId,
                        principalTable: "ConsentVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsentTypes_Code",
                table: "ConsentTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsentTypes_IsActive",
                table: "ConsentTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentVersions_ConsentTypeId_EffectiveTo",
                table: "ConsentVersions",
                columns: new[] { "ConsentTypeId", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsentVersions_ConsentTypeId_VersionNumber",
                table: "ConsentVersions",
                columns: new[] { "ConsentTypeId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserConsents_ConsentedAt",
                table: "UserConsents",
                column: "ConsentedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserConsents_ConsentVersionId",
                table: "UserConsents",
                column: "ConsentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserConsents_UserId",
                table: "UserConsents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserConsents_UserId_ConsentVersionId",
                table: "UserConsents",
                columns: new[] { "UserId", "ConsentVersionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserConsents");

            migrationBuilder.DropTable(
                name: "ConsentVersions");

            migrationBuilder.DropTable(
                name: "ConsentTypes");
        }
    }
}
