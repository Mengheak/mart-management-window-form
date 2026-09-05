using System.Data;
using Microsoft.Data.SqlClient;
using MiniMart.BusinessLogic.Models;
using MiniMart.BusinessLogic.Repositories;
using MiniMart.DataAccess.Infrastructure;

namespace MiniMart.DataAccess.Repositories;

public sealed class UserRepository : SqlRepositoryBase, IUserRepository
{
    private const int PasswordHashMaxLength = 256;

    private const int RoleMaxLength = 20;

    private const string SelectColumns = "UserId, Username, PasswordHash, Role, IsActive, CreatedAt";

    public UserRepository(ISqlConnectionFactory connectionFactory)
        : base(connectionFactory)
    {
    }

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            FROM dbo.Users
            ORDER BY Username;";

        return QueryAsync(sql, _ => { }, Map, cancellationToken);
    }

    public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            FROM dbo.Users
            WHERE UserId = @UserId;";

        return QuerySingleAsync(
            sql,
            parameters => parameters.Add("@UserId", SqlDbType.Int).Value = userId,
            Map,
            cancellationToken);
    }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        const string sql = $@"
            SELECT {SelectColumns}
            FROM dbo.Users
            WHERE Username = @Username;";

        return QuerySingleAsync(
            sql,
            parameters => parameters
                .Add("@Username", SqlDbType.NVarChar, User.UsernameMaxLength)
                .Value = username ?? string.Empty,
            Map,
            cancellationToken);
    }

    public async Task<int> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        const string sql = @"
            INSERT INTO dbo.Users (Username, PasswordHash, Role, IsActive)
            OUTPUT INSERTED.UserId
            VALUES (@Username, @PasswordHash, @Role, @IsActive);";

        return await ExecuteScalarAsync<int>(
            sql,
            parameters => BindUser(parameters, user),
            cancellationToken).ConfigureAwait(false);
    }

    public Task<int> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        const string sql = @"
            UPDATE dbo.Users
            SET Username     = @Username,
                PasswordHash = @PasswordHash,
                Role         = @Role,
                IsActive     = @IsActive
            WHERE UserId = @UserId;";

        return ExecuteNonQueryAsync(
            sql,
            parameters =>
            {
                BindUser(parameters, user);
                parameters.Add("@UserId", SqlDbType.Int).Value = user.UserId;
            },
            cancellationToken);
    }

    public Task<int> DeleteAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            DELETE FROM dbo.Users
            WHERE UserId = @UserId;";

        return ExecuteNonQueryAsync(
            sql,
            parameters => parameters.Add("@UserId", SqlDbType.Int).Value = userId,
            cancellationToken);
    }

    public async Task<bool> UsernameExistsAsync(
        string username,
        int excludeUserId = 0,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM dbo.Users
            WHERE Username = @Username
              AND UserId <> @ExcludeUserId;";

        var count = await ExecuteScalarAsync<int>(
            sql,
            parameters =>
            {
                parameters.Add("@Username", SqlDbType.NVarChar, User.UsernameMaxLength).Value = username ?? string.Empty;
                parameters.Add("@ExcludeUserId", SqlDbType.Int).Value = excludeUserId;
            },
            cancellationToken).ConfigureAwait(false);

        return count > 0;
    }

    public Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM dbo.Users
            WHERE Role = @Role AND IsActive = 1;";

        return ExecuteScalarAsync<int>(
            sql,
            parameters => parameters.Add("@Role", SqlDbType.NVarChar, RoleMaxLength).Value = nameof(UserRole.Admin),
            cancellationToken);
    }

    public async Task<bool> HasSalesHistoryAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM dbo.Sales
            WHERE CashierId = @UserId;";

        var count = await ExecuteScalarAsync<int>(
            sql,
            parameters => parameters.Add("@UserId", SqlDbType.Int).Value = userId,
            cancellationToken).ConfigureAwait(false);

        return count > 0;
    }

    private static void BindUser(SqlParameterCollection parameters, User user)
    {
        parameters.Add("@Username", SqlDbType.NVarChar, User.UsernameMaxLength).Value = user.Username;
        parameters.Add("@PasswordHash", SqlDbType.NVarChar, PasswordHashMaxLength).Value = user.PasswordHash;
        parameters.Add("@Role", SqlDbType.NVarChar, RoleMaxLength).Value = user.Role.ToString();
        parameters.Add("@IsActive", SqlDbType.Bit).Value = user.IsActive;
    }

    private static User Map(SqlDataReader reader) =>
        User.FromDatabase(
            reader.GetInt32(reader.GetOrdinal("UserId")),
            reader.GetString(reader.GetOrdinal("Username")),
            reader.GetString(reader.GetOrdinal("PasswordHash")),
            reader.GetString(reader.GetOrdinal("Role")),
            reader.GetBoolean(reader.GetOrdinal("IsActive")),
            reader.GetDateTime(reader.GetOrdinal("CreatedAt")));
}
