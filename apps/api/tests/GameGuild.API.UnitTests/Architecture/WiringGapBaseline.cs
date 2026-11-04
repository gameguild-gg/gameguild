using FluentAssertions;

namespace GameGuild.API.UnitTests.Architecture;

/// <summary>
///     Ratchet baseline for the wiring-gap pipeline guards (G1-G9; G8 keeps its
///     own dedicated allowlist, see
///     <see cref="UnavailableUseCaseSurfaceGuardTests"/>).
///     Each guard reports violations with a stable identifier of the form
///     <c>G&lt;n&gt;|&lt;item&gt;</c>. Violations listed in
///     <c>Architecture/wiring-gap-baseline.txt</c> are today's KNOWN gaps —
///     every entry must carry the tracking issue number it maps to. The guards
///     fail only for gaps NOT in the baseline (new wiring regressions), and
///     <see cref="WiringGapBaselineTests"/> fails for baseline entries that no
///     longer reproduce (fixed gaps must have their line removed so the
///     ratchet never loosens).
/// </summary>
/// <remarks>
///     This mirrors the host-side allowlist ratchet pattern of
///     <c>GameGuild.API.Security.AnonymousEndpointRegistry</c>
///     (see ControllerAuthorizationArchitectureTests).
/// </remarks>
public static class WiringGapBaseline
{
    public static IReadOnlySet<string> KnownGaps { get; } = LoadBaseline();

    public static string BaselinePath => Path.Combine(
        WiringGapSourceTree.RepositoryRoot,
        "apps", "api", "tests", "GameGuild.API.UnitTests", "Architecture", "wiring-gap-baseline.txt");

    private static IReadOnlySet<string> LoadBaseline()
    {
        var path = BaselinePath;
        var entries = new HashSet<string>(StringComparer.Ordinal);

        if (!File.Exists(path))
        {
            return entries;
        }

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue; // comments and blanks
            }

            // "<violation-id>  # <issue + justification comment>"
            var id = line.Split('#', 2)[0].Trim();
            if (id.Length > 0)
            {
                entries.Add(id);
            }
        }

        return entries;
    }

    /// <summary>
    ///     Asserts that every <paramref name="violations"/> entry is a known
    ///     (baselined) gap. New, unlisted gaps fail the guard.
    /// </summary>
    public static void AssertNoNewGaps(string guardId, string ruleDescription, IEnumerable<string> violations)
    {
        var violationsList = violations.Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToList();
        var newGaps = violationsList
            .Where(violation => !KnownGaps.Contains(violation))
            .ToList();

        newGaps.Should().BeEmpty(
            "{0}: {1}. These violations are NOT in Architecture/wiring-gap-baseline.txt — " +
            "either wire the missing piece or, if it is a reviewed known gap tracked by an issue, " +
            "add it to the baseline with the issue number. New violations:{2}{3}",
            guardId,
            ruleDescription,
            Environment.NewLine,
            string.Join(Environment.NewLine, newGaps));
    }

    /// <summary>
    ///     Asserts that every baseline entry owned by the calling test still
    ///     reproduces, so fixed gaps get their baseline line removed (the ratchet
    ///     only tightens). When several tests share one guard id, pass
    ///     <paramref name="ownsBaselineEntry"/> so each test only checks its own
    ///     entries.
    /// </summary>
    public static void AssertNoStaleEntries(
        string guardId,
        IEnumerable<string> currentViolations,
        Func<string, bool>? ownsBaselineEntry = null)
    {
        var prefix = guardId + "|";
        var current = currentViolations.Distinct(StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
        var stale = KnownGaps
            .Where(entry => entry.StartsWith(prefix, StringComparison.Ordinal))
            .Where(entry => ownsBaselineEntry?.Invoke(entry) ?? true)
            .Where(entry => !current.Contains(entry))
            .OrderBy(entry => entry, StringComparer.Ordinal)
            .ToList();

        stale.Should().BeEmpty(
            "{0}: baseline entries that no longer reproduce must be removed from " +
            "Architecture/wiring-gap-baseline.txt (fixed gaps tighten the ratchet). Stale:{1}{2}",
            guardId,
            Environment.NewLine,
            string.Join(Environment.NewLine, stale));
    }

    /// <summary>
    ///     Guard identifiers as used in baseline entries ("<c>G2|...</c>" keys).
    /// </summary>
    public static class GuardIds
    {
        public const string CommandRegistration = "G1";
        public const string TableMaterialization = "G2";
        public const string HandlerLifetime = "G3";
        public const string EventProducer = "G4";
        public const string OptionsBinding = "G5";
        public const string StubMiddleware = "G6";
        public const string RequestHandlerResolution = "G7";
        public const string UnavailableUseCaseSurface = "G8";
        public const string RouteSmoke = "E9";
    }

    /// <summary>
    ///     Renders a stable violation id: "<c>&lt;guard&gt;|&lt;item&gt;</c>".
    /// </summary>
    public static string Id(string guardId, string item) => guardId + "|" + item;
}
