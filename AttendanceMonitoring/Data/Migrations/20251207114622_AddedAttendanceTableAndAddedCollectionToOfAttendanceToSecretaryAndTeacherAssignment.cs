using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceMonitoring.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedAttendanceTableAndAddedCollectionToOfAttendanceToSecretaryAndTeacherAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Attendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AcademicPeriodId = table.Column<int>(type: "int", nullable: false),
                    AttendanceMarking = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExcuseReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttendanceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    RecordedById = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TeacherAssignmentId = table.Column<int>(type: "int", nullable: true),
                    SecretaryAssignmentId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendances", x => x.Id);
                    table.CheckConstraint("CK_Attendance_Assignment", "([TeacherAssignmentId] IS NOT NULL AND [SecretaryAssignmentId] IS NULL)OR ([TeacherAssignmentId] IS NULL AND [SecretaryAssignmentId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Attendances_AcademicPeriods_AcademicPeriodId",
                        column: x => x.AcademicPeriodId,
                        principalTable: "AcademicPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Attendances_AspNetUsers_RecordedById",
                        column: x => x.RecordedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Attendances_SecretaryAssignments_SecretaryAssignmentId",
                        column: x => x.SecretaryAssignmentId,
                        principalTable: "SecretaryAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Attendances_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Attendances_TeacherAssignments_TeacherAssignmentId",
                        column: x => x.TeacherAssignmentId,
                        principalTable: "TeacherAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_AcademicPeriodId",
                table: "Attendances",
                column: "AcademicPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_RecordedById",
                table: "Attendances",
                column: "RecordedById");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_SecretaryAssignmentId",
                table: "Attendances",
                column: "SecretaryAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_StudentId_AttendanceDate_TeacherAssignmentId_SecretaryAssignmentId_AcademicPeriodId",
                table: "Attendances",
                columns: new[] { "StudentId", "AttendanceDate", "TeacherAssignmentId", "SecretaryAssignmentId", "AcademicPeriodId" },
                unique: true,
                filter: "[TeacherAssignmentId] IS NOT NULL AND [SecretaryAssignmentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_TeacherAssignmentId",
                table: "Attendances",
                column: "TeacherAssignmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attendances");
        }
    }
}
