using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Search;
using DirectoryService.Core.Features.Search.Queries.Search;
using DirectoryService.Core.Features.Search.Queries.SearchPage;
using Microsoft.AspNetCore.Mvc;
using Shared.Core.Abstractions;
using Shared.Framework.Results;
using Shared.Kernel.Errors;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace DirectoryService.Web.Controllers;

[ApiController]
[Route("api/search")]
[ProducesResponseType<Envelope>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<Envelope>(StatusCodes.Status500InternalServerError)]
public class SearchController : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Search departments, locations and positions")]
    [EndpointDescription("Literal case-insensitive name substring search, including Cyrillic; q is trimmed and must contain at least 2 characters. %, _ and ! are literal characters. Active records only, ordered by name ASC, id ASC. Always returns all three blocks, each with items and hasMore; items contain only id and name. limitPerType (default 20, range 1–100) applies independently, without redistribution. No matches return 200 with empty items and hasMore=false. No total count. Continue a type at page=2 with the same q and pageSize=limitPerType; restart when q or the size changes.")]
    [ProducesResponseType<Envelope<SearchResultDto>>(StatusCodes.Status200OK)]
    public async Task<IResult> Search(
        [FromServices] IQueryHandler<SearchQuery, Result<SearchResultDto, Error>> handler,
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] int limitPerType = 20)
    {
        var result = await handler.Handle(new SearchQuery(q, limitPerType), cancellationToken);
        return result.IsFailure ? EndpointResults.Error(result.Error) : EndpointResults.Ok(result.Value);
    }

    [HttpGet("{entityType}")]
    [EndpointSummary("Search one directory type with independent pagination")]
    [EndpointDescription("Searches only departments, locations or positions; an unknown entityType returns 404. Uses the same trimmed, literal, case-insensitive substring search and active-only name ASC, id ASC order as combined search. page starts at 1 (default 1); pageSize is 1–100 (default 20). Empty, noninteger or out-of-range numeric inputs return 400. The first page equals the combined search block with limitPerType=pageSize. No matches or a page beyond the end returns 200 with empty items and hasMore=false. No total count. Uses LIMIT/OFFSET with no consistent snapshot across requests: concurrent additions, deletions or renames may shift pages. Keep q and pageSize unchanged while continuing; restart when either changes.")]
    [ProducesResponseType<Envelope<SearchPageDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Envelope>(StatusCodes.Status404NotFound)]
    public async Task<IResult> SearchPage(
        [FromServices] IQueryHandler<SearchPageQuery, Result<SearchPageDto, Error>> handler,
        [FromRoute] string entityType,
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await handler.Handle(new SearchPageQuery(entityType, q, page, pageSize), cancellationToken);
        return result.IsFailure ? EndpointResults.Error(result.Error) : EndpointResults.Ok(result.Value);
    }
}
