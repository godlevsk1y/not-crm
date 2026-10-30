using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Search;
using FluentValidation;
using Shared.Core.Abstractions;
using Shared.Core.Extensions;
using Shared.Kernel.Errors;

namespace DirectoryService.Core.Features.Search.Queries.SearchPage;

public class SearchPageQueryHandler(ISearchRepository repository, IValidator<SearchPageQuery> validator)
    : IQueryHandler<SearchPageQuery, Result<SearchPageDto, Error>>
{
    public async Task<Result<SearchPageDto, Error>> Handle(SearchPageQuery query, CancellationToken cancellationToken)
    {
        if (query.EntityType is not ("departments" or "locations" or "positions"))
            return SearchErrors.EntityTypeNotFound();

        query = query with { Q = query.Q?.Trim() };
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
            return validationResult.ToError();

        return await repository.SearchPageAsync(query.EntityType, query.Q!, query.Page, query.PageSize, cancellationToken);
    }
}
