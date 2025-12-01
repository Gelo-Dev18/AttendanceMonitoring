using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceMonitoring.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedStudentAndStudentSectionAssignmentsAndTeacherAssignmentsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LRN = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MiddelName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sex = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    imageFilePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    imageFileData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeacherAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SectionSubjectId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherAssignments_AspNetUsers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherAssignments_SectionSubjects_SectionSubjectId",
                        column: x => x.SectionSubjectId,
                        principalTable: "SectionSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentSectionAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    SectionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentSectionAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentSectionAssignments_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentSectionAssignments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentSectionAssignments_SectionId",
                table: "StudentSectionAssignments",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentSectionAssignments_StudentId_SectionId",
                table: "StudentSectionAssignments",
                columns: new[] { "StudentId", "SectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_SectionSubjectId",
                table: "TeacherAssignments",
                column: "SectionSubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_TeacherId_SectionSubjectId",
                table: "TeacherAssignments",
                columns: new[] { "TeacherId", "SectionSubjectId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentSectionAssignments");

            migrationBuilder.DropTable(
                name: "TeacherAssignments");

            migrationBuilder.DropTable(
                name: "Students");
        }
    }
}
