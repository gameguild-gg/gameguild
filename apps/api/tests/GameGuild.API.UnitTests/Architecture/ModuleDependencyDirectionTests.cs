using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace GameGuild.API.UnitTests.Architecture;

/// <summary>
///     Architecture rule T2: platform modules must stay domain-free. Platform modules
///     (identity, resources, shared kernel, assets, features, ledgers, content pages) must
///     not reference domain modules — neither via project references nor via namespace
///     usages. The reverse direction (domain referencing platform) is allowed and expected.
/// </summary>
/// <remarks>
///     <para>
///         The domain set is DERIVED: every module directory under <c>Source/Modules</c>
///         that is not part of the platform set counts as a domain module. There is no
///         per-product hardcoding, so this file mirrors verbatim across products while
///         each product's domain modules are picked up automatically.
///     </para>
///     <para>
///         Namespace matches use the LONGEST domain-module name contained in the file, so
///         an edge is reported once (e.g. a usage of
///         <c>GameGuild.Commerce.Subscriptions</c> is attributed to that module, not to
///         its <c>GameGuild.Commerce</c> prefix).
///     </para>
/// </remarks>
public sealed class ModuleDependencyDirectionTests
{
    /// <summary>
    ///     Platform (domain-free) modules — mirrored byte-for-byte across products.
    /// </summary>
    private static readonly string[] PlatformModules =
    [
        "GameGuild.SharedKernel",
        "GameGuild.Identity.Authentication",
        "GameGuild.Identity.Authorization",
        "GameGuild.Identity.Context",
        "GameGuild.Identity.Tenants",
        "GameGuild.Identity.Users",
        "GameGuild.Resources",
        "GameGuild.Resources.Contents",
        "GameGuild.Assets",
        "GameGuild.Features",
        "GameGuild.Finance.Ledgers",
        "GameGuild.Content.Pages",
    ];

    /// <summary>
    ///     Reviewed, time-boxed exceptions. Each entry is a platform module that still
    ///     references another module; every entry must carry a removal plan. Any
    ///     dependency-direction violation NOT in this list fails the tests.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> KnownDebt =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["GameGuild.Assets -> GameGuild.Localization"] =
                "Asset metadata exposes localized display names. Removal plan: extract a platform-level " +
                "localization abstraction into the shared kernel and let the host bind it to the localization module.",
            ["GameGuild.Features -> GameGuild.Commerce.Subscriptions"] =
                "Subscription-gated capabilities: CapabilityService/SubscriptionFeatureService/UsageEnforcementMiddleware " +
                "bind directly to subscription entities. Removal plan: extract a platform capability-abstraction and move " +
                "the subscription-backed implementations into the commerce side or the host.",
            ["GameGuild.Finance.Ledgers -> GameGuild.Finance.Contracts"] =
                "Ledger posting publishes integration events shaped by the finance contracts. Removal plan: host-compose " +
                "the publishing adapters or move the shared event shapes onto a platform contracts surface.",
            ["GameGuild.Identity.Authentication -> GameGuild.Notifications"] =
                "Authentication events (welcome mail, verification, magic links, password resets) dispatch through the " +
                "notifications module. Removal plan: publish platform events and subscribe from the notifications side " +
                "(or the host) instead of referencing it directly.",
            ["GameGuild.Resources -> GameGuild.Finance.Contracts"] =
                "Cost accounting emits finance-contract events. Removal plan: host-compose an event bridge so the " +
                "resources module stays free of finance namespaces.",
            ["GameGuild.Resources -> GameGuild.Notifications"] =
                "Quota-exceeded alerts dispatch through the notifications module. Removal plan: invert through a " +
                "platform notification abstraction registered by the host.",
        };

    [Fact]
    public void PlatformModules_DoNotProjectReferenceDomainModules()
    {
        var modulesRoot = FindModulesRoot();
        var violations = new List<string>();

        foreach (var module in PlatformModules)
        {
            var csproj = Path.Combine(modulesRoot, module, $"{module}.csproj");
            if (!File.Exists(csproj))
            {
                // Module not present in this product — nothing to check.
                continue;
            }

            var referenced = Regex.Matches(File.ReadAllText(csproj), @"ProjectReference[^>]*Include=""([^""]+)""")
                .Select(match => Path.GetFileNameWithoutExtension(match.Groups[1].Value))
                .ToList();

            foreach (var reference in referenced.Where(reference => IsDomainModule(modulesRoot, reference)))
            {
                violations.Add($"{module} -> {reference}");
            }
        }

        violations
            .Where(violation => !KnownDebt.ContainsKey(violation))
            .Should().BeEmpty(
                "platform modules must not reference domain modules (reverse direction is allowed). Violations: {0}",
                string.Join("; ", violations));
    }

    [Fact]
    public void PlatformModules_DoNotUseDomainModuleNamespaces()
    {
        var modulesRoot = FindModulesRoot();
        var violations = new List<string>();

        foreach (var module in PlatformModules)
        {
            var moduleDir = Path.Combine(modulesRoot, module);
            if (!Directory.Exists(moduleDir))
            {
                continue;
            }

            foreach (var sourceFile in Directory.EnumerateFiles(moduleDir, "*.cs", SearchOption.AllDirectories))
            {
                if (sourceFile.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
                    sourceFile.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                {
                    continue;
                }

                var domain = LongestDomainModuleReferenced(modulesRoot, File.ReadAllText(sourceFile));
                if (domain is not null)
                {
                    violations.Add($"{module} -> {domain} ({Path.GetFileName(sourceFile)})");
                }
            }
        }

        var unlisted = violations
            .Select(violation => violation.Split(" (")[0])
            .Where(violation => !KnownDebt.ContainsKey(violation))
            .Distinct()
            .ToList();

        unlisted.Should().BeEmpty(
            "platform module source must not reference domain module namespaces. Violations: {0}",
            string.Join("; ", unlisted));
    }

    [Fact]
    public void KnownDebt_Entries_ReferToRealModules()
    {
        var modulesRoot = FindModulesRoot();

        foreach (var entry in KnownDebt.Keys)
        {
            var parts = entry.Split(" -> ");
            parts.Length.Should().Be(2, "debt entries must have the form 'Platform -> Domain'");
            Directory.Exists(Path.Combine(modulesRoot, parts[0])).Should().BeTrue($"module {parts[0]} should exist");
            Directory.Exists(Path.Combine(modulesRoot, parts[1])).Should().BeTrue($"module {parts[1]} should exist");
            IsDomainModule(modulesRoot, parts[1]).Should().BeTrue(
                $"debt target {parts[1]} must be a domain module (not a platform module)");
        }
    }

    /// <summary>
    ///     A module is a DOMAIN module when it exists under <c>Source/Modules</c> and is not
    ///     part of the platform set. The set is derived per product — no hardcoding.
    /// </summary>
    private static bool IsDomainModule(string modulesRoot, string moduleName) =>
        !PlatformModules.Contains(moduleName)
        && Directory.Exists(Path.Combine(modulesRoot, moduleName));

    /// <summary>
    ///     Returns the longest domain-module namespace actually referenced by the source
    ///     text (longest first, so prefix modules do not shadow their more specific ones).
    /// </summary>
    private static string? LongestDomainModuleReferenced(string modulesRoot, string content) =>
        Directory.GetDirectories(modulesRoot)
            .Select(path => Path.GetFileName(path))
            .Where(name => IsDomainModule(modulesRoot, name))
            .OrderByDescending(name => name.Length)
            .FirstOrDefault(content.Contains);

    /// <summary>
    ///     Walks up from the test output directory to the directory that contains the
    ///     module sources (<c>Source/Modules</c>).
    /// </summary>
    private static string FindModulesRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "Source", "Modules");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate Source/Modules above {AppContext.BaseDirectory}");
    }
}
