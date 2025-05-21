using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelBridge.API.Migrations
{
    /// <inheritdoc />
    public partial class baordAndCancelInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BoardInfo",
                table: "ReservationRates",
                type: "longtext",
                nullable: true,
                collation: "utf8mb4_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "CancelationInfo",
                table: "ReservationRates",
                type: "longtext",
                nullable: true,
                collation: "utf8mb4_general_ci");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BoardInfo",
                table: "ReservationRates");

            migrationBuilder.DropColumn(
                name: "CancelationInfo",
                table: "ReservationRates");
        }
    }
}
