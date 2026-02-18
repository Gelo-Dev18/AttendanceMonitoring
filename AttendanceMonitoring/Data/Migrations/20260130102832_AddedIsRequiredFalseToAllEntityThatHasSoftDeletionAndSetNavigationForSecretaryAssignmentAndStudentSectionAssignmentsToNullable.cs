using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceMonitoring.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedIsRequiredFalseToAllEntityThatHasSoftDeletionAndSetNavigationForSecretaryAssignmentAndStudentSectionAssignmentsToNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TeacherAssignments_TeacherId_SectionSubjectId",
                table: "TeacherAssignments");

            migrationBuilder.DropIndex(
                name: "IX_SecretaryAssignments_SecretaryId_SectionId",
                table: "SecretaryAssignments");

            migrationBuilder.AlterColumn<string>(
                name: "TeacherId",
                table: "TeacherAssignments",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "SecretaryId",
                table: "SecretaryAssignments",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "RecordedById",
                table: "Attendances",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_TeacherId_SectionSubjectId",
                table: "TeacherAssignments",
                columns: new[] { "TeacherId", "SectionSubjectId" },
                unique: true,
                filter: "[TeacherId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SecretaryAssignments_SecretaryId_SectionId",
                table: "SecretaryAssignments",
                columns: new[] { "SecretaryId", "SectionId" },
                unique: true,
                filter: "[SecretaryId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TeacherAssignments_TeacherId_SectionSubjectId",
                table: "TeacherAssignments");

            migrationBuilder.DropIndex(
                name: "IX_SecretaryAssignments_SecretaryId_SectionId",
                table: "SecretaryAssignments");

            migrationBuilder.AlterColumn<string>(
                name: "TeacherId",
                table: "TeacherAssignments",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SecretaryId",
                table: "SecretaryAssignments",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RecordedById",
                table: "Attendances",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_TeacherId_SectionSubjectId",
                table: "TeacherAssignments",
                columns: new[] { "TeacherId", "SectionSubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecretaryAssignments_SecretaryId_SectionId",
                table: "SecretaryAssignments",
                columns: new[] { "SecretaryId", "SectionId" },
                unique: true);
        }
    }
}
