using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceMonitoring.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStudentIdFromAttendances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Students_StudentId",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_StudentId_StudentSectionAssignmentId_AttendanceDate_TeacherAssignmentId_SecretaryAssignmentId_AcademicPeriodId",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_StudentSectionAssignmentId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "Attendances");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_StudentSectionAssignmentId_AttendanceDate_TeacherAssignmentId_SecretaryAssignmentId_AcademicPeriodId",
                table: "Attendances",
                columns: new[] { "StudentSectionAssignmentId", "AttendanceDate", "TeacherAssignmentId", "SecretaryAssignmentId", "AcademicPeriodId" },
                unique: true,
                filter: "[StudentSectionAssignmentId] IS NOT NULL AND [TeacherAssignmentId] IS NOT NULL AND [SecretaryAssignmentId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Attendances_StudentSectionAssignmentId_AttendanceDate_TeacherAssignmentId_SecretaryAssignmentId_AcademicPeriodId",
                table: "Attendances");

            migrationBuilder.AddColumn<int>(
                name: "StudentId",
                table: "Attendances",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_StudentId_StudentSectionAssignmentId_AttendanceDate_TeacherAssignmentId_SecretaryAssignmentId_AcademicPeriodId",
                table: "Attendances",
                columns: new[] { "StudentId", "StudentSectionAssignmentId", "AttendanceDate", "TeacherAssignmentId", "SecretaryAssignmentId", "AcademicPeriodId" },
                unique: true,
                filter: "[StudentId] IS NOT NULL AND [StudentSectionAssignmentId] IS NOT NULL AND [TeacherAssignmentId] IS NOT NULL AND [SecretaryAssignmentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_StudentSectionAssignmentId",
                table: "Attendances",
                column: "StudentSectionAssignmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Students_StudentId",
                table: "Attendances",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id");
        }
    }
}
