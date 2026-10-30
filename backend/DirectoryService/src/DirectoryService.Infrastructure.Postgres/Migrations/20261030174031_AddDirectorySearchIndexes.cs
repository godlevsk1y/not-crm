using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DirectoryService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectorySearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Complex value-object names use SQL indexes, like the existing department name index.
            migrationBuilder.Sql("""
                CREATE INDEX ix_locations_active_name_trgm ON locations
                USING gin (name gin_trgm_ops) WHERE deleted_at IS NULL;
                CREATE INDEX ix_positions_active_name_trgm ON positions
                USING gin (name gin_trgm_ops) WHERE deleted_at IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX ix_locations_active_name_trgm;
                DROP INDEX ix_positions_active_name_trgm;
                """);
        }
    }
}
