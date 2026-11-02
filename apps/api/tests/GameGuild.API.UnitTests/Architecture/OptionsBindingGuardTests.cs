using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace GameGuild.API.UnitTests.Architecture;

/// <summary>
///     Wiring guard G5 — options classes must be wired to configuration. An options
///     class declaring <c>SectionName</c> that nothing ever binds is dead machinery:
///     features read defaults forever while operators believe the section works.
///     A class passes when ANY of these static wiring signals exists:
///     (a) another source file references <c>&lt;Type&gt;.SectionName</c> (the module's
///         own <c>Configure&lt;T&gt;(configuration.GetSection(T.SectionName))</c> or an
///         OptionsBuilder), or binds <c>Configure&lt;T&gt;</c>/<c>AddOptions&lt;T&gt;</c>;
///     (b) the section is present in a checked-in <c>appsettings*.json</c>;
///     (c) the section is provided by an environment binding
///         (<c>Section__Path</c>) in the compose/deploy surface.
///     Unwired classes must be baselined against a filed issue.
/// </summary>
public sealed class OptionsBindingGuardTests
{
    [Fact]
    public void EveryOptionsClassWithASectionName_IsWiredToConfiguration()
    {
        var optionsTypes = WiringGapSourceTree.LoadProductAssemblies()
            .SelectMany(WiringGapSourceTree.GetLoadableTypes)
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
                           && DeclaresSectionName(type))
            .Distinct()
            .ToList();

        optionsTypes.Should().HaveCountGreaterThan(60, "the options surface is large; a tiny count means the reflection scan is broken");

        var jsonSections = LoadAppSettingsSections();
        var genericBindingArgs = CollectGenericBindingArguments();
        var sectionNameReferences = CollectSectionNameReferences();

        var unwired = new List<string>();
        foreach (var optionsType in optionsTypes)
        {
            var section = GetSectionNameValue(optionsType);
            section.Should().NotBeNullOrEmpty($"{optionsType.FullName} must declare a non-empty SectionName");
            var sectionName = section!;

            var wired = jsonSections.Contains(sectionName)
                        || EnvironmentBindingsContain(sectionName.Replace(":", "__", StringComparison.Ordinal))
                        || genericBindingArgs.Contains(optionsType.Name)
                        || sectionNameReferences.ContainsKey(optionsType.Name);
            if (!wired)
            {
                unwired.Add(WiringGapBaseline.Id(
                    WiringGapBaseline.GuardIds.OptionsBinding, optionsType.FullName ?? optionsType.Name));
            }
        }

        WiringGapBaseline.AssertNoNewGaps(
            WiringGapBaseline.GuardIds.OptionsBinding,
            "an options class with a SectionName must be wired: bound in DI (Configure<T>/AddOptions<T>/T.SectionName " +
            "reference), present in a checked-in appsettings*.json, or provided by a Section__Path environment binding " +
            "in compose/deploy — otherwise baseline it against a filed issue",
            unwired);
        WiringGapBaseline.AssertNoStaleEntries(
            WiringGapBaseline.GuardIds.OptionsBinding, unwired);
    }

    internal static bool DeclaresSectionName(Type type) =>
        type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Any(field => field.FieldType == typeof(string) && field.Name == "SectionName")
        || type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Any(property => property.GetMethod is { IsStatic: true }
                && property.PropertyType == typeof(string) && property.Name == "SectionName");

    internal static string? GetSectionNameValue(Type type)
    {
        var field = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .FirstOrDefault(f => f.FieldType == typeof(string) && f.Name == "SectionName");
        if (field is not null)
        {
            return field.IsLiteral
                ? (string?)field.GetRawConstantValue()
                : (string?)field.GetValue(null); // static readonly simple string constants
        }

        var property = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .FirstOrDefault(p => p.PropertyType == typeof(string) && p.Name == "SectionName");
        return (string?)property?.GetValue(null);
    }

    /// <summary>
    ///     Arguments of every <c>Configure&lt;T&gt;</c>/<c>AddOptions&lt;T&gt;</c> generic use,
    ///     normalized to the trailing type name (qualified usages included).
    /// </summary>
    private static IReadOnlySet<string> CollectGenericBindingArguments()
    {
        var arguments = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (_, text) in WiringGapSourceTree.SourceFiles)
        {
            var index = 0;
            while (true)
            {
                index = text.IndexOf("Configure<", index, StringComparison.Ordinal);
                if (index < 0)
                {
                    break;
                }

                CollectArgument(text, index + "Configure<".Length, arguments);
                index += 10;
            }

            index = 0;
            while (true)
            {
                index = text.IndexOf("AddOptions<", index, StringComparison.Ordinal);
                if (index < 0)
                {
                    break;
                }

                CollectArgument(text, index + "AddOptions<".Length, arguments);
                index += 11;
            }
        }

        return arguments;
    }

    private static void CollectArgument(string text, int start, ISet<string> arguments)
    {
        var end = text.IndexOf('>', start);
        if (end <= start || end - start > 200)
        {
            return; // not a simple type argument
        }

        var argument = text[start..end].Trim();
        var lastDot = argument.LastIndexOf('.');
        arguments.Add(lastDot < 0 ? argument : argument[(lastDot + 1)..]);
    }

    /// <summary>
    ///     Options type names referenced as <c>&lt;Type&gt;.SectionName</c> somewhere in
    ///     product sources (module DI code referencing its options' section key).
    /// </summary>
    private static IReadOnlyDictionary<string, object> CollectSectionNameReferences()
    {
        var references = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var (_, text) in WiringGapSourceTree.SourceFiles)
        {
            var index = 0;
            while (true)
            {
                index = text.IndexOf(".SectionName", index, StringComparison.Ordinal);
                if (index < 0)
                {
                    break;
                }

                var nameStart = index - 1;
                while (nameStart >= 0 && (char.IsLetterOrDigit(text[nameStart]) || text[nameStart] == '_'))
                {
                    nameStart--;
                }

                if (index - nameStart - 1 > 0 && index - nameStart - 1 <= 100)
                {
                    references[text[(nameStart + 1)..index]] = Boxed;
                }

                index += 12;
            }
        }

        return references;
    }

    private static readonly object Boxed = new();

    private static IReadOnlySet<string> LoadAppSettingsSections()
    {
        var sections = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in WiringGapSourceTree.AppSettingsFiles)
        {
            using var document = JsonDocument.Parse(
                File.ReadAllText(path),
                new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            CollectPaths(document.RootElement, [], sections);
        }

        return sections;
    }

    private static void CollectPaths(JsonElement element, IEnumerable<string> prefix, ISet<string> sections)
    {
        foreach (var property in element.EnumerateObject())
        {
            var path = string.Join(":", prefix.Append(property.Name));
            sections.Add(path);
            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                CollectPaths(property.Value, prefix.Append(property.Name), sections);
            }
        }
    }

    /// <summary>
    ///     Whether the compose/deploy surface carries a <c>Section__Path</c>
    ///     environment binding (double-underscore .NET configuration keys).
    /// </summary>
    private static bool EnvironmentBindingsContain(string environmentKey) =>
        WiringGapSourceTree.ConfigTextFiles.Any(file =>
            file.Text.Contains(environmentKey, StringComparison.Ordinal));
}
