using System.Data;
using CSharpFunctionalExtensions;
using DirectoryService.Core.Database;
using DirectoryService.Core.Features.Locations;
using DirectoryService.Shared.Errors;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Infrastructure.Postgres.Transactions;

public partial class Transaction : ITransaction
{
    private readonly IDbTransaction _transaction;
    private readonly ILogger<Transaction> _logger;
    private bool _completed;

    public Transaction(
        IDbTransaction transaction,
        ILogger<Transaction> logger)
    {
        _transaction = transaction;
        _logger = logger;
    }

    public UnitResult<Error> Commit()
    {
        try
        {
            _transaction.Commit();
            _completed = true;
            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            LogCommitFailed(ex);
            return Error.Internal(new ErrorMessage("transaction.commit.failed", "Failed to commit transaction"));
        }
    }

    public UnitResult<Error> Rollback()
    {
        try
        {
            _transaction.Rollback();
            _completed = true;
            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            LogRollbackFailed(ex);
            return Error.Internal(new ErrorMessage("transaction.rollback.failed", "Failed to rollback transaction"));
        }
    }
    
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
            return;

        if (!_completed)
        {
            try
            {
                _transaction.Rollback();
            }
            catch (Exception ex)
            {
                LogRollbackFailed(ex);
            }
        }

        _transaction.Dispose();
    }
    
    [LoggerMessage(LogLevel.Error, "Failed to commit transaction")]
    private partial void LogCommitFailed(Exception exception);
    
    [LoggerMessage(LogLevel.Error, "Failed to rollback transaction")]
    private partial void LogRollbackFailed(Exception exception);
}