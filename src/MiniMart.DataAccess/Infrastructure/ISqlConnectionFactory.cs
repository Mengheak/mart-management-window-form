using Microsoft.Data.SqlClient;

namespace MiniMart.DataAccess.Infrastructure;

public interface ISqlConnectionFactory
{
    string SafeDescription { get; }

    Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
