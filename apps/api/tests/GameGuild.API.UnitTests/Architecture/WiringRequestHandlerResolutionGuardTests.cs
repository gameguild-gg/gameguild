using System.Reflection;
using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.API.UnitTests.Architecture;

/// <summary>
///     Wiring guard G7 — every concrete request must RESOLVE exactly one handler.
///     G1 (<see cref="WiringCommandRegistrationGuardTests"/>) closes the
///     "dead command" hole for <c>ICommand</c>/<c>ICommand&lt;T&gt;</c>; this guard
///     generalizes the same check to the whole request surface: every concrete
///     <c>IRequest</c>/<c>ICommand</c>/<c>IQuery</c> type in the module assemblies
///     must resolve the closed <c>IRequestHandler&lt;TRequest,TResponse&gt;</c> service
///     the mediator actually asks for (<c>MediatorSender.GetHandler</c> →
///     <c>ServiceFactory</c>), and it must resolve exactly ONE implementation —
///     zero handlers is the 15×500 class of gap from issue #1203 (31 dead authz
///     endpoints kept green by IMediator-mocked tests), and two distinct
///     implementations resolve last-wins-silently, which hides wiring mistakes.
/// </summary>
[Collection(WiringGapHostCollection.Name)]
public sealed class WiringRequestHandlerResolutionGuardTests(WiringGapHostFixture host)
{
    [Fact]
    public void EveryConcreteRequest_ResolvesExactlyOneHandlerInTheBuiltContainer()
    {
        var assemblies = WiringGapSourceTree.LoadProductAssemblies();
        var requests = assemblies
            .SelectMany(WiringGapSourceTree.GetLoadableTypes)
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .Where(IsRequest)
            .Distinct()
            .ToList();

        requests.Should().HaveCountGreaterThanOrEqualTo(700,
            "the production request surface (commands + queries) is large; a much smaller count means the assembly scan is broken");

        var unavailable = assemblies
            .SelectMany(assembly => assembly.GetCustomAttributes<UseCaseEventContractAttribute>())
            .Where(contract => contract.UnavailableReason is not null)
            .Select(contract => contract.CommandType)
            .ToHashSet();

        // The canonical service the mediator resolves for a request: the closed
        // IRequestHandler<TRequest,TResponse> for every response the type declares.
        var registrationsByServiceType = host.Registrations
            .GroupBy(descriptor => descriptor.ServiceType)
            .ToDictionary(
                grouping => grouping.Key,
                grouping => grouping.Select(descriptor => descriptor.ImplementationType)
                    .Where(type => type is not null)
                    .Distinct()
                    .ToList());

        var violations = new List<string>();
        using var scope = host.Services.CreateScope();
        foreach (var request in requests)
        {
            if (unavailable.Contains(request))
            {
                // Deliberately unwired commands declare UnavailableReason on their
                // use-case event contract; AddUnavailableUseCaseHandlers registers
                // their throwing handler (same exemption as G1).
                continue;
            }

            if (InternalOnlyRequests.Contains(request.FullName ?? request.Name))
            {
                // Allowlisted internal-only request types — see the comments below.
                continue;
            }

            var responseTypes = DeclaredResponseTypes(request);
            responseTypes.Should().NotBeEmpty(
                "{0} implements IRequest but no closed IRequest<TResponse> interface could be found", request.FullName);

            var resolvedAtLeastOne = false;
            foreach (var response in responseTypes)
            {
                var canonicalService = typeof(IRequestHandler<,>).MakeGenericType(request, response);

                if (registrationsByServiceType.TryGetValue(canonicalService, out var implementations)
                    && implementations.Count > 1)
                {
                    violations.Add(WiringGapBaseline.Id(
                        WiringGapBaseline.GuardIds.RequestHandlerResolution, Describe(request, response))
                        + $"|ambiguous:{implementations.Count}");
                    resolvedAtLeastOne = true; // the ambiguity violation is the finding; the request is not dead
                    continue;
                }

                try
                {
                    if (scope.ServiceProvider.GetService(canonicalService) is not null)
                    {
                        resolvedAtLeastOne = true;
                    }
                }
                catch (Exception exception) when (exception is not NotImplementedException)
                {
                    violations.Add(WiringGapBaseline.Id(
                        WiringGapBaseline.GuardIds.RequestHandlerResolution, Describe(request, response))
                        + "|resolve-throws:" + exception.GetType().Name);
                    resolvedAtLeastOne = true; // a failing registration is wiring feedback, not a dead request
                }
            }

            if (!resolvedAtLeastOne)
            {
                violations.Add(WiringGapBaseline.Id(
                    WiringGapBaseline.GuardIds.RequestHandlerResolution, request.FullName ?? request.Name));
            }
        }

        WiringGapBaseline.AssertNoNewGaps(
            WiringGapBaseline.GuardIds.RequestHandlerResolution,
            "every concrete IRequest/ICommand/IQuery type must resolve exactly one closed IRequestHandler<TRequest,TResponse> " +
            "in the built host container (zero handlers = the 15x500 dead-endpoint class of #1203; more than one distinct " +
            "implementation = silent last-wins dispatch). Requests whose use-case event contract declares UnavailableReason " +
            "are exempt (their throwing UnavailableCommandHandler is registered by design)",
            violations);
        WiringGapBaseline.AssertNoStaleEntries(
            WiringGapBaseline.GuardIds.RequestHandlerResolution, violations);
    }

    internal static bool IsRequest(Type type) =>
        type.GetInterfaces().Any(contract =>
            contract == typeof(IRequest)
            || (contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequest<>)));

    /// <summary>
    ///     The response types this request declares through closed
    ///     <c>IRequest&lt;TResponse&gt;</c> interfaces (a plain <c>IRequest</c>
    ///     declares <c>Unit</c>).
    /// </summary>
    internal static IReadOnlyList<Type> DeclaredResponseTypes(Type request) => request.GetInterfaces()
        .Where(contract => contract.IsGenericType
                           && contract.GetGenericTypeDefinition() == typeof(IRequest<>))
        .Select(contract => contract.GetGenericArguments()[0])
        .Distinct()
        .ToList();

    private static string Describe(Type request, Type response) =>
        (request.FullName ?? request.Name) + "->" + (response.FullName ?? response.Name);

    /// <summary>
    ///     Request types that exist for a subsystem's INTERNAL dispatch machinery and
    ///     are never sent through the host mediator surface, so requiring a DI handler
    ///     for them would be noise. Every entry needs a comment naming its owner.
    /// </summary>
    private static readonly HashSet<string> InternalOnlyRequests =
    [
        // (none yet — populated as discovered; each entry must carry a justification
        //  comment naming the subsystem that owns the type and why it is internal-only)
    ];
}
