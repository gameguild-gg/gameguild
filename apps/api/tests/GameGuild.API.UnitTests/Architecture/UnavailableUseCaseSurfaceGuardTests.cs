using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace GameGuild.API.UnitTests.Architecture;

/// <summary>
///     Wiring guard G8 — the planned-but-unavailable 422 surface must be a
///     COMMITTED, ratcheted allowlist. Generalizes
///     <see cref="PlannedAuthorizationSurfaceGuardTests"/> (which pinned the 16
///     authorization management commands of PR #1228) to EVERY route that answers
///     422 via the unavailable-contract mechanism
///     (<c>UseCaseEventContractAttribute.UnavailableReason</c> → throwing
///     <c>UnavailableCommandHandler</c> → <c>DomainException</c> → HTTP 422):
///     <list type="number">
///         <item>every unavailable contract must appear in the committed allowlist
///         (<c>Architecture/planned-unavailable-surface.txt</c>, one line + reason);
///         an unplanned 422 route fails CI until it is either implemented or added
///         to the planned surface with a reviewed reason</item>
///         <item>allowlist entries whose contract regained a real handler (or lost
///         the contract) are STALE and fail, so the surface only shrinks</item>
///         <item>the generated OpenAPI client must not advertise any route that
///         dispatches an unavailable command as callable — the truthfulness rule
///         of #1228/#1203 (an advertised, always-422 endpoint is a lie to every
///         client consumer)</item>
///     </list>
/// </summary>
public sealed class UnavailableUseCaseSurfaceGuardTests
{
    /// <summary>
    ///     Commands that dispatch through the mediator and whose use-case event
    ///     contract declares an <c>UnavailableReason</c> (the planned 422 surface),
    ///     keyed by command full name → declared reason.
    /// </summary>
    private static IReadOnlyDictionary<string, string> CurrentPlannedSurface() =>
        WiringGapSourceTree.LoadProductAssemblies()
            .SelectMany(assembly => assembly.GetCustomAttributes<UseCaseEventContractAttribute>())
            .Where(contract => !string.IsNullOrWhiteSpace(contract.UnavailableReason))
            .GroupBy(contract => contract.CommandType.FullName ?? contract.CommandType.Name)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.First().UnavailableReason!);

    private static string AllowlistPath => Path.Combine(
        WiringGapSourceTree.RepositoryRoot,
        "apps", "api", "tests", "GameGuild.API.UnitTests", "Architecture", "planned-unavailable-surface.txt");

    private static string GeneratedClientPath => Path.Combine(
        WiringGapSourceTree.RepositoryRoot,
        "packages", "infrastructure", "client", "src", "generated", "endpoints.gen.ts");

    private static IReadOnlySet<string> LoadAllowlist()
    {
        var entries = new HashSet<string>(StringComparer.Ordinal);
        foreach (var rawLine in File.ReadAllLines(AllowlistPath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            entries.Add(line.Split('#', 2)[0].Trim());
        }

        return entries;
    }

    [Fact]
    public void EveryUnavailableContract_IsInTheCommittedPlannedSurface()
    {
        var surface = CurrentPlannedSurface();
        var allowlist = LoadAllowlist();

        allowlist.Should().NotBeEmpty("the planned-unavailable surface is non-empty today (issue #1203)");

        var unplanned = surface.Keys
            .Where(command => !allowlist.Contains(command))
            .OrderBy(command => command, StringComparer.Ordinal)
            .ToList();

        unplanned.Should().BeEmpty(
            "these use-case commands answer 422 via the unavailable-contract mechanism but are NOT in " +
            "Architecture/planned-unavailable-surface.txt. An unplanned 422 is exactly the dead-endpoint class of " +
            "#1203: either wire a real handler or add the command to the committed planned surface with a reviewed " +
            "reason. Unplanned:{0}{1}",
            Environment.NewLine,
            string.Join(Environment.NewLine, unplanned));
    }

    [Fact]
    public void EveryAllowlistEntry_StillAnswers422ViaTheUnavailableContract()
    {
        var surface = CurrentPlannedSurface();
        var stale = LoadAllowlist()
            .Where(command => !surface.ContainsKey(command))
            .OrderBy(command => command, StringComparer.Ordinal)
            .ToList();

        stale.Should().BeEmpty(
            "these planned-surface entries no longer declare UnavailableReason — the command regained a real " +
            "handler (or lost its contract), so the entry is stale and must be REMOVED from " +
            "Architecture/planned-unavailable-surface.txt so the planned 422 surface only shrinks. Stale:{0}{1}",
            Environment.NewLine,
            string.Join(Environment.NewLine, stale));
    }

    [Fact]
    public void EveryAllowlistEntry_CarriesAnIssueReferenceAndTheDeclaredReason()
    {
        File.Exists(AllowlistPath).Should().BeTrue(
            $"the planned-unavailable surface allowlist must exist at {AllowlistPath}");

        var surface = CurrentPlannedSurface();

        var thin = File.ReadAllLines(AllowlistPath)
            .Select((line, index) => new { Line = line.Trim(), Number = index + 1 })
            .Where(entry => entry.Line.Length > 0 && !entry.Line.StartsWith("#", StringComparison.Ordinal))
            .Where(entry => !entry.Line.Contains("issue", StringComparison.OrdinalIgnoreCase)
                            || !entry.Line.Contains("#", StringComparison.Ordinal)
                            || entry.Line["#".Length..].Trim().Length < 15)
            .Select(entry => $"line {entry.Number}: {entry.Line}")
            .ToList();

        thin.Should().BeEmpty(
            "every planned-surface entry needs a '# ... issue #N ...' comment a reviewer can evaluate. Thin entries:");

        var mismatchedReasons = File.ReadAllLines(AllowlistPath)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith("#", StringComparison.Ordinal))
            .Select(line => new
            {
                Command = line.Split('#', 2)[0].Trim(),
                Comment = line.Contains('#') ? line[line.IndexOf('#')..].Trim() : string.Empty
            })
            .Where(entry => surface.TryGetValue(entry.Command, out var reason)
                            && !entry.Comment.Contains(reason, StringComparison.Ordinal))
            .Select(entry => $"{entry.Command}: allowlist comment must quote the declared UnavailableReason")
            .ToList();

        mismatchedReasons.Should().BeEmpty(
            "each line's reason must match the UnavailableReason declared on the contract, so reviewers can diff " +
            "intent against mechanism:");
    }

    /// <summary>
    ///     The planned 422 routes must not be advertised as callable by the generated
    ///     OpenAPI client. A route is "planned 422" when a controller action dispatches
    ///     a command whose contract declares UnavailableReason; hidden controllers
    ///     (<c>ApiExplorerSettings(IgnoreApi = true)</c>, the #1228 pattern) never
    ///     reach the document, so their paths are absent by construction — this catches
    ///     the mixed controllers that still leak planned routes into the client.
    /// </summary>
    [Fact]
    public void GeneratedClient_DoesNotAdvertisePlanned422RoutesAsCallable()
    {
        var plannedCommands = CurrentPlannedSurface()
            .Keys
            .Select(fullName => fullName.Split('.')[^1])
            .ToHashSet(StringComparer.Ordinal);

        var advertisedPaths = ReadAdvertisedClientPaths();

        var violations = new List<string>();
        foreach (var (controllerName, route, actions) in FindControllerRoutesDispatchingPlannedCommands(plannedCommands))
        {
            foreach (var (actionTemplate, _) in actions)
            {
                foreach (var expanded in ExpandAdvertisablePath(route, actionTemplate))
                {
                    if (advertisedPaths.Contains(expanded))
                    {
                        violations.Add(WiringGapBaseline.Id(
                            WiringGapBaseline.GuardIds.UnavailableUseCaseSurface,
                            $"client-advertises:{expanded}"));
                    }
                }
            }
        }

        WiringGapBaseline.AssertNoNewGaps(
            WiringGapBaseline.GuardIds.UnavailableUseCaseSurface,
            "the generated OpenAPI client (packages/infrastructure/client/src/generated/endpoints.gen.ts) must not " +
            "advertise a route that dispatches an unavailable (planned 422) command as callable — hide the route " +
            "from the API explorer ([ApiExplorerSettings(IgnoreApi = true)], the #1228 pattern) or wire a real " +
            "handler, then regenerate the client",
            violations);
        WiringGapBaseline.AssertNoStaleEntries(
            WiringGapBaseline.GuardIds.UnavailableUseCaseSurface, violations,
            ownsBaselineEntry: entry => entry.Contains("client-advertises:", StringComparison.Ordinal));
    }

    /// <summary>
    ///     Controller files (under any <c>Controllers/</c> directory) that reference a
    ///     planned command, with the class-level route template and the action-level
    ///     HTTP templates that dispatch them, from a source scan.
    /// </summary>
    private static IEnumerable<(string Controller, string Route, IReadOnlyList<(string Template, string Command)> Actions)>
        FindControllerRoutesDispatchingPlannedCommands(IReadOnlySet<string> plannedCommands)
    {
        foreach (var file in WiringGapSourceTree.SourceFiles)
        {
            var segments = file.Path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!segments.Contains("Controllers", StringComparer.Ordinal))
            {
                continue;
            }

            var text = file.Text;
            var classRoute = Regex.Match(text, @"\[Route\(""(?<template>[^""]+)""\)\]");
            if (!classRoute.Success)
            {
                continue;
            }

            var actions = new List<(string Template, string Command)>();
            var actionMatches = Regex.Matches(text,
                    @"\[Http(?<method>Get|Post|Put|Patch|Delete)(?:\(""(?<template>[^""]*)"")?\)")
                .OfType<Match>()
                .ToList();
            foreach (var command in plannedCommands)
            {
                foreach (var reference in Regex.Matches(text, $@"\b{Regex.Escape(command)}\b").OfType<Match>())
                {
                    // Attribute the reference to the nearest [Http*] action above it.
                    var action = actionMatches
                        .Where(match => match.Index < reference.Index)
                        .OrderByDescending(match => match.Index)
                        .FirstOrDefault();
                    if (action is null)
                    {
                        continue;
                    }

                    actions.Add((action.Groups["template"].Value, command));
                }
            }

            if (actions.Count > 0)
            {
                yield return (
                    Path.GetFileNameWithoutExtension(file.Path),
                    classRoute.Groups["template"].Value,
                    actions.Distinct().ToList());
            }
        }
    }

    /// <summary>
    ///     The client-visible path candidates for a controller route + action template
    ///     pair: <c>{version:apiVersion}</c> expands to the advertised versioned
    ///     prefixes (v1..v3) and remaining parameters keep the client's constraint-less
    ///     <c>{name}</c> form.
    /// </summary>
    private static IEnumerable<string> ExpandAdvertisablePath(string controllerRoute, string actionTemplate)
    {
        var combined = string.IsNullOrEmpty(actionTemplate)
            ? controllerRoute
            : controllerRoute.TrimEnd('/') + "/" + actionTemplate.TrimStart('/');

        var normalized = Regex.Replace(combined, @"\{(?<name>[A-Za-z][A-Za-z0-9]*)(?::[^}]*)?\}", "{${name}}");
        var versionless = Regex.Replace(normalized, @"\{version\}", "v{version}");

        for (var version = 1; version <= 3; version++)
        {
            yield return "/" + versionless.Replace("{version}", $"v{version}", StringComparison.Ordinal);
        }
    }

    private static IReadOnlySet<string> ReadAdvertisedClientPaths()
    {
        File.Exists(GeneratedClientPath).Should().BeTrue(
            "the generated OpenAPI client must be committed and regenerated via the real pipeline " +
            "(scripts/ci/verify-openapi-surface.sh) — never hand-spliced");

        return File.ReadAllLines(GeneratedClientPath)
            .Select(line => Regex.Match(line.Trim(), @"^path:\s*'(?<path>[^']+)'"))
            .Where(match => match.Success)
            .Select(match => match.Groups["path"].Value)
            .ToHashSet(StringComparer.Ordinal);
    }
}
