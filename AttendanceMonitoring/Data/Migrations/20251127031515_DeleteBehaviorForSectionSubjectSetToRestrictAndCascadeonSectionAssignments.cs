using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceMonitoring.Data.Migrations
{
    /// <inheritdoc />
    public partial class DeleteBehaviorForSectionSubjectSetToRestrictAndCascadeonSectionAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SectionSubjects_Subjects_SubjectId",
                table: "SectionSubjects");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentSectionAssignments_Students_StudentId",
                table: "StudentSectionAssignments");

            migrationBuilder.AddForeignKey(
                name: "FK_SectionSubjects_Subjects_SubjectId",
                table: "SectionSubjects",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentSectionAssignments_Students_StudentId",
                table: "StudentSectionAssignments",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SectionSubjects_Subjects_SubjectId",
                table: "SectionSubjects");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentSectionAssignments_Students_StudentId",
                table: "StudentSectionAssignments");

            migrationBuilder.AddForeignKey(
                name: "FK_SectionSubjects_Subjects_SubjectId",
                table: "SectionSubjects",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentSectionAssignments_Students_StudentId",
                table: "StudentSectionAssignments",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
