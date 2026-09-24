using DirectoryService.Core.Validation;
using DirectoryService.Shared.Errors;
using FluentValidation;

namespace DirectoryService.Core.Features.Departments.Queries.GetAncestorsById;

public class GetAncestorsByIdValidator : AbstractValidator<GetAncestorsByIdQuery>
{
    public GetAncestorsByIdValidator()
    {
        RuleFor(q => q.Page)
            .GreaterThan(0)
            .WithError(Error.Validation(new ErrorMessage(
                "departments.page.invalid",
                "The page parameter must be greater than 0.",
                nameof(GetAncestorsByIdQuery.Page)
            )));
        
        RuleFor(q => q.PageSize)
            .Must(x => x is >= 5 and <= 100)
            .WithError(Error.Validation(new ErrorMessage(
                "departments.page.size.invalid",
                "The page size must be from 5 to 100.",
                nameof(GetAncestorsByIdQuery.PageSize)
            )));
    }
}