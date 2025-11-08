using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceMonitoring.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedSubjectTableForDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sections_Grades_GradesId",
                table: "Sections");

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubjectName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_Sections_Grades_GradesId",
                table: "Sections",
                column: "GradesId",
                principalTable: "Grades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sections_Grades_GradesId",
                table: "Sections");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.AddForeignKey(
                name: "FK_Sections_Grades_GradesId",
                table: "Sections",
                column: "GradesId",
                principalTable: "Grades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
