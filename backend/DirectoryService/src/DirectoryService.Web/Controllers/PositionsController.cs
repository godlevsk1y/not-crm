using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Positions;
using DirectoryService.Contracts.Positions.QueryContracts;
using DirectoryService.Contracts.WebApi.Positions;
using DirectoryService.Core.Features.Positions.Commands.CreatePosition;
using DirectoryService.Core.Features.Positions.Commands.DeletePosition;
using DirectoryService.Core.Features.Positions.Commands.UpdatePosition;
using DirectoryService.Core.Features.Positions.Queries.GetPositionList;
using Microsoft.AspNetCore.Mvc;
using Shared.Core.Abstractions;
using Shared.Framework.Results;
using Shared.Kernel.Errors;
using Shared.Kernel.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace DirectoryService.Web.Controllers;

[ProducesResponseType<Envelope>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<Envelope>(StatusCodes.Status500InternalServerError)]
[ApiController]
[Route("api/[controller]")]
public class PositionsController : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List positions")]
    [EndpointDescription("Positions with id, name and createdAt. isActive defaults to true (active only); false returns inactive, including soft-deleted and archived positions. Empty or null isActive values are invalid. Optional case-insensitive name search, at most 100 characters (PostgreSQL ILIKE; % and _ are wildcards). Sort by name or createdAt (default name), asc or desc (default asc), then ID ascending. Page starts at 1 (default 1); pageSize is 5–100 (default 20). totalCount is the matching count before pagination, including empty pages. No matches return 200 with an empty list.")]
    [ProducesResponseType<Envelope<PagedResult<PositionListItemDto>>>(StatusCodes.Status200OK)]
    public async Task<IResult> GetPositionList(
        [FromServices] IQueryHandler<GetPositionListQuery,
            Result<PagedResult<PositionListItemDto>, Error>> handler,
        [FromQuery] GetPositionListRequest request,
        CancellationToken cancellationToken)
    {
        var query = new GetPositionListQuery(
            request.Search,
            request.SortBy,
            request.SortDirection,
            request.Page,
            request.PageSize,
            request.IsActive);

        var getResult = await handler.Handle(query, cancellationToken);
        if (getResult.IsFailure)
        {
            return EndpointResults.Error(getResult.Error);
        }

        return EndpointResults.Ok(getResult.Value);
    }

    [HttpPost]
    [EndpointSummary("Create a position")]
    [EndpointDescription("Creates a position. Name is trimmed; duplicate names are allowed. Location is /positions/{id}; there is currently no GET position-by-ID endpoint.")]
    [ProducesResponseType<Envelope<PositionDto>>(StatusCodes.Status201Created)]
    public async Task<IResult> Create(
        [FromServices] ICommandHandler<CreatePositionCommand, PositionDto> handler,
        [FromBody] CreatePositionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePositionCommand(request);

        var createResult = await handler.Handle(command, cancellationToken);
        if (createResult.IsFailure)
        {
            return EndpointResults.Error(createResult.Error);
        }

        return EndpointResults.Created(
            $"/positions/{createResult.Value.Id}",
            createResult.Value
        );
    }

    [HttpPatch("{id:guid}")]
    [EndpointSummary("Update a position")]
    [EndpointDescription("Name is required and replaces the existing name. Returns the ID.")]
    [ProducesResponseType<Envelope<Guid>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Envelope>(StatusCodes.Status404NotFound)]
    public async Task<IResult> Update(
        [FromServices] ICommandHandler<UpdatePositionCommand, Guid> handler,
        [FromRoute] Guid id,
        [FromBody] UpdatePositionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePositionCommand(id, request);

        var updateResult = await handler.Handle(command, cancellationToken);
        if (updateResult.IsFailure)
        {
            return EndpointResults.Error(updateResult.Error);
        }

        return EndpointResults.Ok(updateResult.Value);
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Delete a position")]
    [EndpointDescription("Removes all department links and soft-deletes the position in a transaction.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Envelope>(StatusCodes.Status404NotFound)]
    public async Task<IResult> Delete(
        [FromServices] ICommandHandler<DeletePositionCommand> handler,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeletePositionCommand(id);

        var deleteResult = await handler.Handle(command, cancellationToken);
        if (deleteResult.IsFailure)
        {
            return EndpointResults.Error(deleteResult.Error);
        }

        return EndpointResults.NoContent();
    }
}
