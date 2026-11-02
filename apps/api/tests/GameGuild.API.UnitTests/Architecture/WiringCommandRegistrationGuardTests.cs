using System.Reflection;
using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.API.UnitTests.Architecture;

/// <summary>
///     Wiring guard G1 — command handlers must be REGISTERED, not just written.
///     A command type plus a handler class is not wiring: the closed
///     <c>IRequestHandler&lt;TCommand,TResponse&gt;</c> service must exist in the real
///     host service collection, or every <c>ISender.Send</c> of that command fails at
///     resolve time. Prevents the "dead command" class of gap (inspiring bug:
///     issue #1154 — LogoutCommand and its handler existed but nothing wired them).
///     Handler TYPE cardinality is already enforced by
///     <see cref="DurableEventArchitectureTests"/>; this guard closes the remaining
///     hole — the DI registration itself.
/// </summary>
[Collection(WiringGapHostCollection.Name)]
public sealed class WiringCommandRegistrationGuardTests(WiringGapHostFixture host)
{
    [Fact]
    public void EveryProductionCommand_HasItsHandlerRegisteredInHostDI()
    {
        var assemblies = WiringGapSourceTree.LoadProductAssemblies();
        var commands = assemblies
            .SelectMany(WiringGapSourceTree.GetLoadableTypes)
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .Where(IsCommand)
            .Distinct()
            .ToList();

        commands.Should().HaveCountGreaterThanOrEqualTo(380, "the production command surface is large; a much smaller count means the assembly scan is broken");

        var unavailable = assemblies
            .SelectMany(assembly => assembly.GetCustomAttributes<UseCaseEventContractAttribute>())
            .Where(contract => contract.UnavailableReason is not null)
            .Select(contract => contract.CommandType)
            .ToHashSet();

        var registeredServiceTypes = host.Registrations
            .Select(descriptor => descriptor.ServiceType)
            .ToHashSet();

        var unwired = new List<string>();
        foreach (var command in commands)
        {
            if (unavailable.Contains(command))
            {
                // Deliberately unwired commands declare UnavailableReason on their
                // use-case event contract (same exemption as DurableEventArchitectureTests).
                continue;
            }

            var handlerServiceTypes = HandlerServiceTypesFor(command);
            if (!handlerServiceTypes.Any(registeredServiceTypes.Contains))
            {
                unwired.Add(WiringGapBaseline.Id(
                    WiringGapBaseline.GuardIds.CommandRegistration, command.FullName ?? command.Name));
            }
        }

        WiringGapBaseline.AssertNoNewGaps(
            WiringGapBaseline.GuardIds.CommandRegistration,
            "every concrete ICommand/ICommand<T> must have its closed handler service registered by the host " +
            "(services.AddScoped<IRequestHandler<TCommand,TResponse>, ...>). Commands whose use-case event contract " +
            "declares UnavailableReason are exempt",
            unwired);
        WiringGapBaseline.AssertNoStaleEntries(
            WiringGapBaseline.GuardIds.CommandRegistration, unwired);
    }

    internal static bool IsCommand(Type type) =>
        type.GetInterfaces().Any(contract =>
            contract == typeof(ICommand)
            || (contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(ICommand<>)));

    /// <summary>
    ///     The closed service types a module may register a command handler under:
    ///     <c>IRequestHandler&lt;T,R&gt;</c> / <c>ICommandHandler&lt;T,R&gt;</c> for each
    ///     declared response, or the unit-response forms for commands without one.
    /// </summary>
    internal static IReadOnlyList<Type> HandlerServiceTypesFor(Type command)
    {
        var services = new List<Type>();
        var responseTypes = command.GetInterfaces()
            .Where(contract => contract.IsGenericType
                               && contract.GetGenericTypeDefinition() == typeof(IRequest<>))
            .Select(contract => contract.GetGenericArguments()[0])
            .Where(response => response != typeof(Unit))
            .Distinct()
            .ToList();

        if (responseTypes.Count > 0)
        {
            foreach (var response in responseTypes)
            {
                services.Add(typeof(IRequestHandler<,>).MakeGenericType(command, response));
                services.Add(typeof(ICommandHandler<,>).MakeGenericType(command, response));
            }

            return services;
        }

        // Unit-response commands may be registered via the one-argument forms; the
        // generic constraints only allow them for unit/command marker interfaces.
        services.Add(typeof(IRequestHandler<>).MakeGenericType(command));
        if (typeof(ICommand).IsAssignableFrom(command))
        {
            services.Add(typeof(ICommandHandler<>).MakeGenericType(command));
        }

        services.Add(typeof(IRequestHandler<,>).MakeGenericType(command, typeof(Unit)));
        return services;
    }
}
