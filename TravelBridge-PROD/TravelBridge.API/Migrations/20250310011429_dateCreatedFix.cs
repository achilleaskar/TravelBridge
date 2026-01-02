using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelBridge.API.Migrations
{
    /// <inheritdoc />
    public partial class dateCreatedFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "DateCreated",
                table: "Reservations",
                type: "datetime(6)",
                nullable: true,
                defaultValueSql: "CONVERT_TZ(NOW(), 'UTC', 'Europe/Athens')",
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldDefaultValueSql: "CONVERT_TZ(NOW(), 'UTC', 'Europe/Athens')");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateCreated",
                table: "ReservationRate",
                type: "datetime(6)",
                nullable: true,
                defaultValueSql: "CONVERT_TZ(NOW(), 'UTC', 'Europe/Athens')",
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldDefaultValueSql: "CONVERT_TZ(NOW(), 'UTC', 'Europe/Athens')");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateCreated",
                table: "Payments",
                type: "datetime(6)",
                nullable: true,
                defaultValueSql: "CONVERT_TZ(NOW(), 'UTC', 'Europe/Athens')",
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldDefaultValueSql: "CONVERT_TZ(NOW(), 'UTC', 'Europe/Athens')");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateCreated",
                table: "Customers",
                type: "datetime(6)",
                nullable: true,
                defaultValueSql: "CONVERT_TZ(NOW(), 'UTC', 'Europe/Athens')",
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldDefaultValueSql: "CONVERT_TZ(NOW(), 'UTC', 'Europe/Athens')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "DateCreated",
                table: "Reservations",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CONVERT_TZ(NOW(), 'UTC', 'Europe/Athens')",
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true,
                oldDefaultValueSql: "CONVERT_TZ(NOW(), 'UTC', 'Europe/Athens')");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateCreated",
                table: "ReservationRate",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CONVERT_TZ(NOW(), 'UTC', 'Europe/Athens')",
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true,
                oldDefaultValueSql: "CONVERT_TZ(NOW(), 'UTC', 'Europe/Athens')");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateCreated",
                table: "Payments",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CONVERT_TZ(NOW(), 'UTC', 'Europe/Athens')",
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true,
                oldDefaultValueSql: "CONVERT_TZ(NOW(), 'UTC', 'Europe/Athens')");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateCreated",
                table: "Customers",
                type: "datetime(6)",
                nullable: false,
                defaultValueSql: "CONVERT_TZ(NOW(), 'UTC', 'Europe/Athens')",
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true,
                oldDefaultValueSql: "CONVERT_TZ(NOW(), 'UTC', 'Europe/Athens')");
        }
    }
}
