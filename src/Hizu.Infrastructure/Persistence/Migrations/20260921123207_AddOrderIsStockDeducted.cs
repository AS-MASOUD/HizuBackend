using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hizu.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderIsStockDeducted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsStockDeducted",
                table: "Orders",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsStockDeducted",
                table: "Orders");
        }
    }
}
