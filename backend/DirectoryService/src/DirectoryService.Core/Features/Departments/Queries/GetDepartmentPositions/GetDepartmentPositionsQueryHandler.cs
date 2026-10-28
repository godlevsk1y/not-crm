using System.Data;
using System.Diagnostics;
using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Contracts.Positions.QueryContracts;
using DirectoryService.Core.Database;
using Shared.Kernel.Errors;
using Shared.Kernel.Results;
using FluentValidation;
using Shared.Core.Abstractions;
using Shared.Core.Extensions;

namespace DirectoryService.Core.Features.Departments.Queries.GetDepartmentPositions;

public class GetDepartmentPositionsQueryHandler : IQueryHandler<GetDepartmentPositionsQuery, Result<PagedResult<PositionListItemDto>, Error>>
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IValidator<GetDepartmentPositionsQuery> _validator;

    public GetDepartmentPositionsQueryHandler(
        IDbConnectionFactory connectionFactory,
        IValidator<GetDepartmentPositionsQuery> validator)
    {
        _connectionFactory = connectionFactory;
        _validator = validator;
    }

    public async Task<Result<PagedResult<PositionListItemDto>, Error>> Handle(
        GetDepartmentPositionsQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        var departmentExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT EXISTS (SELECT 1 FROM departments WHERE id = @departmentId AND deleted_at IS NULL)",
            new { departmentId = request.DepartmentId },
            cancellationToken: cancellationToken));

        if (!departmentExists)
        {
            return DepartmentErrors.NotFound(request.DepartmentId);
        }

        var parameters = new DynamicParameters();
        parameters.Add("departmentId", request.DepartmentId, DbType.Guid);
        parameters.Add("pageSize", request.PageSize, DbType.Int32);
        parameters.Add("offset", (long)(request.Page - 1) * request.PageSize, DbType.Int64);

        const string whereExpression = """
                                       WHERE p.deleted_at IS NULL
                                         AND EXISTS (
                                             SELECT 1 FROM department_positions dp
                                             WHERE dp.position_id = p.id AND dp.department_id = @departmentId
                                         )
                                       """;

        var sortBy = request.SortBy switch
        {
            "name" => "p.name",
            "createdAt" => "p.created_at",
            _ => throw new UnreachableException(),
        };

        var sortDirection = request.SortDirection switch
        {
            "asc" => "ASC",
            "desc" => "DESC",
            _ => throw new UnreachableException(),
        };

        var sql = $"""
                  SELECT p.id,
                         p.name,
                         p.created_at,
                         count(p.id) OVER () AS total_count
                  FROM positions p
                  {whereExpression}
                  ORDER BY {sortBy} {sortDirection}, p.id
                  LIMIT @pageSize OFFSET @offset
                  """;

        long? totalCount = null;

        var positionListItems = await connection.QueryAsync<PositionListItemDto, long, PositionListItemDto>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken),
            map: (dto, count) =>
            {
                totalCount ??= count;
                return dto;
            },
            splitOn: "total_count");

        if (totalCount is null && request.Page > 1)
        {
            var countSql = $"""
                            SELECT count(*)
                            FROM positions p
                            {whereExpression}
                            """;

            totalCount = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
                countSql, parameters, cancellationToken: cancellationToken));
        }

        return new PagedResult<PositionListItemDto>(
            positionListItems,
            request.Page,
            request.PageSize,
            totalCount ?? 0
        );
    }
}
