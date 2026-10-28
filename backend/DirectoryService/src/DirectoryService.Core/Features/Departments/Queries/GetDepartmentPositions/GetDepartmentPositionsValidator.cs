using DirectoryService.Core.Validation;
using Shared.Kernel.Errors;
using FluentValidation;

namespace DirectoryService.Core.Features.Departments.Queries.GetDepartmentPositions;

public class GetDepartmentPositionsValidator : AbstractValidator<GetDepartmentPositionsQuery>
{
    public GetDepartmentPositionsValidator()
    {
        RuleFor(q => q.SortBy)
            .Must(x => x is "name" or "createdAt")
            .WithError(Error.Validation(new ErrorMessage(
                "positions.sort.by.invalid",
                "The sortBy parameter only allows 'name' or 'createdAt' values.",
                nameof(GetDepartmentPositionsQuery.SortBy)
            )));

        RuleFor(q => q.SortDirection)
            .Must(x => x is "asc" or "desc")
            .WithError(Error.Validation(new ErrorMessage(
                "positions.sort.direction.invalid",
                "The sortDirection parameter only allows 'asc' or 'desc' values.",
                nameof(GetDepartmentPositionsQuery.SortDirection)
            )));

        RuleFor(q => q.Page)
            .GreaterThan(0)
            .WithError(Error.Validation(new ErrorMessage(
                "positions.page.invalid",
                "The page parameter must be greater than 0.",
                nameof(GetDepartmentPositionsQuery.Page)
            )));

        RuleFor(q => q.PageSize)
            .Must(x => x is >= 5 and <= 100)
            .WithError(Error.Validation(new ErrorMessage(
                "positions.page.size.invalid",
                "The page size must be from 5 to 100.",
                nameof(GetDepartmentPositionsQuery.PageSize)
            )));
    }
}
