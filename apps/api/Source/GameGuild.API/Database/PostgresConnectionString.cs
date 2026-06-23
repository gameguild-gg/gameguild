using Npgsql;

namespace GameGuild.API.Database;

public static class PostgresConnectionString
{
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
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'))
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
