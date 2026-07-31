using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DirectoryService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    [SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments")]
    public partial class AddNameValueObjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_locations_name",
                table: "locations");

            migrationBuilder.CreateIndex(
                name: "IX_locations_name",
                table: "locations",
                column: "name",
                unique: true);
            
            migrationBuilder.CreateIndex(
                name: "IX_department_locations_id_location_id",
                table: "department_locations",
                columns: new[] { "id", "location_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_department_locations_id_location_id",
                table: "department_locations");
            
            migrationBuilder.DropIndex(
                name: "IX_locations_name",
                table: "locations");

            migrationBuilder.CreateIndex(
                name: "IX_locations_name",
                table: "locations",
                column: "name",
                unique: true);
        }
    }
}
