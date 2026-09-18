using CSharpFunctionalExtensions;
using DirectoryService.Core.Database;
using DirectoryService.Shared.Errors;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Infrastructure.Postgres.Transactions;

public partial class TransactionManager : ITransactionManager
{
    private readonly DirectoryServiceDbContext _context;
    private readonly ILogger<TransactionManager> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public TransactionManager(
        DirectoryServiceDbContext context,
        ILogger<TransactionManager> logger,
        ILoggerFactory loggerFactory)
    {
        _context = context;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public async Task<Result<ITransaction, Error>> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            var transactionScopeLogger = _loggerFactory.CreateLogger<Transaction>();
            var transactionScope = new Transaction(transaction, transactionScopeLogger);

            return transactionScope;
        }
        catch (OperationCanceledException)
            when(cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogBeginTransactionFailed(ex);
            return GeneralErrors.Internal();
        }
    }

    public async Task<UnitResult<Error>> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return UnitResult.Success<Error>();
        }
        catch (OperationCanceledException)
            when(cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (PostgresExceptionMapper.TryMap(ex, out var error))
            {
                return error;
            }
            
            LogFailedToSaveChanges(ex);
            
            return GeneralErrors.Internal();
        }
    }

    
    [LoggerMessage(LogLevel.Error, "Failed to begin transaction")]
    private partial void LogBeginTransactionFailed(Exception exception);

    [LoggerMessage(LogLevel.Error, "Failed to save changes")]
    private partial void LogFailedToSaveChanges(Exception exception);
}