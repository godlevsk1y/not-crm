using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Locations;
using DirectoryService.Contracts.Locations.QueryContracts;
using DirectoryService.Core.Features.Locations.Commands.CreateLocation;
using DirectoryService.Core.Features.Locations.Commands.DeleteLocation;
using DirectoryService.Core.Features.Locations.Commands.UpdateLocation;
using DirectoryService.Core.Features.Locations.Queries.GetLocationById;
using DirectoryService.Core.Features.Locations.Queries.GetLocationsList;
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
public class LocationsController : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Create a location")]
    [EndpointDescription("Creates a location with a flat address request. Name is trimmed and must be unique. Returns the location with a nested address and a Location header.")]
    [ProducesResponseType<Envelope<LocationDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType<Envelope>(StatusCodes.Status409Conflict)]
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
    [EndpointSummary("Update a location")]
    [EndpointDescription("Null or omitted fields are unchanged. Empty region, district and postalCode also preserve the existing value. Supplied required address components cannot be whitespace. Returns the ID.")]
    [ProducesResponseType<Envelope<Guid>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Envelope>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Envelope>(StatusCodes.Status409Conflict)]
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
    [EndpointSummary("Delete a location")]
    [EndpointDescription("Removes all department links and soft-deletes the location in a transaction.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Envelope>(StatusCodes.Status404NotFound)]
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
    [EndpointSummary("Get a location by ID")]
    [EndpointDescription("Returns an active resource; deleted or missing resources return 404.")]
    [ProducesResponseType<Envelope<LocationDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Envelope>(StatusCodes.Status404NotFound)]
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
    [EndpointSummary("Get the top five locations")]
    [EndpointDescription("Up to five active locations ordered by departmentCount descending, then name ascending. Includes locations with zero links; no query parameters.")]
    [ProducesResponseType<Envelope<IReadOnlyList<LocationWithDepartmentCountDto>>>(StatusCodes.Status200OK)]
    public async Task<IResult> GetTopLocationsWithDepartmentCount(
        [FromServices] IQueryHandler<IReadOnlyList<LocationWithDepartmentCountDto>> handler,
        CancellationToken cancellationToken)
    {
        var locations = await handler.Handle(cancellationToken);
        
        return EndpointResults.Ok(locations);
    }

    [HttpGet]
    [EndpointSummary("List locations")]
    [EndpointDescription("Locations with nested addresses and department link counts. isActive defaults to true (active only); false returns inactive, including soft-deleted and archived locations. Empty or null isActive values are invalid. Case-insensitive ILIKE name search (% and _ are wildcards); optional minDepartmentCount >= 0. Optional comma-separated departmentIds selects locations linked to any selected department, without duplicates; omitted or empty means no department filter. Filters combine before pagination; totalCount counts all unique matches. Sort by name, createdAt or departmentCount, then ID. Page starts at 1; pageSize is 5–100, default 20.")]
    [ProducesResponseType<Envelope<PagedResult<LocationListItemDto>>>(StatusCodes.Status200OK)]
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
