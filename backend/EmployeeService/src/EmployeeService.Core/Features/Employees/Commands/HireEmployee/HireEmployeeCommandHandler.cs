using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Communication;
using EmployeeService.Contracts.Employees;
using EmployeeService.Domain.Ids;
using EmployeeService.Domain.Models;
using EmployeeService.Domain.ValueObjects;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Core.Abstractions;
using Shared.Core.Extensions;
using Shared.Kernel.Errors;

namespace EmployeeService.Core.Features.Employees.Commands.HireEmployee;

public partial class HireEmployeeCommandHandler 
    : ICommandHandler<HireEmployeeCommand, EmployeeDto>
{
    private readonly IDirectoryServiceCommunication _directoryService;
    private readonly IValidator<HireEmployeeCommand> _validator;
    private readonly ILogger<HireEmployeeCommandHandler> _logger;

    public HireEmployeeCommandHandler( 
        IDirectoryServiceCommunication directoryService,
        IValidator<HireEmployeeCommand> validator, 
        ILogger<HireEmployeeCommandHandler> logger)
    {
        _validator = validator;
        _logger = logger;
        _directoryService = directoryService;
    }
    
    public async Task<Result<EmployeeDto, Error>> Handle(
        HireEmployeeCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        var positionTask = _directoryService.GetPositionByIdAsync(command.PositionId, cancellationToken);
        var departmentTask = _directoryService.GetDepartmentByIdAsync(command.DepartmentId, cancellationToken);

        await Task.WhenAll(positionTask, departmentTask);

        var positionResult = await positionTask;
        var departmentResult = await departmentTask;

        if (positionResult.IsFailure)
        {
            return positionResult.Error;
        }

        if (departmentResult.IsFailure)
        {
            return departmentResult.Error;
        }
        
        var employeeNameResult = EmployeeName.Create(command.FirstName, command.LastName);
        if (employeeNameResult.IsFailure)
        {
            return employeeNameResult.Error;
        }

        var employeeName = employeeNameResult.Value;

        var employeeResult = Employee.Create(
            employeeName, 
            new PositionId(command.PositionId), 
            new DepartmentId(command.DepartmentId), 
            command.HiredAt
        );

        if (employeeResult.IsFailure)
        {
            return employeeResult.Error;
        }
        
        var employee = employeeResult.Value;
        
        LogEmployeeHired(employee.Id);
        
        return new EmployeeDto
        {
            Id = employee.Id.Value,
            FirstName = employee.Name.FirstName,
            LastName = employee.Name.LastName,
            PositionId = employee.PositionId.Value,
            DepartmentId = employee.DepartmentId.Value,
            HiredAt = employee.HiredAt,
            DismissedAt = employee.DismissedAt,
            UpdatedAt = employee.UpdatedAt,
        };
    }

    [LoggerMessage(LogLevel.Information, "Employee {Id} hired")]
    partial void LogEmployeeHired(EmployeeId id);
}