using DirectoryService.Contracts.WebApi.Departments;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Features.Departments.Commands.AddLocation;
using DirectoryService.Core.Features.Departments.Commands.AddPosition;
using DirectoryService.Core.Features.Departments.Commands.CreateDepartment;
using DirectoryService.Core.Features.Departments.Commands.DeleteDepartment;
using DirectoryService.Core.Features.Departments.Commands.RemoveLocation;
using DirectoryService.Core.Features.Departments.Commands.RemovePosition;
using DirectoryService.Core.Features.Departments.Commands.UpdateDepartment;
using DirectoryService.Web.Results;
using Microsoft.AspNetCore.Mvc;

namespace DirectoryService.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DepartmentsController : ControllerBase
{
    [HttpPost]
    public async Task<IResult> Create(
        [FromServices] ICommandHandler<CreateDepartmentCommand, DepartmentDto> handler,
        [FromBody] CreateDepartmentRequest request, 
        CancellationToken cancellationToken)
    {
        var command = new CreateDepartmentCommand(request);
        
        var createResult = await handler.Handle(command, cancellationToken);
        if (createResult.IsFailure)
        {
            return EndpointResults.Error(createResult.Error);
        }
        
        return EndpointResults.Created(
            $"/api/departments/{createResult.Value.Id}",
            createResult.Value
        );
    }

    [HttpPatch("{id:guid}")]
    public async Task<IResult> Update(
        [FromServices] ICommandHandler<UpdateDepartmentCommand, Guid> handler,
        [FromRoute] Guid id,
        [FromBody] UpdateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateDepartmentCommand(id, request);
        
        var updateResult = await handler.Handle(command, cancellationToken);
        if (updateResult.IsFailure)
        {
            return EndpointResults.Error(updateResult.Error);
        }
        
        return EndpointResults.Ok(updateResult.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IResult> Delete(
        [FromServices] ICommandHandler<DeleteDepartmentCommand> handler,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteDepartmentCommand(id);

        var deleteResult = await handler.Handle(command, cancellationToken);
        if (deleteResult.IsFailure)
        {
            return EndpointResults.Error(deleteResult.Error);
        }

        return EndpointResults.NoContent();
    }

    [HttpPost("{departmentId:guid}/locations/{locationId:guid}")]
    public async Task<IResult> AddLocationAsync(
        [FromServices] ICommandHandler<AddLocationCommand> handler,
        [FromRoute] Guid departmentId, 
        [FromRoute] Guid locationId,
        CancellationToken cancellationToken)
    {
        var command = new AddLocationCommand(departmentId, locationId);
        
        var addResult = await handler.Handle(command, cancellationToken);
        if (addResult.IsFailure)
        {
            return EndpointResults.Error(addResult.Error);
        }
        
        return EndpointResults.NoContent();
    }

    [HttpPost("{departmentId:guid}/positions/{positionId:guid}")]
    public async Task<IResult> AddPositionAsync(
        [FromServices] ICommandHandler<AddPositionCommand> handler,
        [FromRoute] Guid departmentId,
        [FromRoute] Guid positionId,
        CancellationToken cancellationToken)
    {
        var command = new AddPositionCommand(departmentId, positionId);

        var addResult = await handler.Handle(command, cancellationToken);
        if (addResult.IsFailure)
        {
            return EndpointResults.Error(addResult.Error);
        }

        return EndpointResults.NoContent();
    }

    [HttpDelete("{departmentId:guid}/locations/{locationId:guid}")]
    public async Task<IResult> RemoveLocationAsync(
        [FromServices] ICommandHandler<RemoveLocationCommand> handler,
        [FromRoute] Guid departmentId,
        [FromRoute] Guid locationId,
        CancellationToken cancellationToken)
    {
        var command = new RemoveLocationCommand(departmentId, locationId);
        
        var removeResult = await handler.Handle(command, cancellationToken);
        if (removeResult.IsFailure)
        {
            return EndpointResults.Error(removeResult.Error);
        }
        
        return EndpointResults.NoContent();
    }

    [HttpDelete("{departmentId:guid}/positions/{positionId:guid}")]
    public async Task<IResult> RemovePositionAsync(
        [FromServices] ICommandHandler<RemovePositionCommand> handler,
        [FromRoute] Guid departmentId,
        [FromRoute] Guid positionId,
        CancellationToken cancellationToken)
    {
        var command = new RemovePositionCommand(departmentId, positionId);

        var removeResult = await handler.Handle(command, cancellationToken);
        if (removeResult.IsFailure)
        {
            return EndpointResults.Error(removeResult.Error);
        }

        return EndpointResults.NoContent();
    }
}
