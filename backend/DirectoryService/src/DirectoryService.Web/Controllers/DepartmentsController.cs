using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Departments.QueryContracts;
using DirectoryService.Contracts.WebApi.Departments;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Features.Departments.Commands.AddLocation;
using DirectoryService.Core.Features.Departments.Commands.AddPosition;
using DirectoryService.Core.Features.Departments.Commands.CreateDepartment;
using DirectoryService.Core.Features.Departments.Commands.DeleteDepartment;
using DirectoryService.Core.Features.Departments.Commands.RemoveLocation;
using DirectoryService.Core.Features.Departments.Commands.RemovePosition;
using DirectoryService.Core.Features.Departments.Commands.TransferDepartment;
using DirectoryService.Core.Features.Departments.Commands.UpdateDepartment;
using DirectoryService.Core.Features.Departments.Queries.GetAncestorsById;
using DirectoryService.Core.Features.Departments.Queries.GetChildrenByParentId;
using DirectoryService.Core.Features.Departments.Queries.GetDepartmentById;
using DirectoryService.Core.Features.Departments.Queries.GetDepartmentList;
using DirectoryService.Core.Features.Departments.Queries.GetDepartmentTree;
using DirectoryService.Core.Features.Departments.Queries.GetDepartmentTreeByName;
using DirectoryService.Shared.Errors;
using DirectoryService.Shared.Results;
using DirectoryService.Web.Results;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

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

    [HttpPut("{id:guid}/parent")]
    public async Task<IResult> TransferDepartment(
        [FromServices] ICommandHandler<TransferDepartmentCommand, TransferredDepartmentDto> handler,
        [FromRoute] Guid id,
        [FromBody] TransferDepartmentRequest request,
        CancellationToken cancellationToken
    )
    {
        var command = new TransferDepartmentCommand(id, request.NewParentId);

        var result = await handler.Handle(command, cancellationToken);
        if (result.IsFailure)
        {
            return EndpointResults.Error(result.Error);
        }
        
        return EndpointResults.Ok(result.Value);
    }
    
    [HttpGet("{id:guid}")]
    public async Task<IResult> GetById(
        [FromServices] IQueryHandler<GetDepartmentByIdQuery, Result<DepartmentDto, Error>> handler,
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
        [FromServices] IQueryHandler<GetDepartmentListQuery,
            Result<PagedResult<DepartmentListItemDto>, Error>> handler,
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

    [HttpGet("tree")]
    public async Task<IResult> GetDepartmentTree(
        [FromServices] IQueryHandler<GetDepartmentTreeQuery, 
            Result<PagedResult<DepartmentNodeDto>, Error>> handler,
        [FromQuery] GetDepartmentTreeRequest request,
        CancellationToken cancellationToken
    )
    {
        var query = new GetDepartmentTreeQuery(request.Page, request.PageSize);

        var result = await handler.Handle(query, cancellationToken);
        if (result.IsFailure)
        {
            return EndpointResults.Error(result.Error);
        }
        
        return EndpointResults.Ok(result.Value);
    }

    [HttpGet("{id}/children")]
    public async Task<IResult> GetChildrenByParentId(
        [FromServices] IQueryHandler<GetChildrenByParentIdQuery, 
            Result<PagedResult<DepartmentNodeDto>, Error>> handler,
        [FromRoute] Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default
    )
    {
        var query = new GetChildrenByParentIdQuery(
            id, page, pageSize
        );

        var result = await handler.Handle(query, cancellationToken);
        if (result.IsFailure)
        {
            return EndpointResults.Error(result.Error);
        }
        
        return EndpointResults.Ok(result.Value);
    }

    [HttpGet("{id}/ancestors")]
    public async Task<IResult> GetAncestorsById(
        [FromServices] IQueryHandler<GetAncestorsByIdQuery,
            Result<PagedResult<DepartmentAncestorDto>, Error>> handler,
        [FromRoute] Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default
    )
    {
        var query = new GetAncestorsByIdQuery(id, page, pageSize);
        
        var result = await handler.Handle(query, cancellationToken);
        if (result.IsFailure)
        {
            return EndpointResults.Error(result.Error);
        }
        
        return EndpointResults.Ok(result.Value);
    }

    [HttpGet("tree/search")]
    public async Task<IResult> GetDepartmentTreeByName(
        IQueryHandler<GetDepartmentTreeByNameQuery, 
            Result<PagedResult<DepartmentWithAncestorsDto>, Error>> handler,
        [FromQuery] GetDepartmentTreeByNameRequest request,
        CancellationToken cancellationToken
    )
    {
        var query = new GetDepartmentTreeByNameQuery(
            request.Q, request.Page, request.PageSize
        );

        var result = await handler.Handle(query, cancellationToken);
        if (result.IsFailure)
        {
            return EndpointResults.Error(result.Error);
        }
        
        return EndpointResults.Ok(result.Value);
    }
}
