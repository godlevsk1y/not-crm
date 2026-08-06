using CSharpFunctionalExtensions;
using DirectoryService.Shared.Errors;

namespace DirectoryService.Core.Database;

public interface ITransactionManager
{
    Task<Result<ITransaction, Error>> BeginTransactionAsync(CancellationToken cancellationToken);
    
    Task<UnitResult<Error>> SaveChangesAsync(CancellationToken cancellationToken);
}