using DirectoryService.Contracts.WebApi.Locations;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Features.Locations;
using DirectoryService.Core.Features.Locations.CreateLocation;
using DirectoryService.Core.Features.Locations.UpdateLocation;
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
}