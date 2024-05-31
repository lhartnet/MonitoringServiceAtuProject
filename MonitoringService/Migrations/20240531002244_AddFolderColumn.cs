using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringService.Migrations
{
    /// <inheritdoc />
    public partial class AddFolderColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Folder",
                table: "SpecDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Folder",
                table: "SpecDetails");
        }
    }
}
