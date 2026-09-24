using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DirectoryService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddGinIndexOnDepartmentName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
            migrationBuilder.Sql("CREATE INDEX ix_departments_active_name_trgm ON departments " +
                                 "USING gin (name gin_trgm_ops) WHERE deleted_at IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX ix_departments_active_name_trgm");
            migrationBuilder.Sql("DROP EXTENSION IF EXISTS pg_trgm;");
        }
    }
}
