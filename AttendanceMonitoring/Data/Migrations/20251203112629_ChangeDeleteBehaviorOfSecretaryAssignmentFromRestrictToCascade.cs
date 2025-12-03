using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceMonitoring.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeDeleteBehaviorOfSecretaryAssignmentFromRestrictToCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SecretaryAssignments_AspNetUsers_SecretaryId",
                table: "SecretaryAssignments");

            migrationBuilder.AddForeignKey(
                name: "FK_SecretaryAssignments_AspNetUsers_SecretaryId",
                table: "SecretaryAssignments",
                column: "SecretaryId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SecretaryAssignments_AspNetUsers_SecretaryId",
                table: "SecretaryAssignments");

            migrationBuilder.AddForeignKey(
                name: "FK_SecretaryAssignments_AspNetUsers_SecretaryId",
                table: "SecretaryAssignments",
                column: "SecretaryId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
