using DirectoryService.Core.Features.Departments.Queries.GetDepartmentTree;
using DirectoryService.Core.Validation;
using DirectoryService.Shared.Errors;
using FluentValidation;

namespace DirectoryService.Core.Features.Departments.Queries.GetChildrenByParentId;

public class GetChildrenByParentIdValidator : AbstractValidator<GetChildrenByParentIdQuery>
{
    public GetChildrenByParentIdValidator()
    {
        RuleFor(q => q.Page)
            .GreaterThan(0)
            .WithError(Error.Validation(new ErrorMessage(
                "departments.page.invalid",
                "The page parameter must be greater than 0.",
                nameof(GetChildrenByParentIdQuery.Page)
            )));
        
        RuleFor(q => q.PageSize)
            .Must(x => x is >= 5 and <= 100)
            .WithError(Error.Validation(new ErrorMessage(
                "departments.page.size.invalid",
                "The page size must be from 5 to 100.",
                nameof(GetChildrenByParentIdQuery.PageSize)
            )));
    }
}