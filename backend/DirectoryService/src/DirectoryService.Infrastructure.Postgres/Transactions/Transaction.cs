using CSharpFunctionalExtensions;
using DirectoryService.Core.Database;
using DirectoryService.Shared.Errors;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Infrastructure.Postgres.Transactions;

public partial class Transaction : ITransaction
{
    private readonly IDbContextTransaction _transaction;
    private readonly ILogger<Transaction> _logger;
    private bool _completed;

    public Transaction(
        IDbContextTransaction transaction,
        ILogger<Transaction> logger)
    {
        _transaction = transaction;
        _logger = logger;
    }

    public async Task<UnitResult<Error>> CommitAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _transaction.CommitAsync(cancellationToken);
            _completed = true;
            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            LogCommitFailed(ex);
            return Error.Internal(new ErrorMessage("transaction.commit.failed", "Failed to commit transaction"));
        }
    }

    public async Task<UnitResult<Error>> RollbackAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _transaction.RollbackAsync(cancellationToken);
            _completed = true;
            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            LogRollbackFailed(ex);
            return Error.Internal(new ErrorMessage("transaction.rollback.failed", "Failed to rollback transaction"));
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (!_completed)
        {
            try
            {
                await _transaction.RollbackAsync();
            }
            catch (Exception ex)
            {
                LogRollbackFailed(ex);
            }
        }

        await _transaction.DisposeAsync();
    }
    
    [LoggerMessage(LogLevel.Error, "Failed to commit transaction")]
    private partial void LogCommitFailed(Exception exception);
    
    [LoggerMessage(LogLevel.Error, "Failed to rollback transaction")]
    private partial void LogRollbackFailed(Exception exception);
}
