using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceMonitoring.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedFKAcademicPeriodIdOnSecretaryAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AcademicPeriodId",
                table: "SecretaryAssignments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecretaryAssignments_AcademicPeriodId",
                table: "SecretaryAssignments",
                column: "AcademicPeriodId");

            migrationBuilder.AddForeignKey(
                name: "FK_SecretaryAssignments_AcademicPeriods_AcademicPeriodId",
                table: "SecretaryAssignments",
                column: "AcademicPeriodId",
                principalTable: "AcademicPeriods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SecretaryAssignments_AcademicPeriods_AcademicPeriodId",
                table: "SecretaryAssignments");

            migrationBuilder.DropIndex(
                name: "IX_SecretaryAssignments_AcademicPeriodId",
                table: "SecretaryAssignments");

            migrationBuilder.DropColumn(
                name: "AcademicPeriodId",
                table: "SecretaryAssignments");
        }
    }
}
