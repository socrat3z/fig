using Fig.Api.Constants;
using Fig.Client.Abstractions.Data;
using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.Mappings;
using Microsoft.Extensions.Options;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Mapping.ByCode;
using NHibernate.Tool.hbm2ddl;

namespace Fig.Api.Datalayer;

public class FigSessionFactory : IFigSessionFactory
{
    private const string UserTableCreationPart = "create table users (";
    private readonly ILogger<FigSessionFactory> _logger;
    private readonly IOptions<ApiSettings> _settings;
    private readonly IConfiguration _appConfiguration;
    private readonly IDatabaseProviderResolver _databaseProviderResolver;
    private Configuration? _configuration;
    private bool _isDatabaseNewlyCreated;
    private HbmMapping? _mapping;
    private ISessionFactory? _sessionFactory;
    private DatabaseProviderType? _databaseProvider;

    public FigSessionFactory(
        ILogger<FigSessionFactory> logger,
        IOptions<ApiSettings> settings,
        IConfiguration appConfiguration,
        IDatabaseProviderResolver databaseProviderResolver)
    {
        _logger = logger;
        _settings = settings;
        _appConfiguration = appConfiguration;
        _databaseProviderResolver = databaseProviderResolver;
        
        MigrateDatabase();
        CreateDefaultUser();
    }

    public ISessionFactory SessionFactory => _sessionFactory ??= Configuration.BuildSessionFactory();

    private Configuration Configuration => _configuration ??= CreateConfiguration();

    private HbmMapping Mapping => _mapping ??= CreateMapping();

    private void MigrateDatabase()
    {
        _logger.LogInformation("Starting database migration...");
    
        try
        {
            var schemaUpdate = new SchemaUpdate(Configuration);
        
            _logger.LogInformation("Checking for database connection...");
            schemaUpdate.Execute(CheckForUserTableCreation, true);

            if (schemaUpdate.Exceptions.Any())
            {
                foreach (var exception in schemaUpdate.Exceptions)
                {
                    _logger.LogError(exception, "Exception while updating database schema: {Message}", exception.Message);
                }
            }
            else
            {
                _logger.LogInformation("Database migration completed successfully");
            }
        }
        catch (HibernateException ex)
        {
            _logger.LogError(ex, "Failed to perform database migration: unable to connect to the database");
            throw; // Rethrow if you want the application to handle the failure further up.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during database migration");
            throw;
        }
    }

    private void CheckForUserTableCreation(string sql)
    {
        if (sql.Contains(UserTableCreationPart))
            _isDatabaseNewlyCreated = true;

        // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
        _logger.LogInformation(sql);
    }

    private Configuration CreateConfiguration()
    {
        var connectionString = PrepareConnectionString();
        var provider = GetProvider(connectionString);
        var configuration = new Configuration();
        
        configuration.SetProperty("connection.connection_string", connectionString);
        configuration.SetProperty("connection.driver_class", _databaseProviderResolver.GetDriverClass(provider));
        configuration.SetProperty("dialect", _databaseProviderResolver.GetDialect(provider));

        //Loads properties from hibernate.cfg.xml
        configuration.Configure();

        //Loads nhibernate mappings 
        configuration.AddDeserializedMapping(Mapping, null);

        return configuration;
    }

    private DatabaseProviderType GetProvider(string? connectionString)
    {
        if (_databaseProvider is not null)
            return _databaseProvider.Value;

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        _databaseProvider = _databaseProviderResolver.ResolveProvider(connectionString);
        return _databaseProvider.Value;
    }

    private HbmMapping CreateMapping()
    {
        var mapper = new ModelMapper();

        mapper.AddMappings(new List<Type>
        {
            typeof(SettingsClientMap),
            typeof(SettingMap),
            typeof(SettingValueMap),
            typeof(ClientStatusMap),
            typeof(ClientRunSessionMap),
            typeof(EventLogMap),
            typeof(UserMap),
            typeof(LookupTableMap),
            typeof(FigConfigurationMapping),
            typeof(ApiStatusMap),
            typeof(DeferredClientImportMap),
            typeof(WebHookClientMap),
            typeof(WebHookMap),
            typeof(SettingChangeMap),
            typeof(CheckPointMap),
            typeof(CheckPointDataMap),
            typeof(DeferredClientImportMap),
            typeof(DeferredChangeMap),
            typeof(CheckPointTriggerMap),
            typeof(CustomActionExecutionMap),
            typeof(CustomActionMap),
            typeof(DatabaseMigrationMap),
            typeof(ClientRegistrationHistoryMap)
        });

        return mapper.CompileMappingForAllExplicitlyAddedEntities();
    }

    private void CreateDefaultUser()
    {
        // Default user is only created when the database is being created.
        if (!_isDatabaseNewlyCreated)
            return;

        var defaultUser = new UserBusinessEntity
        {
            Username = DefaultUser.UserName,
            FirstName = "Default",
            LastName = "User",
            Role = Role.Administrator,
            ClientFilter = ".*",
            PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(DefaultUser.Password),
            AllowedClassifications = Enum.GetValues(typeof(Classification)).Cast<Classification>().ToList()
        };

        using var session = SessionFactory.OpenSession();
        using var transaction = session.BeginTransaction();
        session.Save(defaultUser);
        transaction.Commit();
    }

    private string? PrepareConnectionString()
    {
        var connectionString = _appConfiguration.GetConnectionString("Fig");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = _settings.Value.DbConnectionString;
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _logger.LogError("Connection string is null. Fig will not start");
            return null;
        }

        var provider = GetProvider(connectionString);
        if (provider != DatabaseProviderType.Sqlite)
        {
            var logConnectionString = _databaseProviderResolver.GetConnectionStringForLogging(connectionString);
            _logger.LogInformation("Connecting to database with connection string {ConnectionString}",
                logConnectionString);
        }

        return _databaseProviderResolver.NormalizeConnectionString(provider, connectionString);
    }
}
