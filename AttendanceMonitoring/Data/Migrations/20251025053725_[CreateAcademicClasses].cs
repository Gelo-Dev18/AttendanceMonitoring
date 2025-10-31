using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceMonitoring.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateAcademicClasses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcademicClasses",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GradeLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SectionName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicClasses", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademicClasses");
        }
    }
}
