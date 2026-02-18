using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceMonitoring.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedSoftDeletionToAllEntitiesAndAllRemainingCascadeDeleteBehaviorTurnedToRestrict : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SecretaryAssignments_AspNetUsers_SecretaryId",
                table: "SecretaryAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_SectionSubjects_Sections_SectionId",
                table: "SectionSubjects");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentSectionAssignments_Students_StudentId",
                table: "StudentSectionAssignments");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "TeacherAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TeacherAssignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Subjects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Subjects",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "StudentSectionAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "StudentSectionAssignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Students",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Students",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "SectionSubjects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SectionSubjects",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Sections",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Sections",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "SecretaryAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SecretaryAssignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Grades",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Grades",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_SecretaryAssignments_AspNetUsers_SecretaryId",
                table: "SecretaryAssignments",
                column: "SecretaryId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SectionSubjects_Sections_SectionId",
                table: "SectionSubjects",
                column: "SectionId",
                principalTable: "Sections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentSectionAssignments_Students_StudentId",
                table: "StudentSectionAssignments",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SecretaryAssignments_AspNetUsers_SecretaryId",
                table: "SecretaryAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_SectionSubjects_Sections_SectionId",
                table: "SectionSubjects");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentSectionAssignments_Students_StudentId",
                table: "StudentSectionAssignments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "TeacherAssignments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TeacherAssignments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "StudentSectionAssignments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "StudentSectionAssignments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SectionSubjects");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SectionSubjects");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Sections");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Sections");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "SecretaryAssignments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SecretaryAssignments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Grades");

            migrationBuilder.AddForeignKey(
                name: "FK_SecretaryAssignments_AspNetUsers_SecretaryId",
                table: "SecretaryAssignments",
                column: "SecretaryId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SectionSubjects_Sections_SectionId",
                table: "SectionSubjects",
                column: "SectionId",
                principalTable: "Sections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentSectionAssignments_Students_StudentId",
                table: "StudentSectionAssignments",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
