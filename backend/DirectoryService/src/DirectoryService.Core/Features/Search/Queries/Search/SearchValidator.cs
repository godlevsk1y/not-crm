using DirectoryService.Core.Validation;
using FluentValidation;

namespace DirectoryService.Core.Features.Search.Queries.Search;

public class SearchValidator : AbstractValidator<SearchQuery>
{
    public SearchValidator()
    {
        RuleFor(q => q.Q).Must(q => q?.Trim().Length >= 2).WithError(SearchErrors.QueryInvalid());
        RuleFor(q => q.LimitPerType).InclusiveBetween(1, 100).WithError(SearchErrors.LimitInvalid());
    }
}
