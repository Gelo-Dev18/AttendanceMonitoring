using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceMonitoring.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedIsDeletedOnAcademicPeriodTableForSoftDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_AcademicPeriods_AcademicPeriodId",
                table: "Attendances");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "AcademicPeriods",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AcademicPeriods",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_AcademicPeriods_AcademicPeriodId",
                table: "Attendances",
                column: "AcademicPeriodId",
                principalTable: "AcademicPeriods",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_AcademicPeriods_AcademicPeriodId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AcademicPeriods");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AcademicPeriods");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_AcademicPeriods_AcademicPeriodId",
                table: "Attendances",
                column: "AcademicPeriodId",
                principalTable: "AcademicPeriods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
