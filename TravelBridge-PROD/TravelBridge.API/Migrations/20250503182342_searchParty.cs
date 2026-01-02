using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelBridge.API.Migrations
{
    /// <inheritdoc />
    public partial class searchParty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SearchPartyId",
                table: "ReservationRate",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PartyItemDB",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Adults = table.Column<int>(type: "int", nullable: false),
                    Children = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_general_ci"),
                    Party = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, collation: "utf8mb4_general_ci"),
                    DateCreated = table.Column<DateTime>(type: "datetime(6)", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyItemDB", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationRate_SearchPartyId",
                table: "ReservationRate",
                column: "SearchPartyId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReservationRate_PartyItemDB_SearchPartyId",
                table: "ReservationRate",
                column: "SearchPartyId",
                principalTable: "PartyItemDB",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReservationRate_PartyItemDB_SearchPartyId",
                table: "ReservationRate");

            migrationBuilder.DropTable(
                name: "PartyItemDB");

            migrationBuilder.DropIndex(
                name: "IX_ReservationRate_SearchPartyId",
                table: "ReservationRate");

            migrationBuilder.DropColumn(
                name: "SearchPartyId",
                table: "ReservationRate");
        }
    }
}
