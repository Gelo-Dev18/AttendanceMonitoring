using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceMonitoring.Data.Migrations
{
    /// <inheritdoc />
    public partial class StudentIdFromStudentSectionAssignmentSetToNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentSectionAssignments_StudentId_SectionId",
                table: "StudentSectionAssignments");

            migrationBuilder.AlterColumn<int>(
                name: "StudentId",
                table: "StudentSectionAssignments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_StudentSectionAssignments_StudentId_SectionId",
                table: "StudentSectionAssignments",
                columns: new[] { "StudentId", "SectionId" },
                unique: true,
                filter: "[StudentId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudentSectionAssignments_StudentId_SectionId",
                table: "StudentSectionAssignments");

            migrationBuilder.AlterColumn<int>(
                name: "StudentId",
                table: "StudentSectionAssignments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentSectionAssignments_StudentId_SectionId",
                table: "StudentSectionAssignments",
                columns: new[] { "StudentId", "SectionId" },
                unique: true);
        }
    }
}
