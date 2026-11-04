using System.Reflection;
using FluentAssertions;
using GameGuild.Identity.Authentication;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace GameGuild.API.UnitTests.Architecture;

/// <summary>
///     Pins the truthfulness of the planned-but-unavailable authorization management
///     surface (issue #1203, docs/authorization/cqrs-wiring-matrix.md §2.1/§2.2).
/// </summary>
/// <remarks>
///     The 16 management commands that answer 422 via the unavailable-contract
///     mechanism must (a) KEEP that contract — the honest, designed failure mode —
///     and (b) live on controllers hidden from OpenAPI
///     (<c>[ApiExplorerSettings(IgnoreApi = true)]</c>), so the generated client
///     never advertises them as working endpoints. Implementing the 16 management
///     APIs is tracked by issue #1203 as follow-up work; until then this guard keeps
///     the advertised surface truthful.
/// </remarks>
public sealed class PlannedAuthorizationSurfaceGuardTests
{
    private static readonly Type[] ControllersCarryingPlannedEndpoints =
    [
        typeof(AbacPolicyController),
        typeof(ConditionalPolicyCrudController),
        typeof(ConditionalPolicyEvaluationController),
    ];

    private static readonly Type[] UnavailableAuthorizationCommands =
    [
        // ABAC management (8)
        typeof(CreateAbacPolicyCommand),
        typeof(UpdateAbacPolicyCommand),
        typeof(DeleteAbacPolicyCommand),
        typeof(ActivateAbacPolicyCommand),
        typeof(DeactivateAbacPolicyCommand),
        typeof(CloneAbacPolicyCommand),
        typeof(ValidateAbacPolicyCommand),
        typeof(CreateAbacPolicyFromTemplateCommand),
        // Conditional policy management (8)
        typeof(CreateConditionalPolicyCommand),
        typeof(UpdateConditionalPolicyCommand),
        typeof(DeleteConditionalPolicyCommand),
        typeof(ActivateConditionalPolicyCommand),
        typeof(DeactivateConditionalPolicyCommand),
        typeof(CloneConditionalPolicyCommand),
        typeof(UpdateConditionalPolicyPriorityCommand),
        typeof(CreateConditionalPolicyFromTemplateCommand),
    ];

    [Fact]
    public void ControllersCarryingPlannedEndpoints_AreHiddenFromTheGeneratedClient()
    {
        foreach (var controller in ControllersCarryingPlannedEndpoints)
        {
            var ignoreApi = controller.GetCustomAttribute<ApiExplorerSettingsAttribute>();
            ignoreApi.Should().NotBeNull(
                "{0} serves planned/unavailable endpoints and must stay excluded from the OpenAPI surface",
                controller.Name);
            ignoreApi!.IgnoreApi.Should().BeTrue(
                "{0} must set ApiExplorerSettings.IgnoreApi=true so the generated client never advertises its endpoints as working",
                controller.Name);
        }
    }

    [Fact]
    public void PlannedAuthorizationCommands_KeepTheUnavailableContract()
    {
        var contracts = typeof(AbacPolicyController).Assembly
            .GetCustomAttributes<UseCaseEventContractAttribute>()
            .ToDictionary(contract => contract.CommandType, contract => contract.UnavailableReason);

        foreach (var command in UnavailableAuthorizationCommands)
        {
            contracts.TryGetValue(command, out var unavailableReason).Should().BeTrue(
                "{0} is a planned management command: it must keep its use-case event contract so the throwing " +
                "UnavailableCommandHandler answers with the designed 422 instead of a 500 handler-not-found",
                command.Name);
            unavailableReason.Should().NotBeNullOrWhiteSpace(
                "{0}'s contract must carry the UnavailableReason text", command.Name);
        }
    }
}
