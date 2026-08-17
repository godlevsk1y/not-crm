using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DirectoryService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class CascadeDeleteDepartmentChildren : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_departments_parent",
                table: "departments");

            migrationBuilder.AddForeignKey(
                name: "fk_departments_parent",
                table: "departments",
                column: "parent_id",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_departments_parent",
                table: "departments");

            migrationBuilder.AddForeignKey(
                name: "fk_departments_parent",
                table: "departments",
                column: "parent_id",
                principalTable: "departments",
                principalColumn: "id");
        }
    }
}
