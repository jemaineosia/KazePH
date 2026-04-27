using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KazePH.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "wallet_transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallet_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wallet_transactions_events_EventId",
                        column: x => x.EventId,
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_wallet_transactions_kaze_users_UserId",
                        column: x => x.UserId,
                        principalTable: "kaze_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_wallet_transactions_CreatedAt",
                table: "wallet_transactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_transactions_EventId",
                table: "wallet_transactions",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_transactions_UserId",
                table: "wallet_transactions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "wallet_transactions");
        }
    }
}
