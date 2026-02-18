using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceMonitoring.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedFKAcademicPeriodIdOnStudentSectionAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AcademicPeriodId",
                table: "StudentSectionAssignments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentSectionAssignments_AcademicPeriodId",
                table: "StudentSectionAssignments",
                column: "AcademicPeriodId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentSectionAssignments_AcademicPeriods_AcademicPeriodId",
                table: "StudentSectionAssignments",
                column: "AcademicPeriodId",
                principalTable: "AcademicPeriods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentSectionAssignments_AcademicPeriods_AcademicPeriodId",
                table: "StudentSectionAssignments");

            migrationBuilder.DropIndex(
                name: "IX_StudentSectionAssignments_AcademicPeriodId",
                table: "StudentSectionAssignments");

            migrationBuilder.DropColumn(
                name: "AcademicPeriodId",
                table: "StudentSectionAssignments");
        }
    }
}
