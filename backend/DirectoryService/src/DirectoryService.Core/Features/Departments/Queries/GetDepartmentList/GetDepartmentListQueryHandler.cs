using System.Diagnostics;
using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Departments.QueryContracts;
using DirectoryService.Core.Database;
using DirectoryService.Core.Extensions;
using DirectoryService.Shared.Errors;
using DirectoryService.Shared.Results;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.Core.Features.Departments.Queries.GetDepartmentList;

public class GetDepartmentListQueryHandler
{
    private readonly IReadDbContext _readContext;
    private readonly IValidator<GetDepartmentListQuery> _validator;

    public GetDepartmentListQueryHandler(
        IReadDbContext readContext, 
        IValidator<GetDepartmentListQuery> validator)
    {
        _readContext = readContext;
        _validator = validator;
    }

    public async Task<Result<PagedResult<DepartmentListItemDto>, Error>> Handle(
        GetDepartmentListQuery request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }
        
        var query = _readContext.DepartmentsRead;
        
        if (request.Search is not null)
            query = query.Where(d => EF.Functions.Like(d.Name.Value, $"%{request.Search}%"));

        query = (request.SortBy, request.SortDirection) switch
        {
            ("name", "asc") => 
                query.OrderBy(d => d.Name.Value)
                    .ThenBy(d => d.Id),
                
            ("name", "desc") => 
                query.OrderByDescending(d => d.Name.Value)
                    .ThenBy(d => d.Id),
            
            ("createdAt", "asc") => 
                query.OrderBy(d => d.CreatedAt)
                    .ThenBy(d => d.Id),
            
            ("createdAt", "desc") => 
                query.OrderByDescending(d => d.CreatedAt)
                    .ThenBy(d => d.Id),
                
            _ => throw new UnreachableException(),
        };

        var totalCount = await query.CountAsync(cancellationToken);
        
        query = query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize);
        
        var departmentListItems = await query.Select(d => new DepartmentListItemDto(
            d.Id,
            d.Name.Value,
            d.Slug.Value,
            d.Path.Value,
            d.CreatedAt
        )).ToListAsync(cancellationToken);
        
        return new PagedResult<DepartmentListItemDto>(
            departmentListItems,
            request.Page,
            request.PageSize,
            totalCount
        );
    }
}