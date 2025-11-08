using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceMonitoring.Data.Migrations
{
    /// <inheritdoc />
    public partial class SubjectClassModifiedSubjectNameToSubjectDescriptionAndSubjectCodeNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SubjectName",
                table: "Subjects",
                newName: "SubjectDescription");

            migrationBuilder.AlterColumn<string>(
                name: "SubjectCode",
                table: "Subjects",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SubjectDescription",
                table: "Subjects",
                newName: "SubjectName");

            migrationBuilder.AlterColumn<string>(
                name: "SubjectCode",
                table: "Subjects",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
