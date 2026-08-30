using Npgsql;

namespace GameGuild.API.Database;

/// <summary>Validates startup migration boundaries before the HTTP host is constructed.</summary>
public static class DatabaseStartupConfiguration
{
    public static IReadOnlyList<string> Validate(IConfiguration configuration, string environmentName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (!ShouldRunStartupInitialization(configuration, environmentName) ||
            AllowsRuntimeFallback(environmentName))
        {
            return [];
        }

        var failures = new List<string>();
        var runtimeConnection = PostgresConnectionString.Resolve(configuration);
        var migrationConnection = ResolveMigrationConnectionString(configuration);
        var allowSameMigrationUser = AllowsSameMigrationUser(configuration);

        if (string.IsNullOrWhiteSpace(runtimeConnection))
            failures.Add("A runtime database connection is required.");

        if (string.IsNullOrWhiteSpace(migrationConnection) && !allowSameMigrationUser)
            failures.Add("A dedicated migration database connection is required.");

        if (failures.Count != 0)
            return failures;

        migrationConnection ??= runtimeConnection;

        try
        {
            var runtimeBuilder = new NpgsqlConnectionStringBuilder(runtimeConnection);
            var migrationBuilder = new NpgsqlConnectionStringBuilder(migrationConnection);
            var runtimeUser = runtimeBuilder.Username;
            var migrationUser = migrationBuilder.Username;

            if (string.Equals(runtimeUser, "postgres", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(runtimeBuilder.Password, "postgres", StringComparison.Ordinal))
            {
                failures.Add("The runtime database must not use default PostgreSQL credentials.");
            }
            else if (string.IsNullOrWhiteSpace(runtimeUser) || string.IsNullOrWhiteSpace(migrationUser))
            {
                failures.Add("Runtime and migration database connections must identify their roles.");
            }
            else if (string.Equals(runtimeUser, migrationUser, StringComparison.Ordinal) && !allowSameMigrationUser)
            {
                failures.Add("Runtime and migration database roles must be distinct.");
            }
        }
        catch (ArgumentException)
        {
            failures.Add("Runtime and migration database connections must be valid PostgreSQL connection strings.");
        }

        return failures;
    }

    public static void ThrowIfInvalid(IConfiguration configuration, string environmentName)
    {
        var failures = Validate(configuration, environmentName);
        if (failures.Count != 0)
            throw new InvalidOperationException($"Unsafe database startup configuration: {string.Join(" ", failures)}");
    }

    public static bool ShouldRunStartupInitialization(IConfiguration configuration, string environmentName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return configuration.GetValue<bool?>("Database:RunStartupInitialization")
            ?? !IsTestEnvironment(environmentName);
    }

    public static string? ResolveMigrationConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MigrationConnection")
            ?? configuration["ConnectionStrings:MigrationConnection"]
            ?? configuration["Database:MigrationConnectionString"]
            ?? configuration["POSTGRES_MIGRATION_CONNECTION"];

        return PostgresConnectionString.Normalize(connectionString);
    }

    /// <summary>
    /// Resolves whether a failed migration run must abort startup. Environments without runtime
    /// fallback (everything except Development/Test) always fail closed: an explicit
    /// <c>Database:FailStartupOnMigrationFailure=false</c> override is IGNORED so the API can never
    /// open its listener on a database whose migrations failed or rolled back.
    /// FailStartupOnSeedFailure/FailStartupOnGrantFailure intentionally keep the honor-the-override
    /// behavior for now (out of scope for this guard).
    /// </summary>
    internal static bool ResolveFailStartupOnMigrationFailure(IConfiguration configuration, string environmentName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return !AllowsRuntimeFallback(environmentName) ||
            configuration.GetValue<bool?>("Database:FailStartupOnMigrationFailure") == true;
    }

    internal static bool AllowsRuntimeFallback(string environmentName) =>
        string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase) ||
        IsTestEnvironment(environmentName);

    private static bool IsTestEnvironment(string environmentName) =>
        string.Equals(environmentName, "Test", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase);

    private static bool AllowsSameMigrationUser(IConfiguration configuration) =>
        configuration.GetValue<bool?>("Database:AllowSameMigrationUser") == true ||
        configuration.GetValue<bool?>("ALLOW_SAME_MIGRATION_USER") == true ||
        configuration.GetValue<bool?>("Database:RequireDistinctMigrationUser") == false;
}
