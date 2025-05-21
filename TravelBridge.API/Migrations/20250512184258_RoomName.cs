using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelBridge.API.Migrations
{
    /// <inheritdoc />
    public partial class RoomName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ReservationRates",
                type: "longtext",
                nullable: true,
                collation: "utf8mb4_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Customers",
                type: "longtext",
                nullable: false,
                collation: "utf8mb4_general_ci");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "ReservationRates");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Customers");
        }
    }
}
