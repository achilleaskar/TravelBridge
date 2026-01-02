using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelBridge.API.Migrations
{
    /// <inheritdoc />
    public partial class decimalFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "prepayAmount",
                table: "PartialPaymentDB",
                type: "DECIMAL(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "prepayAmount",
                table: "PartialPaymentDB",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "DECIMAL(10,2)");
        }
    }
}
