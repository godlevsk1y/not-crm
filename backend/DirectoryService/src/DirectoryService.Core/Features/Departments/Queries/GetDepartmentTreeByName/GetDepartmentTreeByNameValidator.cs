using DirectoryService.Core.Features.Departments.Queries.GetDepartmentTree;
using DirectoryService.Core.Validation;
using DirectoryService.Shared.Errors;
using FluentValidation;

namespace DirectoryService.Core.Features.Departments.Queries.GetDepartmentTreeByName;

public class GetDepartmentTreeByNameValidator : AbstractValidator<GetDepartmentTreeByNameQuery>
{
    public GetDepartmentTreeByNameValidator()
    {
        RuleFor(q => q.Search)
            .MinimumLength(2)
            .WithError(Error.Validation(new ErrorMessage(
                "departments.search.query.invalid",
                "Search query must be at least 2 characters long.",
                nameof(GetDepartmentTreeByNameQuery.Search)
            )));
        
        RuleFor(q => q.Page)
            .GreaterThan(0)
            .WithError(Error.Validation(new ErrorMessage(
                "departments.page.invalid",
                "The page parameter must be greater than 0.",
                nameof(GetDepartmentTreeByNameQuery.Page)
            )));
        
        RuleFor(q => q.PageSize)
            .Must(x => x is >= 5 and <= 100)
            .WithError(Error.Validation(new ErrorMessage(
                "departments.page.size.invalid",
                "The page size must be from 5 to 100.",
                nameof(GetDepartmentTreeByNameQuery.PageSize)
            )));
    }
}