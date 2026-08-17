using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FriendlySeat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RelaxSeatCodeUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Seats_ZoneId_Code",
                table: "Seats");

            migrationBuilder.CreateIndex(
                name: "IX_Seats_ZoneId_Code",
                table: "Seats",
                columns: new[] { "ZoneId", "Code" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Seats_ZoneId_Code",
                table: "Seats");

            migrationBuilder.CreateIndex(
                name: "IX_Seats_ZoneId_Code",
                table: "Seats",
                columns: new[] { "ZoneId", "Code" },
                unique: true);
        }
    }
}
