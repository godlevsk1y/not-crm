using System.Data.Common;

namespace EmployeeService.Core.Database;

public interface IDbConnectionFactory
{
    Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken);
}
