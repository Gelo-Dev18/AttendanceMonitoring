using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendanceMonitoring.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddedTrackInSection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Track",
                table: "Sections",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Track",
                table: "Sections");
        }
    }
}
