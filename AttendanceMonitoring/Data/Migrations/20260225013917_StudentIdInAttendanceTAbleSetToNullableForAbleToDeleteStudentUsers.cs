using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceMonitoring.Data.Migrations
{
    /// <inheritdoc />
    public partial class StudentIdInAttendanceTAbleSetToNullableForAbleToDeleteStudentUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Attendances_StudentId_AttendanceDate_TeacherAssignmentId_SecretaryAssignmentId_AcademicPeriodId",
                table: "Attendances");

            migrationBuilder.AlterColumn<int>(
                name: "StudentId",
                table: "Attendances",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_StudentId_AttendanceDate_TeacherAssignmentId_SecretaryAssignmentId_AcademicPeriodId",
                table: "Attendances",
                columns: new[] { "StudentId", "AttendanceDate", "TeacherAssignmentId", "SecretaryAssignmentId", "AcademicPeriodId" },
                unique: true,
                filter: "[StudentId] IS NOT NULL AND [TeacherAssignmentId] IS NOT NULL AND [SecretaryAssignmentId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Attendances_StudentId_AttendanceDate_TeacherAssignmentId_SecretaryAssignmentId_AcademicPeriodId",
                table: "Attendances");

            migrationBuilder.AlterColumn<int>(
                name: "StudentId",
                table: "Attendances",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_StudentId_AttendanceDate_TeacherAssignmentId_SecretaryAssignmentId_AcademicPeriodId",
                table: "Attendances",
                columns: new[] { "StudentId", "AttendanceDate", "TeacherAssignmentId", "SecretaryAssignmentId", "AcademicPeriodId" },
                unique: true,
                filter: "[TeacherAssignmentId] IS NOT NULL AND [SecretaryAssignmentId] IS NOT NULL");
        }
    }
}
