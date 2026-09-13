using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FriendlySeat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSeatSwap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SeatSwapRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    VenueId = table.Column<long>(type: "bigint", nullable: false),
                    FloorId = table.Column<long>(type: "bigint", nullable: true),
                    AreaId = table.Column<long>(type: "bigint", nullable: true),
                    ZoneId = table.Column<long>(type: "bigint", nullable: true),
                    WantFloorId = table.Column<long>(type: "bigint", nullable: true),
                    WantAreaId = table.Column<long>(type: "bigint", nullable: true),
                    WantZoneId = table.Column<long>(type: "bigint", nullable: true),
                    Reasons = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpireAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MatchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MatchedResponseId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatSwapRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeatSwapRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeatSwapRequests_Venues_VenueId",
                        column: x => x.VenueId,
                        principalTable: "Venues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeatSwapResponses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    FloorId = table.Column<long>(type: "bigint", nullable: true),
                    AreaId = table.Column<long>(type: "bigint", nullable: true),
                    ZoneId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatSwapResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeatSwapResponses_SeatSwapRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "SeatSwapRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeatSwapResponses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeatSwapRequests_UserId_Status",
                table: "SeatSwapRequests",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SeatSwapRequests_VenueId_Status_CreatedAt",
                table: "SeatSwapRequests",
                columns: new[] { "VenueId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SeatSwapResponses_RequestId_UserId",
                table: "SeatSwapResponses",
                columns: new[] { "RequestId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_SeatSwapResponses_UserId",
                table: "SeatSwapResponses",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeatSwapResponses");

            migrationBuilder.DropTable(
                name: "SeatSwapRequests");
        }
    }
}
