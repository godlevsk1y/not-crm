using CSharpFunctionalExtensions;
using DirectoryService.Shared.Errors;

namespace DirectoryService.Core.Database;

public interface ITransaction : IAsyncDisposable
{
    Task<UnitResult<Error>> CommitAsync(CancellationToken cancellationToken);
    
    Task<UnitResult<Error>> RollbackAsync(CancellationToken cancellationToken);
}
