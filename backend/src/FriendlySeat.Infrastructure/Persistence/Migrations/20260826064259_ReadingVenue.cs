using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FriendlySeat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReadingVenue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "VenueId",
                table: "ReadingSessions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VenueName",
                table: "ReadingSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "VenueId",
                table: "ReadingBooks",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VenueName",
                table: "ReadingBooks",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReadingSessions_VenueId",
                table: "ReadingSessions",
                column: "VenueId");

            migrationBuilder.CreateIndex(
                name: "IX_ReadingBooks_VenueId",
                table: "ReadingBooks",
                column: "VenueId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReadingBooks_Venues_VenueId",
                table: "ReadingBooks",
                column: "VenueId",
                principalTable: "Venues",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReadingSessions_Venues_VenueId",
                table: "ReadingSessions",
                column: "VenueId",
                principalTable: "Venues",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReadingBooks_Venues_VenueId",
                table: "ReadingBooks");

            migrationBuilder.DropForeignKey(
                name: "FK_ReadingSessions_Venues_VenueId",
                table: "ReadingSessions");

            migrationBuilder.DropIndex(
                name: "IX_ReadingSessions_VenueId",
                table: "ReadingSessions");

            migrationBuilder.DropIndex(
                name: "IX_ReadingBooks_VenueId",
                table: "ReadingBooks");

            migrationBuilder.DropColumn(
                name: "VenueId",
                table: "ReadingSessions");

            migrationBuilder.DropColumn(
                name: "VenueName",
                table: "ReadingSessions");

            migrationBuilder.DropColumn(
                name: "VenueId",
                table: "ReadingBooks");

            migrationBuilder.DropColumn(
                name: "VenueName",
                table: "ReadingBooks");
        }
    }
}
