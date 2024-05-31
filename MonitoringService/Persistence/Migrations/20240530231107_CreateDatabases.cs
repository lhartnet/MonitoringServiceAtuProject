using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringService.Migrations
{
    /// <inheritdoc />
    public partial class CreateDatabases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SpecDetail",
                table: "SpecDetail");

            migrationBuilder.RenameTable(
                name: "SpecDetail",
                newName: "SpecDetails");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SpecDetails",
                table: "SpecDetails",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SpecDetails",
                table: "SpecDetails");

            migrationBuilder.RenameTable(
                name: "SpecDetails",
                newName: "SpecDetail");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SpecDetail",
                table: "SpecDetail",
                column: "Id");
        }
    }
}
