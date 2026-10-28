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

    /// <summary>
    ///     Master switch for set-based capture (issue #166 remainder): <c>ExecuteUpdate</c>/
    ///     <c>ExecuteDelete</c> (and equivalent single-statement raw <c>UPDATE</c>/<c>DELETE</c> SQL)
    ///     produce before/after audit evidence. Defaults to <c>true</c> and is additionally gated
    ///     by <see cref="Enabled"/>; disabling either switch stops set-based capture only —
    ///     tracked-entity capture follows <see cref="Enabled"/> alone.
    /// </summary>
    public bool CaptureSetBasedChanges { get; set; } = true;

    /// <summary>
    ///     Maximum number of affected rows captured per set-based statement. Statements that
    ///     match more rows capture the first rows up to this bound and log a warning naming the
    ///     truncation, so a runaway bulk mutation cannot grow audit storage unboundedly.
    /// </summary>
    public int MaxSetBasedCapturedRows { get; set; } = 5_000;
}
