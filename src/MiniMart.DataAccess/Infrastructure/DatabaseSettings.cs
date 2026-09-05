using System.Configuration;

namespace MiniMart.DataAccess.Infrastructure;

public static class DatabaseSettings
{
    public const string ConnectionStringName = "MiniMartDb";

    public static string GetConnectionString()
    {
        var setting = ConfigurationManager.ConnectionStrings[ConnectionStringName];

        if (setting is null || string.IsNullOrWhiteSpace(setting.ConnectionString))
        {
            throw new ConfigurationErrorsException(
                $"No connection string named '{ConnectionStringName}' was found in App.config. " +
                "Add one pointing at your SQL Server instance and the MiniMartDB database.");
        }

        return setting.ConnectionString;
    }

    public static ISqlConnectionFactory CreateConnectionFactory() =>
        new SqlConnectionFactory(GetConnectionString());
}
