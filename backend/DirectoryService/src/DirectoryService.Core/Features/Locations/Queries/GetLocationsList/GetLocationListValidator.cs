using DirectoryService.Core.Features.Departments.Queries.GetDepartmentList;
using DirectoryService.Core.Validation;
using DirectoryService.Shared.Errors;
using FluentValidation;

namespace DirectoryService.Core.Features.Locations.Queries.GetLocationsList;

public class GetLocationListValidator : AbstractValidator<GetLocationListQuery>
{
    public GetLocationListValidator()
    {
        RuleFor(q => q.Search)
            .MaximumLength(100)
            .WithError(Error.Validation(new ErrorMessage(
                "location.search.too.long",
                "The search string must not exceed 100 characters.",
                nameof(GetLocationListQuery.Search)
            )))
            .When(q => q.Search is not null);

        RuleFor(q => q.MinDepartmentCount)
            .Must(x => x is >= 0)
            .WithError(Error.Validation(new ErrorMessage(
                "location.department.count.invalid",
                "The minimum number of departments must be greater than or equal to zero.",
                nameof(GetLocationListQuery.MinDepartmentCount)
            )))
            .When(q => q.MinDepartmentCount is not null);
        
        RuleFor(q => q.SortBy)
            .Must(x => x is "name" or "createdAt" or "departmentCount")
            .WithError(Error.Validation(new ErrorMessage(
                "locations.sort.by.invalid",
                "The sortBy parameter only allows 'name', 'createdAt' or 'departmentCount' values.",
                nameof(GetLocationListQuery.SortBy)
            )));

        RuleFor(q => q.SortDirection)
            .Must(x => x is "asc" or "desc")
            .WithError(Error.Validation(new ErrorMessage(
                "locations.sort.direction.invalid",
                "The sortBy parameter only allows 'asc' or 'desc' values.",
                nameof(GetLocationListQuery.SortDirection)
            )));
        
        RuleFor(q => q.Page)
            .GreaterThan(0)
            .WithError(Error.Validation(new ErrorMessage(
                "locations.page.invalid",
                "The page parameter must be greater than 0.",
                nameof(GetLocationListQuery.Page)
            )));
        
        RuleFor(q => q.PageSize)
            .Must(x => x is >= 5 and <= 100)
            .WithError(Error.Validation(new ErrorMessage(
                "locations.page.size.invalid",
                "The page size must be from 5 to 100.",
                nameof(GetLocationListQuery.PageSize)
            )));
    }
}