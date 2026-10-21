namespace GameGuild.API.Database;

/// <summary>
///     Service-level configuration for universal entity-change audit capture (issue #166).
///     Capture is fail-closed by default: <see cref="Enabled"/> defaults to <c>true</c>, so every
///     tracked entity modification is recorded unless a deployment explicitly opts out. Exclusions
///     are an additional performance/config guard for entity types that must never be captured;
///     audit-store and infrastructure entities are always excluded regardless of configuration.
/// </summary>
public sealed class EntityChangeAuditOptions
{
    public const string SectionName = "Audit:EntityChangeCapture";

    /// <summary>
    ///     Master switch. Defaults to <c>true</c> (opt-out): universal capture must be on unless it is
    ///     deliberately disabled, so a missing configuration section can never silently drop coverage.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     Entity types excluded from universal capture, by simple CLR name or full name
    ///     (case-insensitive). Audit-store entities are excluded independently and cannot be
    ///     re-included through this list.
    /// </summary>
    public HashSet<string> ExcludedEntityTypes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
