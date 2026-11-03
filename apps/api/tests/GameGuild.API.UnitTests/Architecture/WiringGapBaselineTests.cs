using FluentAssertions;
using Xunit;

namespace GameGuild.API.UnitTests.Architecture;

/// <summary>
///     Ratchet hygiene for <c>wiring-gap-baseline.txt</c>: every entry must map to a
///     tracking issue (with a human-evaluable justification), and the file must only
///     contain well-formed guard entries. Stale (fixed) entries are reported by each
///     guard's own AssertNoStaleEntries call.
/// </summary>
public sealed class WiringGapBaselineTests
{
    [Fact]
    public void EveryBaselineEntry_CarriesAnIssueReferenceAndJustification()
    {
        var path = BaselinePath;
        File.Exists(path).Should().BeTrue($"the wiring-gap baseline must exist at {path}");

        var thin = File.ReadAllLines(path)
            .Select((line, index) => new { Line = line.Trim(), Number = index + 1 })
            .Where(entry => entry.Line.Length > 0 && !entry.Line.StartsWith("#", StringComparison.Ordinal))
            .Where(entry => !entry.Line.Contains("#", StringComparison.Ordinal)
                            || entry.Line["#".Length..].Trim().Length < 15
                            || !entry.Line.Contains("issue", StringComparison.OrdinalIgnoreCase))
            .Select(entry => $"line {entry.Number}: {entry.Line}")
            .ToList();

        thin.Should().BeEmpty(
            "every baseline entry needs a '# ... issue #N ...' comment a reviewer can evaluate. Thin entries:");
    }

    [Fact]
    public void EveryBaselineEntry_UsesAKnownGuardPrefix()
    {
        var validPrefixes = new[]
        {
            WiringGapBaseline.GuardIds.CommandRegistration + "|",
            WiringGapBaseline.GuardIds.TableMaterialization + "|",
            WiringGapBaseline.GuardIds.HandlerLifetime + "|",
            WiringGapBaseline.GuardIds.EventProducer + "|",
            WiringGapBaseline.GuardIds.OptionsBinding + "|",
            WiringGapBaseline.GuardIds.StubMiddleware + "|",
        };

        var malformed = File.ReadAllLines(BaselinePath)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith("#", StringComparison.Ordinal))
            .Where(line => validPrefixes.All(prefix => !line.StartsWith(prefix, StringComparison.Ordinal)))
            .ToList();

        malformed.Should().BeEmpty("baseline entries must start with a known guard id (G1|..G6|). Malformed:");
    }

    private static string BaselinePath => WiringGapBaseline.BaselinePath;
}
