namespace GameGuild;

/// <summary>
/// Static builder for database configuration options.
/// </summary>
public static class DatabaseOptionsBuilder
{
    /// <summary>
    /// Creates a new database options builder.
    /// </summary>
    public static DatabaseOptionsBuilderInstance Create(IConfiguration configuration) { return new DatabaseOptionsBuilderInstance(configuration); }

    /// <summary>
    /// Creates default database options from configuration.
    /// </summary>
    public static DatabaseOptions CreateDefault(IConfiguration configuration) { return Create(configuration).UseDefaultConfiguration().Build(); }

    /// <summary>
    /// Creates database options with in-memory database for testing.
    /// </summary>
    public static DatabaseOptions CreateInMemory()
    {
        return new DatabaseOptions
        {
            UseInMemoryDatabase = true, EnableSensitiveDataLogging = true, EnableDetailedErrors = true
        };
    }

    /// <summary>
    /// Creates database options with SQLite for development.
    /// </summary>
    public static DatabaseOptions CreateSqLite(string connectionString)
    {
        return new DatabaseOptions
        {
            ConnectionString = connectionString, UseInMemoryDatabase = false, EnableSensitiveDataLogging = IsEnvironment("Development"), EnableDetailedErrors = IsEnvironment("Development")
        };
    }

    /// <summary>
    /// Checks if the current environment matches the specified environment name.
    /// </summary>
    internal static bool IsEnvironment(string environmentName)
    {
        var currentEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        return environmentName.Equals(currentEnvironment, StringComparison.OrdinalIgnoreCase);
    }

    public class DatabaseOptionsBuilderInstance
    {
        private readonly IConfiguration _configuration;

        private readonly DatabaseOptions _options = new DatabaseOptions();

        internal DatabaseOptionsBuilderInstance(IConfiguration configuration) { _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration)); }

        public DatabaseOptionsBuilderInstance UseConnectionString(string connectionString)
        {
            _options.ConnectionString = connectionString;

            return this;
        }

        public DatabaseOptionsBuilderInstance UseConnectionStringFromConfiguration(string configurationKey = "DefaultConnection")
        {
            var connectionString = _configuration.GetConnectionString(configurationKey);
            if (!string.IsNullOrEmpty(connectionString))
            {
                _options.ConnectionString = connectionString;
            }

            return this;
        }

        public DatabaseOptionsBuilderInstance UseConnectionStringFromEnvironment(string environmentVariable = "DB_CONNECTION_STRING")
        {
            var connectionString = Environment.GetEnvironmentVariable(environmentVariable);
            if (!string.IsNullOrEmpty(connectionString))
            {
                _options.ConnectionString = connectionString;
            }

            return this;
        }

        public DatabaseOptionsBuilderInstance UseInMemoryDatabase(bool useInMemory = true)
        {
            _options.UseInMemoryDatabase = useInMemory;

            return this;
        }

        public DatabaseOptionsBuilderInstance UseSqliteDatabase()
        {
            _options.UseInMemoryDatabase = false;

            return this;
        }

        public DatabaseOptionsBuilderInstance EnableSensitiveDataLogging(bool enable = true)
        {
            _options.EnableSensitiveDataLogging = enable;

            return this;
        }

        public DatabaseOptionsBuilderInstance EnableDetailedErrors(bool enable = true)
        {
            _options.EnableDetailedErrors = enable;

            return this;
        }

        public DatabaseOptionsBuilderInstance WithMigrationsHistoryTable(string tableName)
        {
            _options.MigrationsHistoryTable = tableName;

            return this;
        }

        public DatabaseOptionsBuilderInstance WithSchemaName(string schemaName)
        {
            _options.SchemaName = schemaName;

            return this;
        }

        public DatabaseOptionsBuilderInstance WithDevelopmentSettings()
        {
            _options.EnableSensitiveDataLogging = true;
            _options.EnableDetailedErrors = true;

            return this;
        }

        public DatabaseOptionsBuilderInstance WithProductionSettings()
        {
            _options.EnableSensitiveDataLogging = false;
            _options.EnableDetailedErrors = false;

            return this;
        }

        public DatabaseOptionsBuilderInstance UseDefaultConfiguration()
        {
            _options.ConnectionString = GetDatabaseConnectionString(_configuration);
            _options.UseInMemoryDatabase = ShouldUseInMemoryDatabase();
            _options.EnableSensitiveDataLogging = DatabaseOptionsBuilder.IsEnvironment("Development");
            _options.EnableDetailedErrors = DatabaseOptionsBuilder.IsEnvironment("Development");

            return this;
        }

        public DatabaseOptionsBuilderInstance Reset()
        {
            _options.ConnectionString = string.Empty;
            _options.UseInMemoryDatabase = false;
            _options.EnableSensitiveDataLogging = false;
            _options.EnableDetailedErrors = false;
            _options.MigrationsHistoryTable = "__EFMigrationsHistory";
            _options.SchemaName = "dbo";

            return this;
        }

        public DatabaseOptions Build()
        {
            _options.Validate();

            return _options;
        }

        private static string GetDatabaseConnectionString(IConfiguration configuration)
        {
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
            connectionString ??= configuration.GetConnectionString("DB_CONNECTION_STRING");
            connectionString ??= configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "Database connection string not found. Please set DB_CONNECTION_STRING environment variable " +
                    "or configure 'ConnectionStrings:DB_CONNECTION_STRING' in appSettings.json"
                );
            }

            return connectionString;
        }

        private static bool ShouldUseInMemoryDatabase()
        {
            var useInMemoryEnv = Environment.GetEnvironmentVariable("USE_IN_MEMORY_DB");

            return !string.IsNullOrEmpty(useInMemoryEnv) &&
                   useInMemoryEnv.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
