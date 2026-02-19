using System.Data.Common;
using Fig.Api.ExtensionMethods;
using Microsoft.Data.SqlClient;

namespace Fig.Api.Datalayer;

public class DatabaseProviderResolver : IDatabaseProviderResolver
{
    private static readonly string[] PasswordKeys = ["Password", "Pwd"];

    public DatabaseProviderType ResolveProvider(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };

        if (IsSqlite(builder))
            return DatabaseProviderType.Sqlite;

        if (IsPostgreSql(builder))
            return DatabaseProviderType.PostgreSql;

        return DatabaseProviderType.SqlServer;
    }

    public string GetDialect(DatabaseProviderType provider)
    {
        return provider switch
        {
            DatabaseProviderType.Sqlite => "NHibernate.Dialect.SQLiteDialect",
            DatabaseProviderType.PostgreSql => "NHibernate.Dialect.PostgreSQL82Dialect",
            _ => "NHibernate.Dialect.MsSql2012Dialect"
        };
    }

    public string GetDriverClass(DatabaseProviderType provider)
    {
        return provider switch
        {
            DatabaseProviderType.Sqlite => "NHibernate.Driver.SQLite20Driver",
            DatabaseProviderType.PostgreSql => "NHibernate.Driver.NpgsqlDriver",
            _ => "NHibernate.Driver.MicrosoftDataSqlClientDriver"
        };
    }

    public string NormalizeConnectionString(DatabaseProviderType provider, string connectionString)
    {
        if (provider != DatabaseProviderType.SqlServer)
            return connectionString;

        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!builder.MultipleActiveResultSets)
            builder.MultipleActiveResultSets = true;

        return builder.ConnectionString.NormalizeToLegacyConnectionString();
    }

    public string GetConnectionStringForLogging(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return connectionString;

        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
        foreach (var key in PasswordKeys)
        {
            if (ContainsKey(builder, key))
                builder[key] = "******";
        }

        return builder.ConnectionString;
    }

    private static bool IsSqlite(DbConnectionStringBuilder builder)
    {
        if (TryGetValue(builder, "Data Source", out var dataSource) ||
            TryGetValue(builder, "URI", out dataSource))
        {
            var value = dataSource?.ToString() ?? string.Empty;
            if (!string.IsNullOrEmpty(value) &&
                (value.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase) ||
                 value.EndsWith(".db", StringComparison.OrdinalIgnoreCase) ||
                 value.Equals(":memory:", StringComparison.OrdinalIgnoreCase) ||
                 value.StartsWith("file:", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPostgreSql(DbConnectionStringBuilder builder)
    {
        return ContainsKey(builder, "Host") ||
               ContainsKey(builder, "Username") ||
               ContainsKey(builder, "Search Path");
    }

    private static bool TryGetValue(DbConnectionStringBuilder builder, string key, out object? value)
    {
        foreach (string existingKey in builder.Keys)
        {
            if (!string.Equals(existingKey, key, StringComparison.OrdinalIgnoreCase))
                continue;

            value = builder[existingKey];
            return true;
        }

        value = null;
        return false;
    }

    private static bool ContainsKey(DbConnectionStringBuilder builder, string key)
    {
        foreach (string existingKey in builder.Keys)
        {
            if (string.Equals(existingKey, key, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
