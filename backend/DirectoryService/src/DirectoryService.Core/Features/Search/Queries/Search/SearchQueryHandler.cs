using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Search;
using FluentValidation;
using Shared.Core.Abstractions;
using Shared.Core.Extensions;
using Shared.Kernel.Errors;

namespace DirectoryService.Core.Features.Search.Queries.Search;

public class SearchQueryHandler(ISearchRepository repository, IValidator<SearchQuery> validator)
    : IQueryHandler<SearchQuery, Result<SearchResultDto, Error>>
{
    public async Task<Result<SearchResultDto, Error>> Handle(SearchQuery query, CancellationToken cancellationToken)
    {
        query = query with { Q = query.Q?.Trim() };
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToError();

        return await repository.SearchAsync(query.Q!, query.LimitPerType, cancellationToken);
    }
}
