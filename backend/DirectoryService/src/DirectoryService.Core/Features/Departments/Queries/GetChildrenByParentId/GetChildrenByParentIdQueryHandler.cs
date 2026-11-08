using System.Data;
using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Contracts.Departments.QueryContracts;
using DirectoryService.Core.Database;
using DirectoryService.Core.Features.Departments.Caching;
using Shared.Kernel.Errors;
using Shared.Kernel.Results;
using FluentValidation;
using Shared.Core.Abstractions;
using Shared.Core.Extensions;

namespace DirectoryService.Core.Features.Departments.Queries.GetChildrenByParentId;

public class GetChildrenByParentIdQueryHandler : IQueryHandler<
    GetChildrenByParentIdQuery, 
    Result<PagedResult<DepartmentNodeDto>, Error>>
{
    private readonly IDbConnectionFactory _factory;
    private readonly IValidator<GetChildrenByParentIdQuery> _validator;
    private readonly DepartmentChildrenCache _cache;

    public GetChildrenByParentIdQueryHandler(
        IDbConnectionFactory factory, 
        IValidator<GetChildrenByParentIdQuery> validator,
        DepartmentChildrenCache cache)
    {
        _factory = factory;
        _validator = validator;
        _cache = cache;
    }

    public async Task<Result<PagedResult<DepartmentNodeDto>, Error>> Handle(
        GetChildrenByParentIdQuery query, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }
        
        return await _cache.GetOrCreateAsync(
            query, ct => ReadChildrenAsync(query, ct), cancellationToken);
    }

    private async ValueTask<PagedResult<DepartmentNodeDto>> ReadChildrenAsync(
        GetChildrenByParentIdQuery query, CancellationToken cancellationToken)
    {
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
            .QueryAsync<DepartmentNodeDto, long, DepartmentNodeDto>(
                command: new CommandDefinition(sql, parameters, cancellationToken: cancellationToken),
                map: (dto, total) =>
                {
                    totalCount ??= total;

                    return dto;
                },
                splitOn: "total_count"
            );
        
        if (totalCount is null && query.Page > 1)
        {
            const string countSql = """
                                    SELECT count(*)
                                    FROM departments d
                                    WHERE d.parent_id = @parentId
                                      AND d.deleted_at IS NULL
                                    """;

            totalCount = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
                countSql, parameters, cancellationToken: cancellationToken));
        }

        return new PagedResult<DepartmentNodeDto>(
            children, query.Page, query.PageSize, totalCount ?? 0
        );
    }
}
