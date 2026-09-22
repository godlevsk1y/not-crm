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

namespace DirectoryService.Core.Features.Departments.Queries.GetAncestorsById;

public class GetAncestorsByIdQueryHandler : IQueryHandler<
    GetAncestorsByIdQuery, Result<PagedResult<DepartmentAncestorDto>, Error>>
{
    private readonly IDbConnectionFactory _factory;
    private readonly IValidator<GetAncestorsByIdQuery> _validator;

    public GetAncestorsByIdQueryHandler(
        IDbConnectionFactory factory,
        IValidator<GetAncestorsByIdQuery> validator)
    {
        _factory = factory;
        _validator = validator;
    }
    
    public async Task<Result<PagedResult<DepartmentAncestorDto>, Error>> Handle(
        GetAncestorsByIdQuery query, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        using var connection = await _factory.OpenConnectionAsync(cancellationToken);
        
        var parameters = new DynamicParameters();
        
        parameters.Add("id", query.Id, DbType.Guid);
        parameters.Add("page", query.Page, DbType.Int32);
        parameters.Add("pageSize", query.PageSize, DbType.Int32);
        
        const string sql = """
                           SELECT
                               a.id,
                               a.name,
                               a.slug,
                               a.path::text AS path,
                               a.depth,
                               a.parent_id,
                               count(d.id) OVER () AS total_count
                           FROM departments d 
                           CROSS JOIN LATERAL (
                               SELECT
                                   ancestors.id,
                                   ancestors.name,
                                   ancestors.slug,
                                   ancestors.path::text AS path,
                                   ancestors.depth,
                                   ancestors.parent_id
                               FROM departments ancestors
                               WHERE ancestors.path @> d.path 
                                 AND ancestors.depth < d.depth
                                 AND ancestors.deleted_at IS NULL
                           ) a
                           WHERE d.id = @id
                             AND d.deleted_at IS NULL
                           ORDER BY a.depth
                           OFFSET @pageSize * (@page - 1) LIMIT @pageSize
                           """;

        long? totalCount = null;

        var ancestors = await connection
            .QueryAsync<DepartmentAncestorDto, long, DepartmentAncestorDto>(
                sql: sql,
                param: parameters,
                map: (dto, total) =>
                {
                    totalCount ??= total;

                    return dto;
                },
                splitOn: "total_count"
            );
        
        return new PagedResult<DepartmentAncestorDto>(
            ancestors, query.Page, query.PageSize, totalCount ?? 0
        );
    }
}