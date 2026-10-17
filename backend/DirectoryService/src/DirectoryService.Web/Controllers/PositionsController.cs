using DirectoryService.Contracts.Positions;
using DirectoryService.Core.Features.Positions.Commands.CreatePosition;
using DirectoryService.Core.Features.Positions.Commands.DeletePosition;
using DirectoryService.Core.Features.Positions.Commands.UpdatePosition;
using Microsoft.AspNetCore.Mvc;
using Shared.Core.Abstractions;
using Shared.Framework.Results;

namespace DirectoryService.Web.Controllers;

[ProducesResponseType<Envelope>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<Envelope>(StatusCodes.Status500InternalServerError)]
[ApiController]
[Route("api/[controller]")]
public class PositionsController : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Create a position")]
    [EndpointDescription("Creates a position. Name is trimmed; duplicate names are allowed. Location is /positions/{id}; there is currently no GET position endpoint.")]
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
