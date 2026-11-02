using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace GameGuild.API.UnitTests.Architecture;

/// <summary>
///     Wiring guard G2 — entity configurations must be MATERIALIZED in the compiled
///     model. Every <c>IEntityTypeConfiguration&lt;TEntity&gt;</c> class in the product
///     sources must have its entity present in a checked-in EF model snapshot, and a
///     module that declares configurations must actually be referenced by the host
///     (its assembly in the host output). Prevents the "complete configuration, no
///     table" class of gap (inspiring bug: issue #1158 — Finance.Ledgers,
///     Social.Ratings and Gamification.Achievements had full EF configurations but
///     the host never applied them, so no tables existed; caught only by manual audit).
///     The scan is deliberately SOURCE-based (not reflection): an unreferenced module
///     is invisible to reflection over loaded assemblies — that is exactly the gap.
/// </summary>
public sealed partial class TableMaterializationGuardTests
{
    private const string ConfigurationInterface = "IEntityTypeConfiguration";

    [Fact]
    public void EveryEntityTypeConfiguration_MaterializesInTheCompiledModel()
    {
        var snapshotEntities = LoadSnapshotEntityNames();
        snapshotEntities.Should().HaveCountGreaterThan(400, "the compiled model is large; a tiny count means the snapshot scan is broken");

        var hostAssemblies = Directory.EnumerateFiles(AppContext.BaseDirectory, "GameGuild*.dll")
            .Select(Path.GetFileNameWithoutExtension)
            .ToHashSet(StringComparer.Ordinal);

        var violations = new List<string>();
        var configCount = 0;

        foreach (var (path, text) in WiringGapSourceTree.SourceFiles)
        {
            var declarations = FindEntityConfigurationDeclarations(text);
            if (declarations.Count == 0)
            {
                continue;
            }

            var scope = ScopeLabelFor(path);
            foreach (var (configClass, entityTypeName) in declarations)
            {
                configCount++;
                if (IsMaterialized(entityTypeName, text, path, snapshotEntities))
                {
                    continue;
                }

                violations.Add(WiringGapBaseline.Id(
                    WiringGapBaseline.GuardIds.TableMaterialization, $"{scope}|{configClass}|{entityTypeName}"));
            }

            // A module that declares entity configurations but is not referenced by
            // the host project never even loads — the #1158 signature. Assembly
            // presence in the host output is the cheapest complete signal.
            var moduleAssembly = ModuleAssemblyNameFor(path);
            if (moduleAssembly is not null && !hostAssemblies.Contains(moduleAssembly))
            {
                violations.Add(WiringGapBaseline.Id(
                    WiringGapBaseline.GuardIds.TableMaterialization, $"unreferenced-module|{moduleAssembly}"));
            }
        }

        configCount.Should().BeGreaterThan(150, "the entity-configuration scan is source-based; a small count means the declaration scan is broken");

        WiringGapBaseline.AssertNoNewGaps(
            WiringGapBaseline.GuardIds.TableMaterialization,
            "every IEntityTypeConfiguration<TEntity> must map to an entity in a checked-in EF model snapshot " +
            "(regenerate migrations after adding configurations) and its module assembly must be referenced by the host",
            violations);
        WiringGapBaseline.AssertNoStaleEntries(
            WiringGapBaseline.GuardIds.TableMaterialization, violations);
    }

    /// <summary>
    ///     Extracts <c>(class, TEntity textual argument)</c> pairs for every
    ///     <c>IEntityTypeConfiguration&lt;T&gt;</c> declaration in a file.
    /// </summary>
    internal static IReadOnlyList<(string ConfigClass, string EntityTypeName)> FindEntityConfigurationDeclarations(string text)
    {
        var results = new List<(string, string)>();
        if (!text.Contains(ConfigurationInterface, StringComparison.Ordinal))
        {
            return results;
        }

        foreach (var match in DeclarationRegex().Matches(text).OfType<Match>())
        {
            var entityTypeName = match.Groups[2].Value
                .Replace("global::", string.Empty, StringComparison.Ordinal)
                .Trim();
            results.Add((match.Groups[1].Value, entityTypeName));
        }

        return results;
    }

    [GeneratedRegex(
        @"(?:sealed\s+|abstract\s+|internal\s+|public\s+|partial\s+|static\s+)*(?:class|record)\s+(\w+)\s*:[^{]*?IEntityTypeConfiguration\s*<\s*([\w.]+)\s*>")]
    private static partial Regex DeclarationRegex();

    /// <summary>
    ///     Resolves the textual TEntity argument against the snapshot entity set.
    ///     Name resolution walks the declaring file's namespace hierarchy and its
    ///     using-aliases, mirroring how C# would resolve the same reference, and
    ///     additionally accepts a unique short-name match so a cross-module
    ///     reference never produces a false gap.
    /// </summary>
    internal static bool IsMaterialized(
        string entityTypeName, string declaringFileText, string declaringFilePath, IReadOnlySet<string> snapshotEntities)
    {
        var resolved = ResolveThroughAliases(entityTypeName, declaringFileText);

        if (resolved.StartsWith("GameGuild.", StringComparison.Ordinal) && snapshotEntities.Contains(resolved))
        {
            return true;
        }

        if (!resolved.StartsWith("GameGuild.", StringComparison.Ordinal))
        {
            var namespaceMatch = NamespaceRegex().Match(declaringFileText);
            if (namespaceMatch.Success)
            {
                var parts = namespaceMatch.Groups[1].Value.Split('.');
                for (var depth = parts.Length; depth >= 0; depth--)
                {
                    var candidate = string.Join(".", parts.Take(depth)) + "." + resolved;
                    if (snapshotEntities.Contains(candidate))
                    {
                        return true;
                    }
                }
            }
        }

        // Unique short-name fallback: qualified through another module's namespace.
        var shortName = resolved.Split('.')[^1];
        var shortMatches = snapshotEntities.Where(entity => entity.EndsWith("." + shortName, StringComparison.Ordinal)).ToList();
        return shortMatches.Count == 1;
    }

    [GeneratedRegex(@"^\s*namespace\s+([\w.]+)", RegexOptions.Multiline)]
    private static partial Regex NamespaceRegex();

    private static string ResolveThroughAliases(string entityTypeName, string declaringFileText)
    {
        var firstSegment = entityTypeName.Split('.')[0];
        foreach (var match in UsingAliasRegex().Matches(declaringFileText).OfType<Match>())
        {
            if (match.Groups[1].Value == firstSegment)
            {
                return match.Groups[2].Value + entityTypeName[firstSegment.Length..];
            }
        }

        return entityTypeName;
    }

    [GeneratedRegex(@"^\s*using\s+(\w+)\s*=\s*([\w.]+)\s*;", RegexOptions.Multiline)]
    private static partial Regex UsingAliasRegex();

    /// <summary>
    ///     Entity type names as checked in to every EF model snapshot under the
    ///     product source tree (generated code declares <c>b.Entity("Full.Name", ...)</c>).
    /// </summary>
    internal static IReadOnlySet<string> LoadSnapshotEntityNames()
    {
        var entities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (path, text) in WiringGapSourceTree.SourceFiles)
        {
            if (!Path.GetFileName(path).EndsWith("ModelSnapshot.cs", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var match in SnapshotEntityRegex().Matches(text).OfType<Match>())
            {
                entities.Add(match.Groups[1].Value);
            }
        }

        return entities;
    }

    [GeneratedRegex(@"\.Entity\(""(?<name>[^""]+)""")]
    private static partial Regex SnapshotEntityRegex();

    private static string ScopeLabelFor(string path)
    {
        var relative = Path.GetRelativePath(WiringGapSourceTree.AppsApiSourcePath, path);
        var segments = relative.Split(Path.DirectorySeparatorChar);
        if (segments.Length >= 2 && segments[0] == "Modules")
        {
            return segments[1]; // e.g. GameGuild.Finance.Ledgers
        }

        return segments[0]; // GameGuild.API / GameGuild.ArchitectureAnalyzers
    }

    private static string? ModuleAssemblyNameFor(string path)
    {
        var relative = Path.GetRelativePath(WiringGapSourceTree.AppsApiSourcePath, path);
        var segments = relative.Split(Path.DirectorySeparatorChar);
        // Any source file inside apps/api/Source/Modules/<Module>/ belongs to that
        // module's assembly; host-project files return null (the host is always loaded).
        return segments.Length >= 2 && segments[0] == "Modules" && segments[1].StartsWith("GameGuild.", StringComparison.Ordinal)
            ? segments[1]
            : null;
    }
}
