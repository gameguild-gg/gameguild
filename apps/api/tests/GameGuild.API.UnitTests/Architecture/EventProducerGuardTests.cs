using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Notifications;
using Xunit;

namespace GameGuild.API.UnitTests.Architecture;

/// <summary>
///     Wiring guard G4 — dead domain events and notification kinds. A domain event
///     type (or <see cref="NotificationType"/> member) that no code ever raises is
///     machinery without a producer: consumers accumulate, tests cover phantoms and
///     real flows silently miss the event. Every concrete
///     <see cref="DomainEvent"/> subclass and every <see cref="NotificationType"/>
///     member must have at least one producing call site (a <c>new XEvent(...)</c>
///     construction / <c>NotificationType.X</c> reference outside its own
///     declaration) — or be an explicit baseline entry mapped to a filed issue.
///     Inspiring bug: issue #1182 — UserCreatedEvent had exactly one producer
///     (local sign-up) while every other provisioning path that should raise it
///     did not; an orphan check keeps this whole class of wiring visible.
/// </summary>
public sealed class EventProducerGuardTests
{
    [Fact]
    public void EveryConcreteDomainEvent_HasAProducingCallSite()
    {
        var eventTypes = WiringGapSourceTree.LoadProductAssemblies()
            .SelectMany(WiringGapSourceTree.GetLoadableTypes)
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
                           && typeof(DomainEvent).IsAssignableFrom(type))
            .Distinct()
            .ToList();

        eventTypes.Should().HaveCountGreaterThan(10, "the domain event surface is non-trivial; a tiny count means the reflection scan is broken");

        var constructorCallCounts = CountNameConstructorCalls(eventTypes.Select(type => type.Name).ToHashSet(StringComparer.Ordinal));
        var orphans = new List<string>();
        foreach (var eventType in eventTypes)
        {
            var filesWithConstruction = constructorCallCounts.TryGetValue(eventType.Name, out var constructionFiles)
                ? constructionFiles
                : [];
            var (declarationFiles, productionFiles) = SplitDeclarationAndProductionFiles(
                eventType.Name, filesWithConstruction);
            if (declarationFiles.Count == 0)
            {
                // Event declared in generated/embedded code with no checked-in source:
                // conservative false negative, not a guard failure.
                continue;
            }

            if (productionFiles.Count == 0)
            {
                orphans.Add(WiringGapBaseline.Id(
                    WiringGapBaseline.GuardIds.EventProducer, eventType.FullName ?? eventType.Name));
            }
        }

        WiringGapBaseline.AssertNoNewGaps(
            WiringGapBaseline.GuardIds.EventProducer,
            "every concrete DomainEvent subclass must have at least one 'new XEvent(...)' call site outside its " +
            "own declaration file, or a baseline entry mapped to a filed issue",
            orphans);
        WiringGapBaseline.AssertNoStaleEntries(
            WiringGapBaseline.GuardIds.EventProducer,
            orphans,
            entry => !entry.Contains("NotificationType.", StringComparison.Ordinal));
    }

    [Fact]
    public void EveryNotificationTypeMember_IsUsedByProducingCode()
    {
        var members = Enum.GetNames<NotificationType>();
        members.Should().HaveCountGreaterThan(10, "the notification kind surface is non-trivial; a tiny count means the enum scan is broken");

        var usageCounts = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var declarationFiles = WiringGapSourceTree.SourceFiles
            .Where(file => Path.GetFileName(file.Path) == "Notification.cs"
                           && file.Path.Contains("GameGuild.Notifications", StringComparison.Ordinal))
            .Select(file => file.Path)
            .ToHashSet(StringComparer.Ordinal);
        declarationFiles.Should().NotBeEmpty("the NotificationType enum declaration must be locatable in the source tree");

        foreach (var member in members)
        {
            usageCounts[member] = [];
        }

        foreach (var (path, text) in WiringGapSourceTree.SourceFiles)
        {
            if (declarationFiles.Contains(path))
            {
                continue;
            }

            foreach (var member in members)
            {
                if (text.Contains("NotificationType." + member, StringComparison.Ordinal))
                {
                    usageCounts[member].Add(path);
                }
            }
        }

        var orphans = members
            .Where(member => usageCounts[member].Count == 0)
            .Select(member => WiringGapBaseline.Id(
                WiringGapBaseline.GuardIds.EventProducer, $"NotificationType.{member}"))
            .ToList();

        WiringGapBaseline.AssertNoNewGaps(
            WiringGapBaseline.GuardIds.EventProducer,
            "every NotificationType member must be referenced by at least one producing/consuming call site " +
            "(NotificationType.<member>) outside the enum declaration, or a baseline entry mapped to a filed issue",
            orphans);
        WiringGapBaseline.AssertNoStaleEntries(
            WiringGapBaseline.GuardIds.EventProducer,
            orphans,
            entry => entry.Contains("NotificationType.", StringComparison.Ordinal));
    }

    /// <summary>
    ///     Counts <c>new Name(...)</c> constructor call sites per event short name,
    ///     returning file names (not line details) for the declaration/production split.
    /// </summary>
    private static IReadOnlyDictionary<string, List<string>> CountNameConstructorCalls(IReadOnlySet<string> names)
    {
        var callSites = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var name in names)
        {
            callSites[name] = [];
        }

        foreach (var (path, text) in WiringGapSourceTree.SourceFiles)
        {
            foreach (var name in names)
            {
                var search = "new " + name + "(";
                if (text.Contains(search, StringComparison.Ordinal)
                    || text.Contains("new " + name + " {", StringComparison.Ordinal))
                {
                    callSites[name].Add(path);
                }
            }
        }

        return callSites;
    }

    private static (HashSet<string> DeclarationFiles, List<string> ProductionFiles) SplitDeclarationAndProductionFiles(
        string eventName, List<string> filesWithConstruction)
    {
        // A file that DECLARES the event also contains its constructor; it is not a
        // producer. Declaration files contain "class/record <Name> ... DomainEvent".
        var declarationFiles = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (path, text) in WiringGapSourceTree.SourceFiles)
        {
            var declarationIndex = text.IndexOf("class " + eventName, StringComparison.Ordinal);
            if (declarationIndex < 0)
            {
                declarationIndex = text.IndexOf("record " + eventName, StringComparison.Ordinal);
            }

            if (declarationIndex < 0)
            {
                continue;
            }

            var baseListWindow = text.Substring(declarationIndex, Math.Min(400, text.Length - declarationIndex));
            if (baseListWindow.Contains("DomainEvent", StringComparison.Ordinal))
            {
                declarationFiles.Add(path);
            }
        }

        return (declarationFiles, filesWithConstruction.Where(path => !declarationFiles.Contains(path)).ToList());
    }
}
