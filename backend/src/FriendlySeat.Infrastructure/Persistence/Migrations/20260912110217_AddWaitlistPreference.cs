using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FriendlySeat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWaitlistPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WaitlistPreferences",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    VenueId = table.Column<long>(type: "bigint", nullable: false),
                    FloorId = table.Column<long>(type: "bigint", nullable: true),
                    AreaId = table.Column<long>(type: "bigint", nullable: true),
                    Preference = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BookedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReservationId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaitlistPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaitlistPreferences_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WaitlistPreferences_Floors_FloorId",
                        column: x => x.FloorId,
                        principalTable: "Floors",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WaitlistPreferences_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WaitlistPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WaitlistPreferences_Venues_VenueId",
                        column: x => x.VenueId,
                        principalTable: "Venues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistPreferences_AreaId",
                table: "WaitlistPreferences",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistPreferences_FloorId",
                table: "WaitlistPreferences",
                column: "FloorId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistPreferences_ReservationId",
                table: "WaitlistPreferences",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistPreferences_UserId_Status",
                table: "WaitlistPreferences",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistPreferences_VenueId_Status_CreatedAt",
                table: "WaitlistPreferences",
                columns: new[] { "VenueId", "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WaitlistPreferences");
        }
    }
}
