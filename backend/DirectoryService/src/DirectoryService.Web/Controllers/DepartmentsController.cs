using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Departments.QueryContracts;
using DirectoryService.Contracts.Positions.QueryContracts;
using DirectoryService.Contracts.WebApi.Departments;
using DirectoryService.Core.Features.Departments.Commands.AddLocation;
using DirectoryService.Core.Features.Departments.Commands.AddPosition;
using DirectoryService.Core.Features.Departments.Commands.CreateDepartment;
using DirectoryService.Core.Features.Departments.Commands.DeleteDepartment;
using DirectoryService.Core.Features.Departments.Commands.RemoveLocation;
using DirectoryService.Core.Features.Departments.Commands.RemovePosition;
using DirectoryService.Core.Features.Departments.Commands.TransferDepartment;
using DirectoryService.Core.Features.Departments.Commands.RestoreDepartment;
using DirectoryService.Core.Features.Departments.Commands.UpdateDepartment;
using DirectoryService.Core.Features.Departments.Queries.GetAncestorsById;
using DirectoryService.Core.Features.Departments.Queries.GetChildrenByParentId;
using DirectoryService.Core.Features.Departments.Queries.GetDepartmentById;
using DirectoryService.Core.Features.Departments.Queries.GetDepartmentList;
using DirectoryService.Core.Features.Departments.Queries.GetDepartmentPositions;
using DirectoryService.Core.Features.Departments.Queries.GetDepartmentTree;
using DirectoryService.Core.Features.Departments.Queries.GetDepartmentTreeByName;
using DirectoryService.Core.Features.Departments.Queries.GetDescendantsById;
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
public class DepartmentsController : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Create a department")]
    [EndpointDescription("Creates a root when parentId is null. locationIds is required; [] is allowed. Use distinct IDs of existing locations. Returns the department and a Location header.")]
    [ProducesResponseType<Envelope<DepartmentDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType<Envelope>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Envelope>(StatusCodes.Status409Conflict)]
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
    [EndpointSummary("Update a department")]
    [EndpointDescription("Null or omitted fields are unchanged. parentId = 00000000-0000-0000-0000-000000000000 makes a root. PATCH recalculates only this department path; use PUT /{id}/parent to transfer a subtree. Returns the ID.")]
    [ProducesResponseType<Envelope<Guid>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Envelope>(StatusCodes.Status404NotFound)]
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

    [HttpPost("{id:guid}/restore")]
    [EndpointSummary("Restore a department")]
    [EndpointDescription("No request body. Restores an archived record while preserving its data. An active record returns 204 without changes. Missing or physically deleted records return 404. Existing relationships are preserved; removed relationships are not recreated. Hierarchy is preserved; children are not restored. Restore an archived parent first (409 department.parent.archived).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Envelope>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Envelope>(StatusCodes.Status409Conflict)]
    public async Task<IResult> Restore(
        [FromServices] ICommandHandler<RestoreDepartmentCommand> handler,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var command = new RestoreDepartmentCommand(id);

        var restoreResult = await handler.Handle(command, cancellationToken);
        if (restoreResult.IsFailure)
        {
            return EndpointResults.Error(restoreResult.Error);
        }

        return EndpointResults.NoContent();
    }

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Delete a department")]
    [EndpointDescription("Soft-deletes a department. Active children prevent deletion (409 department.has.active.children).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Envelope>(StatusCodes.Status404NotFound)]
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
    [EndpointSummary("Link a location")]
    [EndpointDescription("No request body. Both resources must exist. A duplicate link returns 409 department.location.already.added.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Envelope>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Envelope>(StatusCodes.Status409Conflict)]
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
    [EndpointSummary("Link a position")]
    [EndpointDescription("No request body. Both resources must exist. A duplicate link returns 409 department.position.already.added.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Envelope>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Envelope>(StatusCodes.Status409Conflict)]
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
    [EndpointSummary("Unlink a location")]
    [EndpointDescription("No request body. Removes only the relationship. A missing relationship returns 404 department.location.not.found.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Envelope>(StatusCodes.Status404NotFound)]
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
    [EndpointSummary("Unlink a position")]
    [EndpointDescription("No request body. Removes only the relationship. A missing relationship returns 404 department.position.not.found.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<Envelope>(StatusCodes.Status404NotFound)]
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
    [EndpointSummary("Transfer a department subtree")]
    [EndpointDescription("newParentId = null makes a root. Recalculates paths and depths of descendants transactionally. An unchanged parent is a no-op. Self-parenting, cycles and concurrent transfer conflicts return 409; missing parents return 404.")]
    [ProducesResponseType<Envelope<TransferredDepartmentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Envelope>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<Envelope>(StatusCodes.Status409Conflict)]
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
    [EndpointSummary("Get a department by ID")]
    [EndpointDescription("Returns an active resource; deleted or missing resources return 404.")]
    [ProducesResponseType<Envelope<DepartmentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Envelope>(StatusCodes.Status404NotFound)]
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

    [HttpGet("{id:guid}/positions")]
    [EndpointSummary("List a department's positions")]
    [EndpointDescription("Returns distinct active positions linked directly to this department; positions of child departments are excluded. Missing or deleted departments return 404; an active department without positions returns an empty page. Sort by name (default) or createdAt, asc (default) or desc, then ID ascending. Page starts at 1; pageSize is 5–100, default 20. Empty pages retain totalCount.")]
    [ProducesResponseType<Envelope<PagedResult<PositionListItemDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Envelope>(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetDepartmentPositions(
        [FromServices] IQueryHandler<GetDepartmentPositionsQuery,
            Result<PagedResult<PositionListItemDto>, Error>> handler,
        [FromRoute] Guid id,
        [FromQuery] GetDepartmentPositionsRequest request,
        CancellationToken cancellationToken)
    {
        var query = new GetDepartmentPositionsQuery(
            id, request.SortBy, request.SortDirection, request.Page, request.PageSize);

        var result = await handler.Handle(query, cancellationToken);
        if (result.IsFailure)
        {
            return EndpointResults.Error(result.Error);
        }

        return EndpointResults.Ok(result.Value);
    }

    [HttpGet]
    [EndpointSummary("List departments")]
    [EndpointDescription("Flat active departments. Case-insensitive name search (PostgreSQL ILIKE; % and _ are wildcards). Sort by name or createdAt, then ID. Page starts at 1; pageSize is 5–100, default 20.")]
    [ProducesResponseType<Envelope<PagedResult<DepartmentListItemDto>>>(StatusCodes.Status200OK)]
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
    [EndpointSummary("List root departments")]
    [EndpointDescription("Returns only roots (depth 0), ordered by name, with childCount and hasChildren for active immediate children. Expand with /{id}/children. Page starts at 1; pageSize is 5–100, default 20.")]
    [ProducesResponseType<Envelope<PagedResult<DepartmentNodeDto>>>(StatusCodes.Status200OK)]
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
    [EndpointSummary("List immediate children")]
    [EndpointDescription("Returns active immediate children ordered by name, with counts of their immediate children. A missing parent returns an empty page. Page starts at 1; pageSize is 5–100, default 20.")]
    [ProducesResponseType<Envelope<PagedResult<DepartmentNodeDto>>>(StatusCodes.Status200OK)]
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

    [HttpGet("{id}/descendants")]
    [EndpointSummary("List all department descendants")]
    [EndpointDescription("Returns a flat list of all active and archived descendants at any depth, excluding this department, ordered by name/ID. No pagination or depth limit. An existing department with no descendants returns an empty list; a missing department returns 404. The source department may be active or archived.")]
    [ProducesResponseType<Envelope<IReadOnlyList<DepartmentDescendantDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<Envelope>(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetDescendantsById(
        [FromServices] IQueryHandler<GetDescendantsByIdQuery,
            Result<IReadOnlyList<DepartmentDescendantDto>, Error>> handler,
        [FromRoute] Guid id,
        CancellationToken cancellationToken
    )
    {
        var query = new GetDescendantsByIdQuery(id);

        var result = await handler.Handle(query, cancellationToken);
        if (result.IsFailure)
        {
            return EndpointResults.Error(result.Error);
        }

        return EndpointResults.Ok(result.Value);
    }

    [HttpGet("{id}/ancestors")]
    [EndpointSummary("List department ancestors")]
    [EndpointDescription("Returns active ancestors from root to parent, excluding this department. A root or missing department returns an empty page. Page starts at 1; pageSize is 5–100, default 20.")]
    [ProducesResponseType<Envelope<PagedResult<DepartmentAncestorDto>>>(StatusCodes.Status200OK)]
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
    [EndpointSummary("Search departments with ancestors")]
    [EndpointDescription("q is required, at least 2 characters. Case-insensitive ILIKE name search (% and _ are wildcards). Matches are paged and ordered by name/ID; each includes all active ancestors from root to parent. Page starts at 1; pageSize is 5–100, default 20.")]
    [ProducesResponseType<Envelope<PagedResult<DepartmentWithAncestorsDto>>>(StatusCodes.Status200OK)]
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
