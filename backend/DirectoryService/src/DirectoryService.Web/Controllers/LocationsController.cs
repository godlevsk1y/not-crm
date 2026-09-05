using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Locations;
using DirectoryService.Contracts.Locations.QueryContracts;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Features.Locations.Commands.CreateLocation;
using DirectoryService.Core.Features.Locations.Commands.DeleteLocation;
using DirectoryService.Core.Features.Locations.Commands.UpdateLocation;
using DirectoryService.Core.Features.Locations.Queries.GetLocationById;
using DirectoryService.Core.Features.Locations.Queries.GetLocationsList;
using DirectoryService.Shared.Errors;
using DirectoryService.Shared.Results;
using DirectoryService.Web.Results;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

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
        [FromServices] IQueryHandler<GetLocationByIdQuery, 
            Result<LocationDto, Error>> handler,
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

    [HttpGet("top")]
    public async Task<IResult> GetTopLocationsWithDepartmentCount(
        [FromServices] IQueryHandler<IReadOnlyList<LocationWithDepartmentCountDto>> handler,
        CancellationToken cancellationToken)
    {
        var locations = await handler.Handle(cancellationToken);
        
        return EndpointResults.Ok(locations);
    }

    [HttpGet]
    public async Task<IResult> GetLocationList(
        [FromServices] IQueryHandler<GetLocationListQuery, 
            Result<PagedResult<LocationListItemDto>, Error>> handler,
        [FromQuery] GetLocationListQuery query,
        CancellationToken cancellationToken
    )
    {
        var getResult = await handler.Handle(query, cancellationToken);
        if (getResult.IsFailure)
        {
            return EndpointResults.Error(getResult.Error);
        }
        
        return EndpointResults.Ok(getResult.Value);
    }
}
