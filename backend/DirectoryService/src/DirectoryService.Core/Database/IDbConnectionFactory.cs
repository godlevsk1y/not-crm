using System.Data.Common;

namespace DirectoryService.Core.Database;

public interface IDbConnectionFactory
{
    Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken);
}