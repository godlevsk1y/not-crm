using CSharpFunctionalExtensions;
using DirectoryService.Contracts.WebApi.Positions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Core.Extensions;
using DirectoryService.Domain.Ids;
using DirectoryService.Domain.ValueObjects;
using DirectoryService.Shared.Errors;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Features.Positions.UpdatePosition;

public partial class UpdatePositionHandler : ICommandHandler<UpdatePositionCommand, Guid>
{
    private readonly IPositionsRepository _positionsRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<UpdatePositionRequest> _validator;
    private readonly ILogger<UpdatePositionHandler> _logger;

    public UpdatePositionHandler(
        IPositionsRepository positionsRepository,
        ITransactionManager transactionManager,
        IValidator<UpdatePositionRequest> validator,
        ILogger<UpdatePositionHandler> logger)
    {
        _positionsRepository = positionsRepository;
        _transactionManager = transactionManager;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<Guid, Error>> Handle(
        UpdatePositionCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command.Dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        var position = await _positionsRepository.GetByIdAsync(
            new PositionId(command.Id),
            cancellationToken);
        if (position is null)
        {
            return PositionErrors.NotFound(command.Id);
        }

        var nameResult = PositionName.Create(command.Dto.Name);
        if (nameResult.IsFailure)
        {
            return nameResult.Error;
        }

        position.Rename(nameResult.Value);

        var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        LogPositionUpdated(position.Id.Value);

        return position.Id.Value;
    }

    [LoggerMessage(
        LogLevel.Information,
        "Position updated with ID {PositionId}")]
    private partial void LogPositionUpdated(Guid positionId);
}
