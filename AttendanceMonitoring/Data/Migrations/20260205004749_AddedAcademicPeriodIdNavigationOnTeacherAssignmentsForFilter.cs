using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceMonitoring.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedAcademicPeriodIdNavigationOnTeacherAssignmentsForFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AcademicPeriodId",
                table: "TeacherAssignments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_AcademicPeriodId",
                table: "TeacherAssignments",
                column: "AcademicPeriodId");

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherAssignments_AcademicPeriods_AcademicPeriodId",
                table: "TeacherAssignments",
                column: "AcademicPeriodId",
                principalTable: "AcademicPeriods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TeacherAssignments_AcademicPeriods_AcademicPeriodId",
                table: "TeacherAssignments");

            migrationBuilder.DropIndex(
                name: "IX_TeacherAssignments_AcademicPeriodId",
                table: "TeacherAssignments");

            migrationBuilder.DropColumn(
                name: "AcademicPeriodId",
                table: "TeacherAssignments");
        }
    }
}
