using CSharpFunctionalExtensions;
using Shared.Kernel.Errors;

namespace Shared.Core.Abstractions;

public interface ICommandHandler<in TCommand, TResponse> 
    where TCommand : ICommand
{
    Task<Result<TResponse, Error>> Handle(TCommand command, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task<UnitResult<Error>> Handle(TCommand command, CancellationToken cancellationToken);
}