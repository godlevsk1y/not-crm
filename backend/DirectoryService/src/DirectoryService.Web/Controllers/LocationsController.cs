using DirectoryService.Contracts.Locations;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Features.Locations.Commands.CreateLocation;
using DirectoryService.Core.Features.Locations.Commands.DeleteLocation;
using DirectoryService.Core.Features.Locations.Commands.UpdateLocation;
using DirectoryService.Core.Features.Locations.Queries;
using DirectoryService.Core.Features.Locations.Queries.GetLocationById;
using DirectoryService.Web.Results;
using Microsoft.AspNetCore.Mvc;

namespace DirectoryService.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationsController : ControllerBase
{
    [HttpPost]
    public async Task<IResult> Create(
        [FromServices] ICommandHandler<CreateLocationCommand, LocationDto> handler,
        [FromBody] CreateLocationRequest request, 
        CancellationToken cancellationToken)
    {
        var command = new CreateLocationCommand(request);
        
        var createResult = await handler.Handle(command, cancellationToken);
        if (createResult.IsFailure)
        {
            return EndpointResults.Error(createResult.Error);
        }
        
        return EndpointResults.Created(
            $"/api/locations/{createResult.Value.Id}",
            createResult.Value
        );
    }

    [HttpPatch("{id:guid}")]
    public async Task<IResult> Update(
        [FromServices] ICommandHandler<UpdateLocationCommand, Guid> handler,
        [FromRoute] Guid id,
        [FromBody] UpdateLocationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateLocationCommand(id, request);
        
        var updateResult = await handler.Handle(command, cancellationToken);
        if (updateResult.IsFailure)
        {
            return EndpointResults.Error(updateResult.Error);
        }
        
        return EndpointResults.Ok(updateResult.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IResult> Delete(
        [FromServices] ICommandHandler<DeleteLocationCommand> handler,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteLocationCommand(id);

        var deleteResult = await handler.Handle(command, cancellationToken);
        if (deleteResult.IsFailure)
        {
            return EndpointResults.Error(deleteResult.Error);
        }

        return EndpointResults.NoContent();
    }

    [HttpGet("{id:guid}")]
    public async Task<IResult> GetById(
        [FromServices] GetLocationByIdQueryHandler handler,
        [FromRoute] Guid id,
        CancellationToken cancellationToken
    )
    {
        var query = new GetLocationByIdQuery(id);

        var locationResult = await handler.Handle(query, cancellationToken);
        if (locationResult.IsFailure)
        {
            return EndpointResults.Error(locationResult.Error);
        }
        
        return EndpointResults.Ok(locationResult.Value);
    }
}
