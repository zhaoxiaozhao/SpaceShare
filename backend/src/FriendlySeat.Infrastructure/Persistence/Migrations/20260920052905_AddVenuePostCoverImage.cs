using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FriendlySeat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVenuePostCoverImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverImage",
                table: "VenuePosts",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverImage",
                table: "VenuePosts");
        }
    }
}
