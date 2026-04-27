using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KazePH.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddResultDeclarations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "result_declarations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeclaringUserId = table.Column<string>(type: "character varying(450)", nullable: false),
                    DeclaredWinningSide = table.Column<string>(type: "text", nullable: false),
                    DeclaredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_result_declarations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_result_declarations_events_EventId",
                        column: x => x.EventId,
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_result_declarations_kaze_users_DeclaringUserId",
                        column: x => x.DeclaringUserId,
                        principalTable: "kaze_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_result_declarations_DeclaringUserId",
                table: "result_declarations",
                column: "DeclaringUserId");

            migrationBuilder.CreateIndex(
                name: "IX_result_declarations_EventId_DeclaringUserId",
                table: "result_declarations",
                columns: new[] { "EventId", "DeclaringUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "result_declarations");
        }
    }
}
