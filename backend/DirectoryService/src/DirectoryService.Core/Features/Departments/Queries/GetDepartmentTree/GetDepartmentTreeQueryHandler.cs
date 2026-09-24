using System.Data;
using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Contracts.Departments.QueryContracts;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Core.Extensions;
using DirectoryService.Shared.Errors;
using DirectoryService.Shared.Results;
using FluentValidation;

namespace DirectoryService.Core.Features.Departments.Queries.GetDepartmentTree;

public class GetDepartmentTreeQueryHandler : IQueryHandler<
    GetDepartmentTreeQuery,
    Result<PagedResult<DepartmentNodeDto>, Error>>
{
    private readonly IDbConnectionFactory _factory;
    private readonly IValidator<GetDepartmentTreeQuery> _validator;

    public GetDepartmentTreeQueryHandler(
        IDbConnectionFactory factory, 
        IValidator<GetDepartmentTreeQuery> validator)
    {
        _factory = factory;
        _validator = validator;
    }

    public async Task<Result<PagedResult<DepartmentNodeDto>, Error>> Handle(GetDepartmentTreeQuery query, 
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }
        
        using var connection = await _factory.OpenConnectionAsync(cancellationToken);

        var parameters = new DynamicParameters();
        
        parameters.Add("page", query.Page, DbType.Int32);
        parameters.Add("pageSize", query.PageSize, DbType.Int32);
        
        const string sql = """
                           SELECT 
                               r.id,
                               r.name,
                               r.slug,
                               r.path::text AS path,
                               r.depth,
                               c.child_count AS child_count,
                               c.child_count > 0 AS has_children,
                               count(r.id) OVER () AS total_count
                           FROM departments r
                           CROSS JOIN LATERAL (
                               SELECT count(*) AS child_count
                               FROM departments child
                               WHERE child.path <@ r.path 
                                 AND child.depth = r.depth + 1 
                                 AND child.deleted_at IS NULL
                           ) c
                           WHERE r.depth = 0
                             AND r.deleted_at IS NULL
                           ORDER BY r.name
                           OFFSET @pageSize * (@page - 1) LIMIT @pageSize
                           """;

        long? totalCount = null;
        
        var rootDepartments = await connection
            .QueryAsync<DepartmentNodeDto, long, DepartmentNodeDto>(
                sql: sql,
                map: (dto, total) =>
                {
                    totalCount ??= total;

                    return dto;
                },
                param: parameters,
                splitOn: "total_count"
            );

        return new PagedResult<DepartmentNodeDto>(
            rootDepartments, query.Page, query.PageSize, totalCount ?? 0
        );
    }
}