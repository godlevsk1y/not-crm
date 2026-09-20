using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DirectoryService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationNameUniqueIndexAndLtreeGistIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "uq_location_name",
                table: "locations",
                column: "name",
                unique: true);
            
            migrationBuilder.Sql("""
                                 CREATE INDEX ix_department_path
                                 ON departments USING GIST (path);
                                 """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                                 DROP INDEX ix_department_path;
                                 """);
            
            migrationBuilder.DropIndex(
                name: "uq_location_name",
                table: "locations");
        }
    }
}
