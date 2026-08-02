namespace GameGuild;

/// <summary>
///     Centralized clock abstraction for the domain layer.
///     Uses <see cref="TimeProvider"/> (.NET 8+) to enable deterministic testing.
///     All domain code should use <c>SystemClock.UtcNow</c> instead of <c>DateTime.UtcNow</c>.
/// </summary>
/// <remarks>
///     In production, this uses <see cref="TimeProvider.System"/>.
///     In tests, call <see cref="SetProvider"/> with a fake <see cref="TimeProvider"/>
///     to control time deterministically.
/// </remarks>
public static class SystemClock
{
    private static readonly AsyncLocal<TimeProvider?> Provider = new();

    /// <summary>
    ///     Gets the current UTC date and time.
    /// </summary>
    public static DateTime UtcNow => (Provider.Value ?? TimeProvider.System).GetUtcNow().UtcDateTime;

    /// <summary>
    ///     Sets the time provider (for testing purposes only).
    /// </summary>
    /// <param name="provider">The time provider to use</param>
    public static void SetProvider(TimeProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        Provider.Value = provider;
    }

    /// <summary>
    ///     Resets to the system time provider.
    /// </summary>
    public static void Reset()
    {
        Provider.Value = null;
    }
}
