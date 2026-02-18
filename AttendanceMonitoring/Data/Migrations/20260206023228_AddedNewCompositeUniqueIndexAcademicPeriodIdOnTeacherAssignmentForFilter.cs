using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceMonitoring.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedNewCompositeUniqueIndexAcademicPeriodIdOnTeacherAssignmentForFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TeacherAssignments_TeacherId_SectionSubjectId",
                table: "TeacherAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_TeacherId_SectionSubjectId_AcademicPeriodId",
                table: "TeacherAssignments",
                columns: new[] { "TeacherId", "SectionSubjectId", "AcademicPeriodId" },
                unique: true,
                filter: "[AcademicPeriodId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TeacherAssignments_TeacherId_SectionSubjectId_AcademicPeriodId",
                table: "TeacherAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_TeacherId_SectionSubjectId",
                table: "TeacherAssignments",
                columns: new[] { "TeacherId", "SectionSubjectId" },
                unique: true,
                filter: "[TeacherId] IS NOT NULL");
        }
    }
}
