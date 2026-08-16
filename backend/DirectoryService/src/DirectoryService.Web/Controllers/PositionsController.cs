using DirectoryService.Contracts.WebApi.Positions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Features.Positions.CreatePosition;
using DirectoryService.Web.Results;
using Microsoft.AspNetCore.Mvc;

namespace DirectoryService.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PositionsController : ControllerBase
{
    [HttpPost]
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
}
