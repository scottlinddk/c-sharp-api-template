using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Template.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IndexOrdersByCreatorAndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_CreatedBy",
                table: "orders");

            migrationBuilder.CreateIndex(
                name: "IX_orders_CreatedBy_CreatedAt",
                table: "orders",
                columns: new[] { "CreatedBy", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_CreatedBy_CreatedAt",
                table: "orders");

            migrationBuilder.CreateIndex(
                name: "IX_orders_CreatedBy",
                table: "orders",
                column: "CreatedBy");
        }
    }
}
