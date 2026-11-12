using Shared.Core.Abstractions;

namespace EmployeeService.Core.Features.Employees.Queries.GetEmployeeById;

public record GetEmployeeByIdQuery(Guid Id) : IQuery;
