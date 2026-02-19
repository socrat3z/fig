namespace Fig.Api.Datalayer;

public interface IDatabaseProviderResolver
{
    DatabaseProviderType ResolveProvider(string connectionString);
    string GetDialect(DatabaseProviderType provider);
    string GetDriverClass(DatabaseProviderType provider);
    string NormalizeConnectionString(DatabaseProviderType provider, string connectionString);
    string GetConnectionStringForLogging(string connectionString);
}
