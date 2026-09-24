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

namespace DirectoryService.Core.Features.Departments.Queries.GetDepartmentTreeByName;

public class GetDepartmentTreeByNameQueryHandler : IQueryHandler<
    GetDepartmentTreeByNameQuery, Result<PagedResult<DepartmentWithAncestorsDto>, Error>>
{
    private readonly IDbConnectionFactory _factory;
    private readonly IValidator<GetDepartmentTreeByNameQuery> _validator;

    public GetDepartmentTreeByNameQueryHandler(
        IDbConnectionFactory factory,
        IValidator<GetDepartmentTreeByNameQuery> validator)
    {
        _factory = factory;
        _validator = validator;
    }
    
    public async Task<Result<PagedResult<DepartmentWithAncestorsDto>, Error>> Handle(
        GetDepartmentTreeByNameQuery query, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        using var connection = await _factory.OpenConnectionAsync(cancellationToken);

        var parameters = new DynamicParameters();
        
        parameters.Add("search", query.Search, DbType.String);
        parameters.Add("page", query.Page, DbType.Int32);
        parameters.Add("pageSize", query.PageSize, DbType.Int32);
        
        const string sql = """
                           WITH filtered_departments AS (
                               SELECT id,
                                      name,
                                      slug,
                                      path,
                                      depth,
                                      parent_id,
                                      count(id) OVER () AS total_count
                               FROM departments
                               WHERE name ILIKE '%' || @search ||'%' 
                                 AND deleted_at IS NULL
                               ORDER BY name, id
                               LIMIT @pageSize
                               OFFSET @pageSize * (@page - 1) 
                           )
                           SELECT
                               fd.id,
                               fd.name,
                               fd.slug,
                               fd.path::text AS path,
                               fd.depth,
                               fd.parent_id,
                               fd.total_count,
                               a.id AS ancestor_id,
                               a.name AS ancestor_name,
                               a.slug AS ancestor_slug,
                               a.path AS ancestor_path,
                               a.depth AS ancestor_depth,
                               a.parent_id AS ancestor_parent_id
                           FROM filtered_departments fd
                           LEFT JOIN LATERAL (
                               SELECT 
                                   ancestors.id,
                                   ancestors.name,
                                   ancestors.slug,
                                   ancestors.path,
                                   ancestors.depth,
                                   ancestors.parent_id
                               FROM departments ancestors
                               WHERE ancestors.path @> fd.path
                                 AND ancestors.depth != fd.depth 
                                 AND ancestors.deleted_at IS NULL
                           ) a ON TRUE
                           ORDER BY fd.name, a.depth;
                           """;

        var rows = await connection.QueryAsync<DepartmentWithAncestorsRow>(sql, param: parameters);

        long? totalCount = null;
        
        var departmentsDictionary = new Dictionary<Guid, DepartmentWithAncestorsDto>();
        foreach (var row in rows)
        {
            totalCount ??= row.TotalCount;
            
            if (!departmentsDictionary.TryGetValue(row.Id, out var department))
            {
                department = new DepartmentWithAncestorsDto
                {
                    Id = row.Id,
                    Name = row.Name,
                    Slug = row.Slug,
                    Path = row.Path,
                    Depth = row.Depth,
                    ParentId = row.ParentId,
                    Ancestors = [],
                };
                
                departmentsDictionary.Add(row.Id, department);
            }

            if (row.AncestorId is not null)
            {
                department.Ancestors.Add(new DepartmentAncestorDto(
                    row.AncestorId.Value, 
                    row.AncestorName!, 
                    row.AncestorSlug!, 
                    row.AncestorPath!, 
                    row.AncestorDepth!.Value,
                    row.AncestorParentId)
                );
            }
        }
        
        return new PagedResult<DepartmentWithAncestorsDto>(
            departmentsDictionary.Values, query.Page, query.PageSize, totalCount ?? 0
        );
    }
}

file sealed record DepartmentWithAncestorsRow
{
    public Guid Id { get; init; }
    
    public string Name { get; init; } = null!;
    
    public string Slug { get; init; } = null!;
    
    public string Path { get; init; } = null!;
    
    public int Depth { get; init; }
    
    public Guid? ParentId { get; init; }
    
    public long TotalCount { get; init; }
    
    public Guid? AncestorId { get; init; }
    
    public string? AncestorName { get; init; }
    
    public string? AncestorSlug { get; init; }
    
    public string? AncestorPath { get; init; }
    
    public int? AncestorDepth { get; init; }
    
    public Guid? AncestorParentId { get; init; }
}