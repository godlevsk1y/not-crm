using DirectoryService.Core.Validation;
using DirectoryService.Shared.Errors;
using FluentValidation;

namespace DirectoryService.Core.Features.Departments.Queries.GetDepartmentList;

public class GetDepartmentListValidator : AbstractValidator<GetDepartmentListQuery>
{
    public GetDepartmentListValidator()
    {
        RuleFor(q => q.Search)
            .MaximumLength(100)
            .WithError(Error.Validation(new ErrorMessage(
                "departments.search.too.long",
                "The search string must not exceed 100 characters.",
                nameof(GetDepartmentListQuery.Search)
            )))
            .When(q => q.Search is not null);

        RuleFor(q => q.SortBy)
            .Must(x => x is "name" or "createdAt")
            .WithError(Error.Validation(new ErrorMessage(
                "departments.sort.by.invalid",
                "The sortBy parameter only allows 'name' or 'createdAt' values.",
                nameof(GetDepartmentListQuery.SortBy)
            )));

        RuleFor(q => q.SortDirection)
            .Must(x => x is "asc" or "desc")
            .WithError(Error.Validation(new ErrorMessage(
                "departments.sort.direction.invalid",
                "The sortBy parameter only allows 'asc' or 'desc' values.",
                nameof(GetDepartmentListQuery.SortDirection)
            )));

        RuleFor(q => q.Page)
            .GreaterThan(0)
            .WithError(Error.Validation(new ErrorMessage(
                "departments.page.invalid",
                "The page parameter must be greater than 0.",
                nameof(GetDepartmentListQuery.Page)
            )));
        
        RuleFor(q => q.PageSize)
            .Must(x => x is >= 5 and <= 100)
            .WithError(Error.Validation(new ErrorMessage(
                "departments.page.size.invalid",
                "The page size must be from 5 to 100.",
                nameof(GetDepartmentListQuery.PageSize)
            )));
    }
}