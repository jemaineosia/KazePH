using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KazePH.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOneVsOneFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChallengedUserId",
                table: "events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChallengedUsername",
                table: "events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatorSide",
                table: "events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CreatorStake",
                table: "events",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OpponentStake",
                table: "events",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChallengedUserId",
                table: "events");

            migrationBuilder.DropColumn(
                name: "ChallengedUsername",
                table: "events");

            migrationBuilder.DropColumn(
                name: "CreatorSide",
                table: "events");

            migrationBuilder.DropColumn(
                name: "CreatorStake",
                table: "events");

            migrationBuilder.DropColumn(
                name: "OpponentStake",
                table: "events");
        }
    }
}
