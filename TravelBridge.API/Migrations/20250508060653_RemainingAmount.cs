using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelBridge.API.Migrations
{
    /// <inheritdoc />
    public partial class RemainingAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RemainingAmount",
                table: "Reservations",
                type: "DECIMAL(10,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RemainingAmount",
                table: "Reservations");
        }
    }
}
