using Fig.Api.Datalayer;

namespace Fig.Api.DatabaseMigrations;

public static class DatabaseMigrationExtensions
{
    public static string GetScriptForProvider(this IDatabaseMigration migration, DatabaseProviderType provider)
    {
        return provider switch
        {
            DatabaseProviderType.Sqlite => migration.SqliteScript,
            DatabaseProviderType.PostgreSql => migration.PostgreSqlScript,
            _ => migration.SqlServerScript
        };
    }
}
