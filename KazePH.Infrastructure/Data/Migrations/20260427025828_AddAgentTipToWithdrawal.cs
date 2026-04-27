using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KazePH.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentTipToWithdrawal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AgentTip",
                table: "withdrawal_requests",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgentTip",
                table: "withdrawal_requests");
        }
    }
}
