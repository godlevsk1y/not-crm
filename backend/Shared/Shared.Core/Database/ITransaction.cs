using CSharpFunctionalExtensions;
using Shared.Kernel.Errors;

namespace Shared.Core.Database;

public interface ITransaction : IAsyncDisposable
{
    Task<UnitResult<Error>> CommitAsync(CancellationToken cancellationToken);
    
    Task<UnitResult<Error>> RollbackAsync(CancellationToken cancellationToken);
}
