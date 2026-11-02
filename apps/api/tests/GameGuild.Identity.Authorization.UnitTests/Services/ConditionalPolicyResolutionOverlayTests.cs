using FluentAssertions;
using GameGuild.Identity.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Services;

/// <summary>
///     Pins the conditional-policy overlay at resolution time (issue #1204):
///     policies are consulted whenever permissions resolve; matching denies
///     subtract; zero-policy tenants keep the allow-by-no-policy default; and a
///     policy-configured tenant whose evaluation fails fails closed (opt-out
///     documented) instead of silently allowing.
/// </summary>
public sealed class ConditionalPolicyResolutionOverlayTests
{
    private readonly Mock<IConditionalPolicyRepository> _policyRepository = new();
    private readonly Mock<IConditionalPolicyEvaluator> _evaluator = new();

    private static PermissionEvaluationExtensionContext CreateContext(
        Guid tenantId,
        params string[] allows) => new()
    {
        UserId = Guid.NewGuid(),
        TenantId = tenantId,
        CurrentAllows = new HashSet<string>(allows, StringComparer.OrdinalIgnoreCase),
        CurrentDenies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    };

    private ConditionalPolicyEvaluationExtension CreateExtension(bool failClosed = true) =>
        new(
            _policyRepository.Object,
            _evaluator.Object,
            Options.Create(new PermissionEngineOptions
            {
                ConditionalPolicy = new ConditionalPolicyOptions { FailClosedOnEvaluationError = failClosed }
            }),
            NullLogger<ConditionalPolicyEvaluationExtension>.Instance);

    private void SetupPolicies(params ConditionalPolicy[] policies) =>
        _policyRepository
            .Setup(r => r.GetActivePoliciesAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(policies.ToList());

    [Fact]
    public async Task TenantWithoutPolicies_ContributesNothing_AndNeverInvokesTheEvaluator()
    {
        SetupPolicies();
        var extension = CreateExtension();

        var result = await extension.EvaluateAsync(CreateContext(Guid.NewGuid(), "courses:read"));

        result.Should().BeSameAs(PermissionEvaluationExtensionResult.None,
            "no policy configured is the documented allow-by-no-policy case");
        _evaluator.Verify(
            e => e.EvaluateActionsAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task MatchingTargetedDeny_SubtractsTheTargetedAction()
    {
        var tenantId = Guid.NewGuid();
        SetupPolicies(new ConditionalPolicy { Name = "deny-read", IsEnabled = true, Action = PolicyAction.Deny });
        _evaluator
            .Setup(e => e.EvaluateActionsAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConditionalPolicySetEvaluationResult(
                1, ["courses:read"], false, Array.Empty<PolicyEvaluationDetail>()));
        var extension = CreateExtension();

        var result = await extension.EvaluateAsync(CreateContext(tenantId, "courses:read", "courses:write"));

        result.AdditionalDenies.Should().BeEquivalentTo(["courses:read"]);
        result.AdditionalAllows.Should().BeEmpty();
        _evaluator.Verify(
            e => e.EvaluateActionsAsync(
                It.IsAny<Guid>(), tenantId, It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.Is<IReadOnlyCollection<string>>(actions => actions.Count == 2),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "policies must be consulted with every currently-allowed action at resolution time");
    }

    [Fact]
    public async Task ContextWideDenial_SubtractsEverythingCurrentlyAllowed()
    {
        SetupPolicies(new ConditionalPolicy { Name = "deny-all", IsEnabled = true, Action = PolicyAction.Deny });
        _evaluator
            .Setup(e => e.EvaluateActionsAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConditionalPolicySetEvaluationResult(
                1, ["courses:read", "courses:write"], true, Array.Empty<PolicyEvaluationDetail>()));
        var extension = CreateExtension();

        var result = await extension.EvaluateAsync(CreateContext(Guid.NewGuid(), "courses:read", "courses:write"));

        result.AdditionalDenies.Should().BeEquivalentTo(["courses:read", "courses:write"],
            "an unconstrained deny policy subtracts everything this resolution allowed");
    }

    [Fact]
    public async Task EvaluatorFailure_InPolicyConfiguredTenant_FailsClosedByDefault()
    {
        SetupPolicies(new ConditionalPolicy { Name = "deny-read", IsEnabled = true, Action = PolicyAction.Deny });
        _evaluator
            .Setup(e => e.EvaluateActionsAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("policy store unavailable"));
        var extension = CreateExtension(failClosed: true);

        var result = await extension.EvaluateAsync(CreateContext(Guid.NewGuid(), "courses:read", "courses:write"));

        result.AdditionalDenies.Should().BeEquivalentTo(["courses:read", "courses:write"],
            "policies exist but cannot be enforced: the resolution must not silently allow (issue #1204)");
    }

    [Fact]
    public async Task EvaluatorFailure_WithExplicitOptOut_ContributesNothing()
    {
        SetupPolicies(new ConditionalPolicy { Name = "deny-read", IsEnabled = true, Action = PolicyAction.Deny });
        _evaluator
            .Setup(e => e.EvaluateActionsAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<Guid?>(),
                It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("policy store unavailable"));
        var extension = CreateExtension(failClosed: false);

        var result = await extension.EvaluateAsync(CreateContext(Guid.NewGuid(), "courses:read"));

        result.Should().BeSameAs(PermissionEvaluationExtensionResult.None,
            "FailClosedOnEvaluationError=false is the documented explicit fail-open opt-out");
    }

    [Fact]
    public async Task PolicyLookupFailure_FailsClosedByDefault()
    {
        _policyRepository
            .Setup(r => r.GetActivePoliciesAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database unavailable"));
        var extension = CreateExtension(failClosed: true);

        var result = await extension.EvaluateAsync(CreateContext(Guid.NewGuid(), "courses:read"));

        result.AdditionalDenies.Should().BeEquivalentTo(["courses:read"],
            "when policy existence cannot be established the guard treats the tenant as policy-configured");
    }

    [Fact]
    public async Task EmptyAllowSet_ShortCircuitsWithoutTouchingThePolicyStore()
    {
        var extension = CreateExtension();

        var result = await extension.EvaluateAsync(CreateContext(Guid.NewGuid()));

        result.Should().BeSameAs(PermissionEvaluationExtensionResult.None);
        _policyRepository.Verify(
            r => r.GetActivePoliciesAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void ExtensionOrdersBeforeTheAbacOverlay()
    {
        var extension = CreateExtension();

        extension.Order.Should().BeLessThan(new AbacPolicyEvaluationExtension(
                new Mock<IAbacPolicyEvaluator>().Object,
                NullLogger<AbacPolicyEvaluationExtension>.Instance).Order,
            "the ABAC overlay must stay the final veto (Order 500)");
        extension.Order.Should().Be(400);
        extension.Name.Should().Be("ConditionalPolicyOverlay");
    }
}

/// <summary>
///     Pins the set-based conditional evaluation used at resolution time (issue #1204).
/// </summary>
public sealed class ConditionalPolicyEvaluatorSetTests
{
    private readonly Mock<IConditionalPolicyRepository> _repository = new();

    private ConditionalPolicyEvaluator CreateEvaluator() =>
        new(_repository.Object, NullLogger<ConditionalPolicyEvaluator>.Instance);

    private void SetupPolicies(params ConditionalPolicy[] policies) =>
        _repository
            .Setup(r => r.GetActivePoliciesAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(policies.ToList());

    [Fact]
    public async Task ZeroPolicies_ReturnsAllowByNoPolicy()
    {
        SetupPolicies();
        var evaluator = CreateEvaluator();

        var result = await evaluator.EvaluateActionsAsync(
            Guid.NewGuid(), Guid.NewGuid(), null, null, ["courses:read"]);

        result.PolicyCount.Should().Be(0);
        result.DeniedActions.Should().BeEmpty();
        result.ContextWideDenial.Should().BeFalse();
    }

    [Fact]
    public async Task UnconditionalTargetedDeny_SubtractsOnlyItsTarget()
    {
        SetupPolicies(
            new ConditionalPolicy
            {
                Name = "deny-read",
                IsEnabled = true,
                Action = PolicyAction.Deny,
                PermissionType = "courses:read"
            },
            new ConditionalPolicy
            {
                // Defense-in-depth: a disabled policy slipping through the repository
                // filter must still contribute nothing.
                Name = "disabled deny",
                IsEnabled = false,
                Action = PolicyAction.Deny
            });
        var evaluator = CreateEvaluator();

        var result = await evaluator.EvaluateActionsAsync(
            Guid.NewGuid(), Guid.NewGuid(), null, null, ["courses:read", "courses:write"]);

        result.PolicyCount.Should().Be(2, "the count reports the evaluated policy list");
        result.DeniedActions.Should().BeEquivalentTo(["courses:read"],
            "only the enabled targeted deny subtracts");
        result.ContextWideDenial.Should().BeFalse();
    }

    [Fact]
    public async Task UnconstrainedDeny_IsContextWide()
    {
        SetupPolicies(new ConditionalPolicy
        {
            Name = "deny-everything",
            IsEnabled = true,
            Action = PolicyAction.Deny
        });
        var evaluator = CreateEvaluator();

        var result = await evaluator.EvaluateActionsAsync(
            Guid.NewGuid(), Guid.NewGuid(), null, null, ["courses:read", "billing:manage"]);

        result.ContextWideDenial.Should().BeTrue();
        result.DeniedActions.Should().BeEquivalentTo(["courses:read", "billing:manage"]);
    }

    [Fact]
    public async Task DenyWithUnmetEnvironmentConditions_DoesNotSubtract()
    {
        // MFA-gated deny: at resolution time IsMfaVerified is unknown (null), so the
        // policy's conditions are not met and it stays enforced by request-time gates.
        SetupPolicies(new ConditionalPolicy
        {
            Name = "require-mfa-deny",
            IsEnabled = true,
            Action = PolicyAction.Deny,
            PermissionType = "courses:read",
            EnvironmentConditions = """{ "RequireMfa": true }"""
        });
        var evaluator = CreateEvaluator();

        var result = await evaluator.EvaluateActionsAsync(
            Guid.NewGuid(), Guid.NewGuid(), null, null, ["courses:read"]);

        result.DeniedActions.Should().BeEmpty(
            "request-only attributes (MFA) never match at resolution time");
    }

    [Fact]
    public async Task ResourceTypeScopedDeny_DoesNotApplyToOtherResourceFrames()
    {
        SetupPolicies(new ConditionalPolicy
        {
            Name = "deny-billing-only",
            IsEnabled = true,
            Action = PolicyAction.Deny,
            ResourceType = "BillingAccount"
        });
        var evaluator = CreateEvaluator();

        var result = await evaluator.EvaluateActionsAsync(
            Guid.NewGuid(), Guid.NewGuid(), "Course", null, ["courses:read"]);

        result.DeniedActions.Should().BeEmpty("the policy is scoped to another resource type");
    }

    [Fact]
    public async Task AllowPolicy_NeverMintsOrDeniesAnything()
    {
        SetupPolicies(new ConditionalPolicy
        {
            Name = "allow-list",
            IsEnabled = true,
            Action = PolicyAction.Allow
        });
        var evaluator = CreateEvaluator();

        var result = await evaluator.EvaluateActionsAsync(
            Guid.NewGuid(), Guid.NewGuid(), null, null, ["courses:read"]);

        result.DeniedActions.Should().BeEmpty("allow-effect policies never mint permissions");
    }
}
