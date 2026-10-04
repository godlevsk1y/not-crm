using CSharpFunctionalExtensions;
using DirectoryService.Shared.Errors;

namespace DirectoryService.Core.Database;

public interface ITransactionManager
{
    Task<Result<ITransaction, Error>> BeginTransactionAsync(CancellationToken cancellationToken);
    
    Task<UnitResult<Error>> SaveChangesAsync(CancellationToken cancellationToken);
    
    Task<Result<T, Error>> ExecuteAsync<T>(Func<CancellationToken, Task<Result<T, Error>>> operation, 
        CancellationToken cancellationToken);
}