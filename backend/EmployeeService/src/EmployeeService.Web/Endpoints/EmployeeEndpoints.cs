using CSharpFunctionalExtensions;
using EmployeeService.Contracts.Employees;
using EmployeeService.Contracts.WebApi.Employees;
using EmployeeService.Core.Features.Employees.Commands.HireEmployee;
using EmployeeService.Core.Features.Employees.Queries.GetEmployeeById;
using Microsoft.AspNetCore.Mvc;
using Shared.Core.Abstractions;
using Shared.Framework.Results;
using Shared.Kernel.Errors;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace EmployeeService.Web.Endpoints;

public static class EmployeeEndpoints
{
    public static IEndpointRouteBuilder MapEmployeeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/{id:guid}", GetEmployeeById)
            .WithTags("Employees")
            .WithName("GetEmployeeById")
            .WithSummary("Get an employee by ID")
            .WithDescription("Returns an employee card with position and department UUIDs, without directory lookups. " +
                             "Dismissed employees remain available. Missing or soft-deleted employees return " +
                             "404 with employee.not.found; an empty UUID returns 400 with employee.id.empty. " +
                             "Malformed UUIDs do not match the route and return 404.")
            .Produces<Envelope<EmployeeDto>>()
            .Produces<Envelope>(StatusCodes.Status400BadRequest)
            .Produces<Envelope>(StatusCodes.Status404NotFound);

        endpoints.MapPost("/", HireEmployee)
            .WithTags("Employees")
            .WithName("HireEmployee")
            .WithSummary("Hire an employee")
            .WithDescription("Validates the employee's first and last names and checks that the position and " +
                             "department exist in DirectoryService. Returns the employee card with " +
                             "201 Created and a Location header. Names are trimmed, dismissedAt is null, " +
                             "and updatedAt is in UTC. Invalid names or empty position or department UUIDs " +
                             "return 400; a missing position or department returns 404. " +
                             "DirectoryService communication failures return 500.")
            .Produces<Envelope<EmployeeDto>>(StatusCodes.Status201Created)
            .Produces<Envelope>(StatusCodes.Status400BadRequest)
            .Produces<Envelope>(StatusCodes.Status404NotFound)
            .Produces<Envelope>(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<IResult> GetEmployeeById(
        [FromRoute] Guid id,
        [FromServices] IQueryHandler<GetEmployeeByIdQuery, Result<EmployeeDto, Error>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetEmployeeByIdQuery(id), cancellationToken);
        if (result.IsFailure)
        {
            return EndpointResults.Error(result.Error);
        }

        return EndpointResults.Ok(result.Value);
    }

    private static async Task<IResult> HireEmployee(
        [FromBody] HireEmployeeRequest request,
        [FromServices] ICommandHandler<HireEmployeeCommand, EmployeeDto> handler,
        CancellationToken cancellationToken)
    {
        var command = new HireEmployeeCommand
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            PositionId = request.PositionId,
            DepartmentId = request.DepartmentId,
            HiredAt = request.HiredAt
        };

        var result = await handler.Handle(command, cancellationToken);
        if (result.IsFailure)
        {
            return EndpointResults.Error(result.Error);
        }

        var employee = result.Value;
        
        return EndpointResults.Created($"{employee.Id}", employee);
    }
}
