using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercato.Product.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryIndexToProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Products_Category",
                table: "Products",
                column: "Category");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Category",
                table: "Products");
        }
    }
}
