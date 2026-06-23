using Npgsql;

namespace GameGuild.API.Database;

public sealed class DatabaseConnectivityProbe(IConfiguration configuration)
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(1);

    public async Task<bool> IsReachableAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["ConnectionStrings:DefaultConnection"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        NpgsqlConnectionStringBuilder connectionStringBuilder;
        try
        {
            connectionStringBuilder = new NpgsqlConnectionStringBuilder(
                PostgresConnectionString.Normalize(connectionString) ?? connectionString);
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(connectionStringBuilder.Host) || connectionStringBuilder.Port <= 0)
        {
            return false;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(ProbeTimeout);

        try
        {
            connectionStringBuilder.Timeout = Math.Max(1, (int)Math.Ceiling(ProbeTimeout.TotalSeconds));
            connectionStringBuilder.CommandTimeout = Math.Max(1, (int)Math.Ceiling(ProbeTimeout.TotalSeconds));

            await using var connection = new NpgsqlConnection(connectionStringBuilder.ConnectionString);
            await connection.OpenAsync(timeoutCts.Token).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (NpgsqlException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
