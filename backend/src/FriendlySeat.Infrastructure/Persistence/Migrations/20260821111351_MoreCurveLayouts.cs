using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FriendlySeat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoreCurveLayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ArcAxisB",
                table: "Zones",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "CurveAmplitude",
                table: "Zones",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "CurveAngle",
                table: "Zones",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "CurvePhase",
                table: "Zones",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "CurveRowGap",
                table: "Zones",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "CurveSlantGap",
                table: "Zones",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "CurveWavelength",
                table: "Zones",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArcAxisB",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "CurveAmplitude",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "CurveAngle",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "CurvePhase",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "CurveRowGap",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "CurveSlantGap",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "CurveWavelength",
                table: "Zones");
        }
    }
}
