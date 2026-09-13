using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FriendlySeat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWaitlistPreferenceExpireAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpireAt",
                table: "WaitlistPreferences",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpireAt",
                table: "WaitlistPreferences");
        }
    }
}
