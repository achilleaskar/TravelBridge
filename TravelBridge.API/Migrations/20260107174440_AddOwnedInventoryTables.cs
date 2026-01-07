using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelBridge.API.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnedInventoryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OwnedHotels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_general_ci"),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8mb4_general_ci"),
                    Description = table.Column<string>(type: "TEXT", nullable: true, collation: "utf8mb4_general_ci"),
                    Type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_general_ci"),
                    Rating = table.Column<int>(type: "int", nullable: true),
                    Latitude = table.Column<decimal>(type: "DECIMAL(10,7)", nullable: false),
                    Longitude = table.Column<decimal>(type: "DECIMAL(10,7)", nullable: false),
                    City = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_general_ci"),
                    Address = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true, collation: "utf8mb4_general_ci"),
                    Country = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true, collation: "utf8mb4_general_ci"),
                    PostalCode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true, collation: "utf8mb4_general_ci"),
                    CheckInTime = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true, collation: "utf8mb4_general_ci"),
                    CheckOutTime = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true, collation: "utf8mb4_general_ci"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime(6)", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnedHotels", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "OwnedRoomTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    HotelId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_general_ci"),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, collation: "utf8mb4_general_ci"),
                    Description = table.Column<string>(type: "TEXT", nullable: true, collation: "utf8mb4_general_ci"),
                    MaxAdults = table.Column<int>(type: "int", nullable: false),
                    MaxChildren = table.Column<int>(type: "int", nullable: false),
                    MaxTotalOccupancy = table.Column<int>(type: "int", nullable: false),
                    BasePricePerNight = table.Column<decimal>(type: "DECIMAL(10,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "datetime(6)", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnedRoomTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OwnedRoomTypes_OwnedHotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "OwnedHotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "OwnedInventoryDaily",
                columns: table => new
                {
                    RoomTypeId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalUnits = table.Column<int>(type: "int", nullable: false),
                    ClosedUnits = table.Column<int>(type: "int", nullable: false),
                    HeldUnits = table.Column<int>(type: "int", nullable: false),
                    ConfirmedUnits = table.Column<int>(type: "int", nullable: false),
                    PricePerNight = table.Column<decimal>(type: "DECIMAL(10,2)", nullable: true),
                    LastModifiedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnedInventoryDaily", x => new { x.RoomTypeId, x.Date });
                    table.CheckConstraint("CK_OwnedInventoryDaily_Counters", "ClosedUnits >= 0 AND HeldUnits >= 0 AND ConfirmedUnits >= 0 AND (ClosedUnits + HeldUnits + ConfirmedUnits) <= TotalUnits");
                    table.ForeignKey(
                        name: "FK_OwnedInventoryDaily_OwnedRoomTypes_RoomTypeId",
                        column: x => x.RoomTypeId,
                        principalTable: "OwnedRoomTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_OwnedHotel_Code",
                table: "OwnedHotels",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OwnedHotel_IsActive",
                table: "OwnedHotels",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_OwnedHotel_Location",
                table: "OwnedHotels",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_OwnedInventoryDaily_Date",
                table: "OwnedInventoryDaily",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_OwnedRoomType_HotelId",
                table: "OwnedRoomTypes",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_OwnedRoomType_HotelId_Code",
                table: "OwnedRoomTypes",
                columns: new[] { "HotelId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OwnedRoomType_IsActive",
                table: "OwnedRoomTypes",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OwnedInventoryDaily");

            migrationBuilder.DropTable(
                name: "OwnedRoomTypes");

            migrationBuilder.DropTable(
                name: "OwnedHotels");
        }
    }
}
