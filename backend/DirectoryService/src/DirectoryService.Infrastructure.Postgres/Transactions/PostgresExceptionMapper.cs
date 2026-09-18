using DirectoryService.Core.Features.Departments;
using DirectoryService.Core.Features.Locations;
using DirectoryService.Core.Features.Positions;
using DirectoryService.Shared.Errors;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DirectoryService.Infrastructure.Postgres.Transactions;

internal static class PostgresExceptionMapper
{
    private const string LocationNameUniqueConstraint = 
        "uq_locations_name";
    private const string DepartmentLocationUniqueConstraint = 
        "uq_department_locations_department_id_location_id";
    private const string DepartmentPositionUniqueConstraint = 
        "uq_department_positions_department_id_position_id";
    
    private const string DepartmentParentForeignKeyConstraint =
        "fk_departments_parent";
    private const string DepartmentLocationDepartmentForeignKeyConstraint =
        "fk_department_locations_departments_department_id";
    private const string DepartmentLocationLocationForeignKeyConstraint =
        "fk_department_locations_locations_location_id";
    private const string DepartmentPositionDepartmentForeignKeyConstraint =
        "fk_department_positions_departments_department_id";
    private const string DepartmentPositionPositionForeignKeyConstraint =
        "fk_department_positions_positions_position_id";
    
    
    internal static bool TryMap(Exception exception, out Error error)
    {
        var postgresException = GetPostgresException(exception);

        if (postgresException is null)
        {
            error = null!;
            return false;
        }

        error = postgresException.SqlState switch
        {
            PostgresErrorCodes.UniqueViolation =>
                MapUniqueConstraintViolation(postgresException),

            PostgresErrorCodes.ForeignKeyViolation =>
                MapForeignKeyViolation(postgresException),

            _ => GeneralErrors.Internal(),
        };
        
        return true;
    }

    private static Error MapUniqueConstraintViolation(PostgresException exception) =>
        exception.ConstraintName switch
        {
            LocationNameUniqueConstraint => 
                LocationErrors.AlreadyExists(),

            DepartmentLocationUniqueConstraint => 
                DepartmentErrors.LocationAlreadyAdded(),

            DepartmentPositionUniqueConstraint => 
                DepartmentErrors.PositionAlreadyAdded(),

            _ => GeneralErrors.Internal(),
        };

    private static Error MapForeignKeyViolation(PostgresException exception) =>
        exception.ConstraintName switch
        {
            DepartmentParentForeignKeyConstraint => 
                DepartmentErrors.NotFound(),

            DepartmentLocationDepartmentForeignKeyConstraint
            or DepartmentPositionDepartmentForeignKeyConstraint => 
                DepartmentErrors.NotFound(),

            DepartmentLocationLocationForeignKeyConstraint => 
                LocationErrors.NotFound(),
            
            DepartmentPositionPositionForeignKeyConstraint =>
                PositionErrors.NotFound(),

            _ => GeneralErrors.Internal(),
        };
    
    
    private static PostgresException? GetPostgresException(Exception exception)
    {
        return exception switch
        {
            PostgresException postgresException => postgresException,

            DbUpdateException
            {
                InnerException: PostgresException postgresException,
            } => postgresException,

            _ => null,
        };
    }
}