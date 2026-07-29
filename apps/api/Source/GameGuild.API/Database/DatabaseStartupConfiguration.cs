using Npgsql;

namespace GameGuild.API.Database;

/// <summary>Validates startup migration boundaries before the HTTP host is constructed.</summary>
public static class DatabaseStartupConfiguration
{
    public static IReadOnlyList<string> Validate(IConfiguration configuration, string environmentName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (!(configuration.GetValue<bool?>("Database:RunStartupInitialization") ?? true) ||
            AllowsRuntimeFallback(environmentName) ||
            configuration.GetValue<bool?>("Database:AllowSameMigrationUser") == true ||
            configuration.GetValue<bool?>("ALLOW_SAME_MIGRATION_USER") == true)
        {
            return [];
        }

        var failures = new List<string>();
        var runtimeConnection = PostgresConnectionString.Resolve(configuration);
        var migrationConnection = ResolveMigrationConnectionString(configuration);

        if (string.IsNullOrWhiteSpace(runtimeConnection))
            failures.Add("A runtime database connection is required.");
        if (string.IsNullOrWhiteSpace(migrationConnection))
            failures.Add("A distinct migration connection is required outside Development and Test environments.");

        if (failures.Count != 0)
            return failures;

        try
        {
            var runtimeUser = new NpgsqlConnectionStringBuilder(runtimeConnection).Username;
            var migrationUser = new NpgsqlConnectionStringBuilder(migrationConnection).Username;
            if (string.IsNullOrWhiteSpace(runtimeUser) || string.IsNullOrWhiteSpace(migrationUser))
                failures.Add("Runtime and migration database connections must identify their roles.");
            else if (string.Equals(runtimeUser, migrationUser, StringComparison.Ordinal))
                failures.Add("Runtime and migration database roles must be distinct.");
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

    public static string? ResolveMigrationConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MigrationConnection")
            ?? configuration["ConnectionStrings:MigrationConnection"]
            ?? configuration["Database:MigrationConnectionString"]
            ?? configuration["POSTGRES_MIGRATION_CONNECTION"];

        return PostgresConnectionString.Normalize(connectionString);
    }

    internal static bool AllowsRuntimeFallback(string environmentName) =>
        string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(environmentName, "Test", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase);
}
