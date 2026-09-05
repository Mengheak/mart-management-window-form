using Microsoft.Data.SqlClient;

namespace MiniMart.DataAccess.Infrastructure;

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("A connection string is required.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    public string SafeDescription
    {
        get
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(_connectionString);
                return $"{builder.DataSource} / {builder.InitialCatalog}";
            }
            catch (ArgumentException)
            {
                return "(unparseable connection string)";
            }
        }
    }

    public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch
        {
            // The caller never receives the instance, so this method owns disposing it.
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}
