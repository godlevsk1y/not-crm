using DirectoryService.Contracts.WebApi.Departments;
using DirectoryService.Core.Features.Departments;
using DirectoryService.Web.Results;
using Microsoft.AspNetCore.Mvc;

namespace DirectoryService.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentsService _departmentsService;

    public DepartmentsController(IDepartmentsService departmentsService)
    {
        _departmentsService = departmentsService;
    }
    
    [HttpPost]
    public async Task<IResult> Create([FromBody] CreateDepartmentRequest request, 
        CancellationToken cancellationToken)
    {
        var createResult = await _departmentsService.CreateAsync(request, cancellationToken);
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
        [FromRoute] Guid id,
        [FromBody] UpdateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        var updateResult = await _departmentsService.UpdateAsync(id, request, cancellationToken);
        if (updateResult.IsFailure)
        {
            return EndpointResults.Error(updateResult.Error);
        }
        
        return EndpointResults.Ok(updateResult.Value);
    }

    [HttpPost("{departmentId:guid}/locations/{locationId:guid}")]
    public async Task<IResult> AddLocationAsync(
        [FromRoute] Guid departmentId, 
        [FromRoute] Guid locationId,
        CancellationToken cancellationToken)
    {
        var addResult = await _departmentsService.AddLocationAsync(departmentId, locationId, cancellationToken);
        if (addResult.IsFailure)
        {
            return EndpointResults.Error(addResult.Error);
        }
        
        return EndpointResults.NoContent();
    }

    [HttpDelete("{departmentId:guid}/locations/{locationId:guid}")]
    public async Task<IResult> RemoveLocationAsync(
        [FromRoute] Guid departmentId,
        [FromRoute] Guid locationId,
        CancellationToken cancellationToken)
    {
        var removeResult = await _departmentsService.RemoveLocationAsync(departmentId, locationId, cancellationToken);
        if (removeResult.IsFailure)
        {
            return EndpointResults.Error(removeResult.Error);
        }
        
        return EndpointResults.NoContent();
    }
}