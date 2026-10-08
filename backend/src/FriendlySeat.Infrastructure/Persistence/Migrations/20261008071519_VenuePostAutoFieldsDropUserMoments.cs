using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FriendlySeat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VenuePostAutoFieldsDropUserMoments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserMoments");

            migrationBuilder.AddColumn<bool>(
                name: "IsAuto",
                table: "VenuePosts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TargetKey",
                table: "VenuePosts",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VenuePosts_TargetKey",
                table: "VenuePosts",
                column: "TargetKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VenuePosts_TargetKey",
                table: "VenuePosts");

            migrationBuilder.DropColumn(
                name: "IsAuto",
                table: "VenuePosts");

            migrationBuilder.DropColumn(
                name: "TargetKey",
                table: "VenuePosts");

            migrationBuilder.CreateTable(
                name: "UserMoments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    TargetKey = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false),
                    VenueId = table.Column<long>(type: "bigint", nullable: true),
                    VenueName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMoments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMoments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserMoments_UserId",
                table: "UserMoments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMoments_VenueId_CreatedAt",
                table: "UserMoments",
                columns: new[] { "VenueId", "CreatedAt" });
        }
    }
}
