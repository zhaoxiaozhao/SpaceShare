using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FriendlySeat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVenuePostCommentReply : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ParentCommentId",
                table: "VenuePostComments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReplyToUserId",
                table: "VenuePostComments",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VenuePostComments_ParentCommentId",
                table: "VenuePostComments",
                column: "ParentCommentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VenuePostComments_ParentCommentId",
                table: "VenuePostComments");

            migrationBuilder.DropColumn(
                name: "ParentCommentId",
                table: "VenuePostComments");

            migrationBuilder.DropColumn(
                name: "ReplyToUserId",
                table: "VenuePostComments");
        }
    }
}
