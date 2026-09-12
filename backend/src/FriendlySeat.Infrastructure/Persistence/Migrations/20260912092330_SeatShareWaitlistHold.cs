using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FriendlySeat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeatShareWaitlistHold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "HoldForUserId",
                table: "SeatShares",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AreaId",
                table: "FloorPois",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FloorPois_AreaId",
                table: "FloorPois",
                column: "AreaId");

            migrationBuilder.AddForeignKey(
                name: "FK_FloorPois_Areas_AreaId",
                table: "FloorPois",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FloorPois_Areas_AreaId",
                table: "FloorPois");

            migrationBuilder.DropIndex(
                name: "IX_FloorPois_AreaId",
                table: "FloorPois");

            migrationBuilder.DropColumn(
                name: "HoldForUserId",
                table: "SeatShares");

            migrationBuilder.DropColumn(
                name: "AreaId",
                table: "FloorPois");
        }
    }
}
