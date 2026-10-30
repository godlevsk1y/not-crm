using DirectoryService.Core.Validation;
using FluentValidation;

namespace DirectoryService.Core.Features.Search.Queries.SearchPage;

public class SearchPageValidator : AbstractValidator<SearchPageQuery>
{
    public SearchPageValidator()
    {
        RuleFor(q => q.Q).Must(q => q?.Trim().Length >= 2).WithError(SearchErrors.QueryInvalid());
        RuleFor(q => q.Page).GreaterThan(0).WithError(SearchErrors.PageInvalid());
        RuleFor(q => q.PageSize).InclusiveBetween(1, 100).WithError(SearchErrors.PageSizeInvalid());
    }
}
