using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KazePH.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class NullableDisputeEvidenceUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_dispute_evidence_kaze_users_SubmittedByUserId",
                table: "dispute_evidence");

            migrationBuilder.AlterColumn<string>(
                name: "SubmittedByUserId",
                table: "dispute_evidence",
                type: "character varying(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)");

            migrationBuilder.AddForeignKey(
                name: "FK_dispute_evidence_kaze_users_SubmittedByUserId",
                table: "dispute_evidence",
                column: "SubmittedByUserId",
                principalTable: "kaze_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_dispute_evidence_kaze_users_SubmittedByUserId",
                table: "dispute_evidence");

            migrationBuilder.AlterColumn<string>(
                name: "SubmittedByUserId",
                table: "dispute_evidence",
                type: "character varying(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_dispute_evidence_kaze_users_SubmittedByUserId",
                table: "dispute_evidence",
                column: "SubmittedByUserId",
                principalTable: "kaze_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
