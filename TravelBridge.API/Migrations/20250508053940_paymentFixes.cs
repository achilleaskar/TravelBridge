using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelBridge.API.Migrations
{
    /// <inheritdoc />
    public partial class paymentFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PartialPaymentId",
                table: "Reservations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "RateId",
                table: "ReservationRate",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "PartialPaymentDB",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    prepayAmount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime(6)", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartialPaymentDB", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "NextPaymentDB",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DueDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    PartialPaymentDBId = table.Column<int>(type: "int", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "datetime(6)", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NextPaymentDB", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NextPaymentDB_PartialPaymentDB_PartialPaymentDBId",
                        column: x => x.PartialPaymentDBId,
                        principalTable: "PartialPaymentDB",
                        principalColumn: "Id");
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_PartialPaymentId",
                table: "Reservations",
                column: "PartialPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_NextPaymentDB_PartialPaymentDBId",
                table: "NextPaymentDB",
                column: "PartialPaymentDBId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_PartialPaymentDB_PartialPaymentId",
                table: "Reservations",
                column: "PartialPaymentId",
                principalTable: "PartialPaymentDB",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_PartialPaymentDB_PartialPaymentId",
                table: "Reservations");

            migrationBuilder.DropTable(
                name: "NextPaymentDB");

            migrationBuilder.DropTable(
                name: "PartialPaymentDB");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_PartialPaymentId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "PartialPaymentId",
                table: "Reservations");

            migrationBuilder.AlterColumn<int>(
                name: "RateId",
                table: "ReservationRate",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20)
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");
        }
    }
}
