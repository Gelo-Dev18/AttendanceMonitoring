using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceMonitoring.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSectionSubjectIdToAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SectionSubjectId",
                table: "Attendances",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_SectionSubjectId",
                table: "Attendances",
                column: "SectionSubjectId");

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

            migrationBuilder.DropIndex(
                name: "IX_Attendances_SectionSubjectId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "SectionSubjectId",
                table: "Attendances");
        }
    }
}
