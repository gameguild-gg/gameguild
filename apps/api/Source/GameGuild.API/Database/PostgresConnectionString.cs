using Npgsql;
using Microsoft.Extensions.Configuration;

namespace GameGuild.API.Database;

public static class PostgresConnectionString
{
    public static string? Resolve(IConfiguration configuration, string connectionName = "DefaultConnection")
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var postgresPartsConnectionString = BuildFromPostgresParts(configuration);
        if (!string.IsNullOrWhiteSpace(postgresPartsConnectionString))
        {
            return postgresPartsConnectionString;
        }

        var configuredConnectionString = configuration.GetConnectionString(connectionName)
            ?? configuration[$"ConnectionStrings:{connectionName}"];

        return Normalize(configuredConnectionString);
    }

    public static string? Normalize(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) ||
            !IsPostgresUri(uri))
        {
            return connectionString;
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            GssEncryptionMode = GssEncryptionMode.Disable
        };

        if (!string.IsNullOrWhiteSpace(uri.UserInfo))
        {
            var userInfoParts = uri.UserInfo.Split(':', 2);
            builder.Username = Uri.UnescapeDataString(userInfoParts[0]);

            if (userInfoParts.Length == 2)
            {
                builder.Password = Uri.UnescapeDataString(userInfoParts[1]);
            }
        }

        ApplyQueryParameters(uri, builder);

        return builder.ConnectionString;
    }

    private static string? BuildFromPostgresParts(IConfiguration configuration)
    {
        var host = configuration["POSTGRES_HOST"];
        var database = configuration["POSTGRES_DB"];
        var username = configuration["POSTGRES_USER"];
        var password = configuration["POSTGRES_PASSWORD"];

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(database) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host.Trim(),
            Port = TryParsePort(configuration["POSTGRES_PORT"]) ?? 5432,
            Database = database.Trim(),
            Username = username.Trim(),
            Password = password,
            GssEncryptionMode = GssEncryptionMode.Disable,
            IncludeErrorDetail = bool.TryParse(configuration["POSTGRES_INCLUDE_ERROR_DETAIL"], out var includeErrorDetail) && includeErrorDetail,
            MaxPoolSize = TryParseInt(configuration["POSTGRES_MAX_POOL_SIZE"]) ?? 100,
            MinPoolSize = TryParseInt(configuration["POSTGRES_MIN_POOL_SIZE"]) ?? 5,
            ConnectionIdleLifetime = TryParseInt(configuration["POSTGRES_CONNECTION_IDLE_LIFETIME"]) ?? 300
        };

        var sslMode = configuration["POSTGRES_SSLMODE"];
        if (!string.IsNullOrWhiteSpace(sslMode) &&
            Enum.TryParse<SslMode>(sslMode.Replace("-", string.Empty, StringComparison.Ordinal), true, out var parsedSslMode))
        {
            builder.SslMode = parsedSslMode;
        }

        return builder.ConnectionString;
    }

    private static int? TryParsePort(string? value)
    {
        return int.TryParse(value, out var port) && port > 0 ? port : null;
    }

    private static int? TryParseInt(string? value)
    {
        return int.TryParse(value, out var parsed) && parsed >= 0 ? parsed : null;
    }

    private static bool IsPostgresUri(Uri uri)
    {
        return string.Equals(uri.Scheme, "postgres", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(uri.Scheme, "postgresql", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyQueryParameters(Uri uri, NpgsqlConnectionStringBuilder builder)
    {
        if (string.IsNullOrWhiteSpace(uri.Query))
        {
            return;
        }

        foreach (var parameter in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = parameter.Split('=', 2);
            if (parts.Length != 2)
            {
                continue;
            }

            var key = Uri.UnescapeDataString(parts[0]).Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
            var value = Uri.UnescapeDataString(parts[1]);

            if (key == "sslmode" && Enum.TryParse<SslMode>(value.Replace("-", string.Empty, StringComparison.Ordinal), true, out var sslMode))
            {
                builder.SslMode = sslMode;
            }
        }
    }
}
