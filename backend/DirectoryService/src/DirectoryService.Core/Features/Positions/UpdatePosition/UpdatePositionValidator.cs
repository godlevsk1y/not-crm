using DirectoryService.Contracts.WebApi.Positions;
using DirectoryService.Core.Validation;
using DirectoryService.Domain.ValueObjects;
using FluentValidation;

namespace DirectoryService.Core.Features.Positions.UpdatePosition;

public class UpdatePositionValidator : AbstractValidator<UpdatePositionRequest>
{
    public UpdatePositionValidator()
    {
        RuleFor(request => request.Name)
            .MustBeValueObject(PositionName.Create);
    }
}
