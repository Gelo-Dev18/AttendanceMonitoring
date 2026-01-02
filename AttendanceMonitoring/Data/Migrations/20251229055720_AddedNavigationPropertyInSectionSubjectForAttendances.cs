using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceMonitoring.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedNavigationPropertyInSectionSubjectForAttendances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_SectionSubjects_SectionSubjectId",
                table: "Attendances");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_SectionSubjects_SectionSubjectId",
                table: "Attendances",
                column: "SectionSubjectId",
                principalTable: "SectionSubjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_SectionSubjects_SectionSubjectId",
                table: "Attendances");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_SectionSubjects_SectionSubjectId",
                table: "Attendances",
                column: "SectionSubjectId",
                principalTable: "SectionSubjects",
                principalColumn: "Id");
        }
    }
}
