using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Departments;
using DirectoryService.Core.Database;
using Shared.Kernel.Errors;
using Microsoft.EntityFrameworkCore;
using Shared.Core.Abstractions;

namespace DirectoryService.Core.Features.Departments.Queries.GetDepartmentById;

public class GetDepartmentByIdQueryHandler : IQueryHandler<GetDepartmentByIdQuery, Result<DepartmentDto, Error>>
{
    private readonly IReadDbContext _readDbContext;

    public GetDepartmentByIdQueryHandler(IReadDbContext readDbContext)
    {
        _readDbContext = readDbContext;
    }

    public async Task<Result<DepartmentDto, Error>> Handle(GetDepartmentByIdQuery query,
        CancellationToken cancellationToken)
    {
        var department = await _readDbContext.DepartmentsRead
            .FirstOrDefaultAsync(d => d.Id == query.Id, cancellationToken);

        if (department is null)
        {
            return DepartmentErrors.NotFound(query.Id);
        }
        
        return new DepartmentDto(
            Id: department.Id,
            Name: department.Name.ToString(),
            Slug: department.Slug.ToString(),
            Path: department.Path.ToString(),
            ParentId: department.ParentId?.ToGuid()
        );
    }
}
