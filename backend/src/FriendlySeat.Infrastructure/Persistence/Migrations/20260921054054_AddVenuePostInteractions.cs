using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FriendlySeat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVenuePostInteractions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "VenuePosts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "VenuePostComments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LikeCount",
                table: "VenuePostComments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "VenuePostCommentLikes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CommentId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenuePostCommentLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VenuePostCommentLikes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VenuePostCommentLikes_VenuePostComments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "VenuePostComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VenuePostCommentLikes_CommentId_UserId",
                table: "VenuePostCommentLikes",
                columns: new[] { "CommentId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VenuePostCommentLikes_UserId",
                table: "VenuePostCommentLikes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VenuePostCommentLikes");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "VenuePosts");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "VenuePostComments");

            migrationBuilder.DropColumn(
                name: "LikeCount",
                table: "VenuePostComments");
        }
    }
}
