using System.Data;
using Microsoft.Data.SqlClient;

namespace MiniMart.DataAccess.Infrastructure;

public abstract class SqlRepositoryBase
{
    protected ISqlConnectionFactory ConnectionFactory { get; }

    protected SqlRepositoryBase(ISqlConnectionFactory connectionFactory)
    {
        ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    protected async Task<int> ExecuteNonQueryAsync(
        string commandText,
        Action<SqlParameterCollection> bindParameters,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await ConnectionFactory
            .CreateOpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var command = new SqlCommand(commandText, connection);
        bindParameters(command.Parameters);

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    protected async Task<T?> ExecuteScalarAsync<T>(
        string commandText,
        Action<SqlParameterCollection> bindParameters,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await ConnectionFactory
            .CreateOpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var command = new SqlCommand(commandText, connection);
        bindParameters(command.Parameters);

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        return result is null || result is DBNull ? default : (T)Convert.ChangeType(result, typeof(T));
    }

    protected async Task<IReadOnlyList<T>> QueryAsync<T>(
        string commandText,
        Action<SqlParameterCollection> bindParameters,
        Func<SqlDataReader, T> map,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await ConnectionFactory
            .CreateOpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var command = new SqlCommand(commandText, connection);
        bindParameters(command.Parameters);

        await using var reader = await command
            .ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        var results = new List<T>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(map(reader));
        }

        return results;
    }

    protected async Task<T?> QuerySingleAsync<T>(
        string commandText,
        Action<SqlParameterCollection> bindParameters,
        Func<SqlDataReader, T> map,
        CancellationToken cancellationToken = default)
        where T : class
    {
        await using var connection = await ConnectionFactory
            .CreateOpenConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var command = new SqlCommand(commandText, connection);
        bindParameters(command.Parameters);

        await using var reader = await command
            .ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken)
            .ConfigureAwait(false);

        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? map(reader) : null;
    }

    protected static string? ReadNullableString(SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
