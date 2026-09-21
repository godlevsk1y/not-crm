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

namespace DirectoryService.Core.Features.Departments.Queries.GetChildrenByParentId;

public class GetChildrenByParentIdQueryHandler : IQueryHandler<
    GetChildrenByParentIdQuery, 
    Result<PagedResult<DepartmentTreeItemDto>, Error>>
{
    private readonly IDbConnectionFactory _factory;
    private readonly IValidator<GetChildrenByParentIdQuery> _validator;

    public GetChildrenByParentIdQueryHandler(
        IDbConnectionFactory factory, 
        IValidator<GetChildrenByParentIdQuery> validator)
    {
        _factory = factory;
        _validator = validator;
    }

    public async Task<Result<PagedResult<DepartmentTreeItemDto>, Error>> Handle(
        GetChildrenByParentIdQuery query, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }
        
        using var connection = await _factory.OpenConnectionAsync(cancellationToken);
        
        var parameters = new DynamicParameters();
        
        parameters.Add("parentId", query.ParentId, DbType.Guid);
        parameters.Add("page", query.Page, DbType.Int32);
        parameters.Add("pageSize", query.PageSize, DbType.Int32);
        
        const string sql = """
                           SELECT
                               d.id,
                               d.name,
                               d.slug,
                               d.path::text AS path,
                               d.depth,
                               c.child_count AS child_count,
                               c.child_count > 0 AS has_children,
                               count(d.id) OVER () AS total_count
                           FROM departments d
                            CROSS JOIN LATERAL (
                               SELECT count(*) AS child_count
                               FROM departments child
                               WHERE child.path <@ d.path
                                 AND child.depth = d.depth + 1
                                 AND child.deleted_at IS NULL
                               ) c
                           WHERE d.parent_id = @parentId
                             AND d.deleted_at IS NULL
                           ORDER BY d.name
                           OFFSET @pageSize * (@page - 1) LIMIT @pageSize
                           """;
        
        long? totalCount = null;
        
        var children = await connection
            .QueryAsync<DepartmentTreeItemDto, long, DepartmentTreeItemDto>(
                sql: sql,
                map: (dto, total) =>
                {
                    totalCount ??= total;

                    return dto;
                },
                param: parameters,
                splitOn: "total_count"
            );
        
        return new PagedResult<DepartmentTreeItemDto>(
            children, query.Page, query.PageSize, totalCount ?? 0
        );
    }
}