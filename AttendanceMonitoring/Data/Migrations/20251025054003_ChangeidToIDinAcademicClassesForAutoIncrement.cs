using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceMonitoring.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeidToIDinAcademicClassesForAutoIncrement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "id",
                table: "AcademicClasses",
                newName: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "AcademicClasses",
                newName: "id");
        }
    }
}
