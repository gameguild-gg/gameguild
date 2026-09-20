using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using GameGuild.API.Security;
using Xunit;

namespace GameGuild.API.UnitTests.Architecture;

/// <summary>
///     Architecture rule T1: every MVC controller endpoint must carry an explicit
///     authorization decision — [Authorize] on the controller or the action, or an
///     explicit [AllowAnonymous] listed (with a justification) in the host-side
///     <see cref="AnonymousEndpointRegistry"/>. A new unguarded controller or endpoint
///     fails these tests.
/// </summary>
/// <remarks>
///     The allowlist itself is HOST-side, per-repo content (<see cref="AnonymousEndpointRegistry"/>);
///     this test contains no product-specific entries and mirrors verbatim across products.
/// </remarks>
public sealed class ControllerAuthorizationArchitectureTests
{
    [Fact]
    public void EveryControllerEndpoint_DeclaresAnAuthorizationDecision()
    {
        var unguarded = new List<string>();

        foreach (var controller in GetControllers())
        {
            var classGuarded = controller.IsDefined(typeof(AuthorizeAttribute), true)
                || controller.IsDefined(typeof(AllowAnonymousAttribute), true);

            foreach (var action in GetEndpointActions(controller))
            {
                var actionGuarded = action.IsDefined(typeof(AuthorizeAttribute), true)
                    || action.IsDefined(typeof(AllowAnonymousAttribute), true);

                if (!classGuarded && !actionGuarded)
                {
                    unguarded.Add($"{controller.Name}.{action.Name}");
                }
            }
        }

        unguarded.Should().BeEmpty(
            "every endpoint must carry [Authorize] (class or action) or an explicit [AllowAnonymous]. " +
            "Unguarded: {0}", string.Join(", ", unguarded));
    }

    [Fact]
    public void EveryAnonymousEndpoint_IsInReviewedAllowlist()
    {
        var unlisted = new List<string>();

        foreach (var endpoint in GetAnonymousEndpoints())
        {
            if (!AnonymousEndpointRegistry.Entries.ContainsKey(endpoint))
            {
                unlisted.Add(endpoint);
            }
        }

        unlisted.Should().BeEmpty(
            "anonymous endpoints must be reviewed and added to {0} " +
            "(host-side, with a one-line justification). Unreviewed: {1}",
            nameof(AnonymousEndpointRegistry),
            string.Join(", ", unlisted));
    }

    [Fact]
    public void Allowlist_ContainsNoStaleEntries()
    {
        var anonymous = GetAnonymousEndpoints().ToHashSet(StringComparer.Ordinal);

        var stale = AnonymousEndpointRegistry.Entries.Keys.Where(entry => !anonymous.Contains(entry)).ToList();
        stale.Should().BeEmpty(
            "allowlist entries whose endpoints no longer exist must be removed. Stale: {0}",
            string.Join(", ", stale));
    }

    [Fact]
    public void Allowlist_Justifications_AreMeaningful()
    {
        var thin = AnonymousEndpointRegistry.Entries
            .Where(entry => entry.Value.Trim().Length < 15)
            .Select(entry => entry.Key)
            .ToList();

        thin.Should().BeEmpty("every allowlist entry needs a one-line justification a reviewer can evaluate.");
    }

    [Fact]
    public void Allowlist_ClassScopeEntries_PointToAnonymousControllers()
    {
        var misScoped = new List<string>();

        foreach (var entry in AnonymousEndpointRegistry.Entries.Keys.Where(IsClassScope))
        {
            var controllerName = ControllerName(entry);
            var controller = GetControllers().FirstOrDefault(type => type.Name == controllerName);

            if (controller is null || !controller.IsDefined(typeof(AllowAnonymousAttribute), true))
            {
                // Either the controller disappeared or it is no longer class-level anonymous
                // (it should be re-reviewed as per-action entries instead).
                misScoped.Add(entry);
            }
        }

        misScoped.Should().BeEmpty(
            "class-scope ('Controller.*') entries must point at controllers annotated [AllowAnonymous]. " +
            "Mis-scoped: {0}",
            string.Join(", ", misScoped));
    }

    /// <summary>
    ///     Enumerates every effectively anonymous endpoint. A controller whose class carries
    ///     [AllowAnonymous] exposes ALL of its endpoints anonymously, so it is reported once
    ///     under the class-scope key <c>Controller.*</c>; action-level [AllowAnonymous] is
    ///     reported per action.
    /// </summary>
    private static IEnumerable<string> GetAnonymousEndpoints()
    {
        foreach (var controller in GetControllers())
        {
            if (controller.IsDefined(typeof(AllowAnonymousAttribute), true))
            {
                var endpoints = GetEndpointActions(controller).ToList();
                if (endpoints.Count > 0)
                {
                    yield return $"{controller.Name}{AnonymousEndpointRegistry.ControllerScopeSuffix}";
                }
            }

            foreach (var action in GetEndpointActions(controller))
            {
                if (action.IsDefined(typeof(AllowAnonymousAttribute), true))
                {
                    yield return $"{controller.Name}.{action.Name}";
                }
            }
        }
    }

    private static bool IsClassScope(string entry) => entry.EndsWith(
        AnonymousEndpointRegistry.ControllerScopeSuffix, StringComparison.Ordinal);

    private static string ControllerName(string entry) =>
        entry[..^AnonymousEndpointRegistry.ControllerScopeSuffix.Length];

    private static IEnumerable<Type> GetControllers() =>
        LoadProductAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsClass: true, IsAbstract: false }
                && type.Name.EndsWith("Controller", StringComparison.Ordinal)
                && typeof(ControllerBase).IsAssignableFrom(type))
            .Distinct();

    private static IEnumerable<MethodInfo> GetEndpointActions(Type controller) =>
        controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.IsDefined(typeof(HttpMethodAttribute), true)
                || method.IsDefined(typeof(RouteAttribute), true));

    private static IEnumerable<Assembly> LoadProductAssemblies()
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<Assembly>([typeof(AnonymousEndpointRegistry).Assembly]);
        var loaded = new List<Assembly>();

        while (queue.Count > 0)
        {
            var assembly = queue.Dequeue();
            if (!visited.Add(assembly.GetName().Name ?? string.Empty))
            {
                continue;
            }

            loaded.Add(assembly);

            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                if (!reference.Name?.StartsWith("GameGuild", StringComparison.Ordinal) ?? true)
                {
                    continue;
                }

                try
                {
                    queue.Enqueue(Assembly.Load(reference));
                }
                catch
                {
                    // A referenced product assembly that cannot be loaded in the test
                    // context is skipped; the directly-referenced ones cover the API surface.
                }
            }
        }

        return loaded;
    }
}
