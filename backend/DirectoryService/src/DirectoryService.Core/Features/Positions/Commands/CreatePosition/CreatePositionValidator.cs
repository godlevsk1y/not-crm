using DirectoryService.Contracts.WebApi.Positions;
using DirectoryService.Core.Validation;
using DirectoryService.Domain.ValueObjects;
using FluentValidation;

namespace DirectoryService.Core.Features.Positions.Commands.CreatePosition;

public class CreatePositionValidator : AbstractValidator<CreatePositionRequest>
{
    public CreatePositionValidator()
    {
        RuleFor(request => request.Name)
            .MustBeValueObject(PositionName.Create);
    }
}
