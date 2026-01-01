using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelBridge.API.Migrations
{
    /// <inheritdoc />
    public partial class DBChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReservationRate_PartyItemDB_SearchPartyId",
                table: "ReservationRate");

            migrationBuilder.DropForeignKey(
                name: "FK_ReservationRate_Reservations_ReservationId",
                table: "ReservationRate");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReservationRate",
                table: "ReservationRate");

            migrationBuilder.RenameTable(
                name: "ReservationRate",
                newName: "ReservationRates");

            migrationBuilder.RenameIndex(
                name: "IX_ReservationRate_SearchPartyId",
                table: "ReservationRates",
                newName: "IX_ReservationRates_SearchPartyId");

            migrationBuilder.RenameIndex(
                name: "IX_ReservationRate_ReservationId",
                table: "ReservationRates",
                newName: "IX_ReservationRates_ReservationId");

            migrationBuilder.AddColumn<int>(
                name: "BookingStatus",
                table: "Reservations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CheckInTime",
                table: "Reservations",
                type: "longtext",
                nullable: false,
                collation: "utf8mb4_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "CheckOutTime",
                table: "Reservations",
                type: "longtext",
                nullable: false,
                collation: "utf8mb4_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateFinalized",
                table: "Reservations",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Tel",
                keyValue: null,
                column: "Tel",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Tel",
                table: "Customers",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "LastName",
                keyValue: null,
                column: "LastName",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Customers",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50,
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "FirstName",
                keyValue: null,
                column: "FirstName",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Customers",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50,
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Email",
                keyValue: null,
                column: "Email",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Customers",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldMaxLength: 80,
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.AddColumn<int>(
                name: "BookingStatus",
                table: "ReservationRates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateFinalized",
                table: "ReservationRates",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "NetPrice",
                table: "ReservationRates",
                type: "DECIMAL(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ProviderResId",
                table: "ReservationRates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReservationRates",
                table: "ReservationRates",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReservationRates_PartyItemDB_SearchPartyId",
                table: "ReservationRates",
                column: "SearchPartyId",
                principalTable: "PartyItemDB",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReservationRates_Reservations_ReservationId",
                table: "ReservationRates",
                column: "ReservationId",
                principalTable: "Reservations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReservationRates_PartyItemDB_SearchPartyId",
                table: "ReservationRates");

            migrationBuilder.DropForeignKey(
                name: "FK_ReservationRates_Reservations_ReservationId",
                table: "ReservationRates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReservationRates",
                table: "ReservationRates");

            migrationBuilder.DropColumn(
                name: "BookingStatus",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "CheckInTime",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "CheckOutTime",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "DateFinalized",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "BookingStatus",
                table: "ReservationRates");

            migrationBuilder.DropColumn(
                name: "DateFinalized",
                table: "ReservationRates");

            migrationBuilder.DropColumn(
                name: "NetPrice",
                table: "ReservationRates");

            migrationBuilder.DropColumn(
                name: "ProviderResId",
                table: "ReservationRates");

            migrationBuilder.RenameTable(
                name: "ReservationRates",
                newName: "ReservationRate");

            migrationBuilder.RenameIndex(
                name: "IX_ReservationRates_SearchPartyId",
                table: "ReservationRate",
                newName: "IX_ReservationRate_SearchPartyId");

            migrationBuilder.RenameIndex(
                name: "IX_ReservationRates_ReservationId",
                table: "ReservationRate",
                newName: "IX_ReservationRate_ReservationId");

            migrationBuilder.AlterColumn<string>(
                name: "Tel",
                table: "Customers",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20)
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Customers",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Customers",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Customers",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldMaxLength: 80)
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReservationRate",
                table: "ReservationRate",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReservationRate_PartyItemDB_SearchPartyId",
                table: "ReservationRate",
                column: "SearchPartyId",
                principalTable: "PartyItemDB",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReservationRate_Reservations_ReservationId",
                table: "ReservationRate",
                column: "ReservationId",
                principalTable: "Reservations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
