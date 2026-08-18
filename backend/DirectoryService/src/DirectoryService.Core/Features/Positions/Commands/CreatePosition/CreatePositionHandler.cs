using CSharpFunctionalExtensions;
using DirectoryService.Contracts.WebApi.Positions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Core.Extensions;
using DirectoryService.Domain.Models;
using DirectoryService.Domain.ValueObjects;
using DirectoryService.Shared.Errors;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Features.Positions.Commands.CreatePosition;

public partial class CreatePositionHandler : ICommandHandler<CreatePositionCommand, PositionDto>
{
    private readonly IPositionsRepository _positionsRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<CreatePositionRequest> _validator;
    private readonly ILogger<CreatePositionHandler> _logger;

    public CreatePositionHandler(
        IPositionsRepository positionsRepository,
        ITransactionManager transactionManager,
        IValidator<CreatePositionRequest> validator,
        ILogger<CreatePositionHandler> logger)
    {
        _positionsRepository = positionsRepository;
        _transactionManager = transactionManager;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<PositionDto, Error>> Handle(
        CreatePositionCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command.Dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        var nameResult = PositionName.Create(command.Dto.Name);
        if (nameResult.IsFailure)
        {
            return nameResult.Error;
        }

        var position = Position.Create(nameResult.Value);

        await _positionsRepository.AddAsync(position, cancellationToken);

        var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        LogPositionCreated(position.Id.Value);

        return new PositionDto(position.Id.Value, position.Name.Value);
    }

    [LoggerMessage(
        LogLevel.Information,
        "Position created with ID {PositionId}")]
    private partial void LogPositionCreated(Guid positionId);
}
