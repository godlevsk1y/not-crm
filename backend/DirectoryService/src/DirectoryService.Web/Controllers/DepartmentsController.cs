using DirectoryService.Contracts.Departments;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Features.Departments.Commands.AddLocation;
using DirectoryService.Core.Features.Departments.Commands.AddPosition;
using DirectoryService.Core.Features.Departments.Commands.CreateDepartment;
using DirectoryService.Core.Features.Departments.Commands.DeleteDepartment;
using DirectoryService.Core.Features.Departments.Commands.RemoveLocation;
using DirectoryService.Core.Features.Departments.Commands.RemovePosition;
using DirectoryService.Core.Features.Departments.Commands.UpdateDepartment;
using DirectoryService.Core.Features.Departments.Queries.GetDepartmentById;
using DirectoryService.Core.Features.Departments.Queries.GetDepartmentList;
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
    public async Task<IResult> AddLocation(
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
    public async Task<IResult> AddPosition(
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
    public async Task<IResult> RemoveLocation(
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
    public async Task<IResult> RemovePosition(
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

    [HttpGet("{id:guid}")]
    public async Task<IResult> GetById(
        [FromServices] GetDepartmentByIdQueryHandler handler,
        [FromRoute] Guid id,
        CancellationToken cancellationToken
    )
    {
        var query = new GetDepartmentByIdQuery(id);

        var departmentResult = await handler.Handle(query, cancellationToken);
        if (departmentResult.IsFailure)
        {
            return EndpointResults.Error(departmentResult.Error);
        }
        
        return EndpointResults.Ok(departmentResult.Value);
    }

    [HttpGet]
    public async Task<IResult> GetDepartmentList(
        [FromServices] GetDepartmentListQueryHandler handler,
        [FromQuery] GetDepartmentListQuery query,
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


