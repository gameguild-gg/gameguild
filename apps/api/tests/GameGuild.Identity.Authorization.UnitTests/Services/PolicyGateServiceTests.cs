using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameGuild.Identity.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Services;

public class PolicyGateServiceTests
{
    private readonly Mock<IConditionalPolicyEvaluator> _conditionalMock = new();
    private readonly Mock<IAbacPolicyEvaluator> _abacMock = new();
    private readonly PolicyGateService _sut;

    public PolicyGateServiceTests()
    {
        _sut = new PolicyGateService(
            _conditionalMock.Object,
            _abacMock.Object,
            NullLogger<PolicyGateService>.Instance
        );
    }

    private static PolicyGateContext CreateContext(
        string? ipAddress = "192.168.1.1",
        string? userAgent = "TestBrowser/1.0",
        Dictionary<string, object>? attributes = null)
    {
        return new PolicyGateContext
        {
            ActorId = Guid.NewGuid(),
            ResourceType = "document",
            Action = "read",
            TenantId = Guid.NewGuid(),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Attributes = attributes
        };
    }

    // ── EvaluateGatesAsync ────────────────────────────────────

    [Fact]
    public async Task EvaluateGatesAsync_AllPass_ReturnsAllowed()
    {
        var context = CreateContext();

        _conditionalMock
            .Setup(x => x.EvaluateAsync(It.IsAny<ConditionalPolicyContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConditionalPolicyResult(IsAllowed: true));

        _abacMock
            .Setup(x => x.EvaluateAsync(It.IsAny<AbacRequestContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AbacEvaluationResult(AbacDecision.Permit));

        var result = await _sut.EvaluateGatesAsync(context);

        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateGatesAsync_LocalhostInProduction_Denied()
    {
        var context = CreateContext(
            ipAddress: "127.0.0.1",
            attributes: new Dictionary<string, object> { { "environment", "production" } });

        var result = await _sut.EvaluateGatesAsync(context);

        result.IsAllowed.Should().BeFalse();
        result.DeniedByGate.Should().Be(PolicyGateType.Static);
    }

    [Fact]
    public async Task EvaluateGatesAsync_MissingUserAgentWhenRequired_Denied()
    {
        var context = CreateContext(
            userAgent: null,
            attributes: new Dictionary<string, object> { { "require-user-agent", "true" } });

        var result = await _sut.EvaluateGatesAsync(context);

        result.IsAllowed.Should().BeFalse();
        result.DeniedByGate.Should().Be(PolicyGateType.Static);
    }

    [Fact]
    public async Task EvaluateGatesAsync_ConditionalPolicyDenied_ReturnsConditionalDenial()
    {
        var context = CreateContext();

        _conditionalMock
            .Setup(x => x.EvaluateAsync(It.IsAny<ConditionalPolicyContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConditionalPolicyResult(
                IsAllowed: false,
                DeniedByPolicyId: Guid.NewGuid(),
                DenialReason: "Time window restriction"));

        var result = await _sut.EvaluateGatesAsync(context);

        result.IsAllowed.Should().BeFalse();
        result.DeniedByGate.Should().Be(PolicyGateType.Conditional);
        result.DenialReason.Should().Contain("Time window");
    }

    [Fact]
    public async Task EvaluateGatesAsync_AbacDenied_ReturnsAbacDenial()
    {
        var context = CreateContext();

        _conditionalMock
            .Setup(x => x.EvaluateAsync(It.IsAny<ConditionalPolicyContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConditionalPolicyResult(IsAllowed: true));

        _abacMock
            .Setup(x => x.EvaluateAsync(It.IsAny<AbacRequestContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AbacEvaluationResult(
                AbacDecision.Deny,
                DecidingPolicyId: Guid.NewGuid(),
                DenialReason: "Insufficient attributes"));

        var result = await _sut.EvaluateGatesAsync(context);

        result.IsAllowed.Should().BeFalse();
        result.DeniedByGate.Should().Be(PolicyGateType.Abac);
    }

    [Fact]
    public async Task EvaluateGatesAsync_StaticGateFailsFirst_ConditionalNotEvaluated()
    {
        var context = CreateContext(
            ipAddress: "127.0.0.1",
            attributes: new Dictionary<string, object> { { "environment", "production" } });

        var result = await _sut.EvaluateGatesAsync(context);

        result.IsAllowed.Should().BeFalse();
        _conditionalMock.Verify(
            x => x.EvaluateAsync(It.IsAny<ConditionalPolicyContext>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── EvaluateGateAsync (single gate) ───────────────────────

    [Fact]
    public async Task EvaluateGateAsync_StaticGate_NoIssue_ReturnsAllowed()
    {
        var context = CreateContext();

        var result = await _sut.EvaluateGateAsync(PolicyGateType.Static, context);

        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateGateAsync_ConditionalGate_AllowedPolicy_ReturnsAllowed()
    {
        var context = CreateContext();

        _conditionalMock
            .Setup(x => x.EvaluateAsync(It.IsAny<ConditionalPolicyContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConditionalPolicyResult(IsAllowed: true));

        var result = await _sut.EvaluateGateAsync(PolicyGateType.Conditional, context);

        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateGateAsync_AbacGate_Permit_ReturnsAllowed()
    {
        var context = CreateContext();

        _abacMock
            .Setup(x => x.EvaluateAsync(It.IsAny<AbacRequestContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AbacEvaluationResult(AbacDecision.Permit));

        var result = await _sut.EvaluateGateAsync(PolicyGateType.Abac, context);

        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateGateAsync_EnvironmentGate_CurlUserAgent_AllowedWithDetail()
    {
        var context = CreateContext(userAgent: "curl/7.68.0");

        var result = await _sut.EvaluateGateAsync(PolicyGateType.Environment, context);

        result.IsAllowed.Should().BeTrue();
        result.GateDetails.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task EvaluateGateAsync_EnvironmentGate_NormalUserAgent_Allowed()
    {
        var context = CreateContext(userAgent: "Mozilla/5.0");

        var result = await _sut.EvaluateGateAsync(PolicyGateType.Environment, context);

        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateGateAsync_UnknownGateType_DefaultsToAllowed()
    {
        var context = CreateContext();

        var result = await _sut.EvaluateGateAsync((PolicyGateType)99, context);

        result.IsAllowed.Should().BeTrue();
    }
}
