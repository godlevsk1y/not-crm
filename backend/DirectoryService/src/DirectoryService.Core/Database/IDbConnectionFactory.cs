using System.Data;

namespace DirectoryService.Core.Database;

public interface IDbConnectionFactory
{
    Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken);
}