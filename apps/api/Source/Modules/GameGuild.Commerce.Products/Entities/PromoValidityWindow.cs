namespace GameGuild.Commerce.Products;

/// <summary>
/// Outcome of evaluating a promo code's validity window against a UTC instant
/// (issue #418 timezone awareness). The two failure edges are distinct so callers
/// can reject expired and not-yet-valid codes with separate outcomes.
/// </summary>
public enum PromoWindowOutcome
{
    /// <summary>The instant is inside the window (or the window is unbounded).</summary>
    WithinWindow = 0,

    /// <summary>The instant is before <c>ValidFrom</c>: the code is not yet valid.</summary>
    NotYetValid = 1,

    /// <summary>The instant is at or after <c>ValidUntil</c>: the code has expired.</summary>
    Expired = 2
}

/// <summary>
/// Timezone-aware validity window handling for promo codes (issue #418). Validity
/// instants are stored as UTC; the window may be *expressed* in an IANA timezone
/// (a wall-clock date such as "2026-11-01 00:00" in America/Sao_Paulo), in which
/// case the wall-clock value is converted to the UTC instant at write time.
/// </summary>
public static class PromoValidityWindow
{
    /// <summary>Fallback timezone id used when none is configured.</summary>
    public const string UtcTimeZoneId = "UTC";

    /// <summary>
    /// Resolves an IANA timezone id. Unknown ids fall back to UTC — validity must
    /// never be silently widened by an unresolvable zone, and callers surface the
    /// resolved id so the stored window stays inspectable.
    /// </summary>
    /// <returns>The resolved zone and whether the supplied id was valid.</returns>
    public static (TimeZoneInfo TimeZone, bool Resolved) ResolveTimeZone(string? timeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(timeZoneId)
            && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId.Trim(), out var zone))
        {
            return (zone, true);
        }

        return (TimeZoneInfo.Utc, false);
    }

    /// <summary>
    /// Whether the supplied id names a timezone the runtime can resolve (IANA on
    /// Linux/CI, Windows zone ids also accepted by the ICU mapping on Windows).
    /// </summary>
    public static bool IsKnownTimeZone(string? timeZoneId)
        => !string.IsNullOrWhiteSpace(timeZoneId)
           && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId.Trim(), out _);

    /// <summary>
    /// Normalizes a caller-supplied validity edge to a UTC instant:
    /// <list type="bullet">
    /// <item><c>Unspecified</c> kind is treated as a wall-clock value in <paramref name="timeZone"/>
    /// (the code's timezone, defaulting to UTC) and converted to the equivalent UTC instant;</item>
    /// <item><c>Utc</c> and <c>Local</c> kinds are converted to UTC directly.</item>
    /// </list>
    /// Null passes through unchanged (unbounded edge).
    /// </summary>
    public static DateTime? NormalizeToUtc(DateTime? value, TimeZoneInfo timeZone)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var edge = value.Value;
        if (edge.Kind == DateTimeKind.Unspecified)
        {
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(edge, DateTimeKind.Unspecified), timeZone);
        }

        return edge.ToUniversalTime().SpecifyUtcKind();
    }

    /// <summary>
    /// Renders a stored UTC instant as the wall-clock value in the given timezone
    /// (with <c>Unspecified</c> kind, i.e. the local face of the instant).
    /// </summary>
    public static DateTime? ToWallClock(DateTime? utcInstant, TimeZoneInfo timeZone)
    {
        if (!utcInstant.HasValue)
        {
            return null;
        }

        var instant = utcInstant.Value;
        if (instant.Kind == DateTimeKind.Unspecified)
        {
            instant = DateTime.SpecifyKind(instant, DateTimeKind.Utc);
        }

        return TimeZoneInfo.ConvertTimeFromUtc(instant.ToUniversalTime(), timeZone);
    }

    /// <summary>
    /// Evaluates a stored (UTC) window against a UTC instant. Edges are compared
    /// as instants: <c>ValidFrom</c> is inclusive, <c>ValidUntil</c> is exclusive.
    /// </summary>
    public static PromoWindowOutcome Evaluate(DateTime? validFromUtc, DateTime? validUntilUtc, DateTime utcNow)
    {
        var now = utcNow.ToUniversalTime();

        if (validFromUtc.HasValue)
        {
            var from = validFromUtc.Value.ToUniversalTime();
            if (now < from)
            {
                return PromoWindowOutcome.NotYetValid;
            }
        }

        if (validUntilUtc.HasValue)
        {
            var until = validUntilUtc.Value.ToUniversalTime();
            if (now >= until)
            {
                return PromoWindowOutcome.Expired;
            }
        }

        return PromoWindowOutcome.WithinWindow;
    }
}

/// <summary>Small <see cref="DateTime"/> kind helper for UTC instant plumbing.</summary>
internal static class PromoDateTimeExtensions
{
    /// <summary>Returns the same instant stamped with <see cref="DateTimeKind.Utc"/>.</summary>
    public static DateTime SpecifyUtcKind(this DateTime value)
        => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
