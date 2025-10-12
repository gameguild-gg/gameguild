using GameGuild.Database;


namespace GameGuild.Core.Extensions;

/// <summary>
///     Extensions for PostgreSQL Row-Level Security (RLS) integration
/// </summary>
public static class RowLevelSecurityExtensions
{
    /// <summary>
    ///     Sets the current tenant ID for PostgreSQL RLS policies
    /// </summary>
    public static async Task SetTenantContextAsync(this ApplicationDbContext context, Guid? tenantId)
    {
        if (tenantId.HasValue)
        {
            await context.Database.ExecuteSqlRawAsync(
                "SET app.current_tenant_id = {0}",
                tenantId.Value.ToString()
            );
        }
        else
        {
            await context.Database.ExecuteSqlRawAsync("SET app.current_tenant_id = ''");
        }
    }

    /// <summary>
    ///     Enables RLS bypass for administrative operations
    /// </summary>
    public static async Task EnableRlsBypassAsync(this ApplicationDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync("SET app.bypass_rls = 'true'");
    }

    /// <summary>
    ///     Disables RLS bypass
    /// </summary>
    public static async Task DisableRlsBypassAsync(this ApplicationDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync("SET app.bypass_rls = 'false'");
    }

    /// <summary>
    ///     Executes a block of code with RLS bypass enabled
    /// </summary>
    public static async Task<T> WithRlsBypassAsync<T>(
        this ApplicationDbContext context,
        Func<Task<T>> operation)
    {
        await context.EnableRlsBypassAsync();
        try
        {
            return await operation();
        }
        finally
        {
            await context.DisableRlsBypassAsync();
        }
    }

    /// <summary>
    ///     Executes a block of code with RLS bypass enabled (void return)
    /// </summary>
    public static async Task WithRlsBypassAsync(
        this ApplicationDbContext context,
        Func<Task> operation)
    {
        await context.EnableRlsBypassAsync();
        try
        {
            await operation();
        }
        finally
        {
            await context.DisableRlsBypassAsync();
        }
    }
}
