using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Services;

/// <summary>
///     Issue #331 "advanced expiration rules and conditions": the bulk set/extend operations of
///     <see cref="PermissionExpirationService"/> must reject rule violations BEFORE the first
///     grant is mutated (atomic rejection, no partial application, no audit, no version bump).
/// </summary>
public sealed class PermissionExpirationRuleEnforcementTests
{
    private static readonly Guid TenantA = Guid.NewGuid();

    private readonly Mock<ITenantPermissionRepository> _repoMock = new();
    private readonly Mock<IPermissionAuditService> _auditMock = new();
    private readonly Mock<ITenantSecurityVersionStore> _versionStoreMock = new();
    private readonly PermissionExpirationOptions _options = new();
    private readonly Mock<IActorContextAccessor> _actorAccessorMock = new();

    public PermissionExpirationRuleEnforcementTests()
    {
        _versionStoreMock
            .Setup(x => x.IncrementVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);
        _actorAccessorMock.SetupGet(x => x.ActorContext).Returns(ActorContext.Anonymous);
    }

    private PermissionExpirationService CreateSut()
    {
        var evaluator = new PermissionExpirationRuleEvaluator(Options.Create(_options));
        return new PermissionExpirationService(
            _repoMock.Object,
            _auditMock.Object,
            _versionStoreMock.Object,
            Options.Create(_options),
            NullLogger<PermissionExpirationService>.Instance,
            new InlineExecutor(),
            publisher: null,
            actorContextAccessor: _actorAccessorMock.Object,
            ruleEvaluator: evaluator);
    }

    private static TenantPermission CreateGrant(string[] permissions, DateTime? expiresAt) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        TenantId = TenantA,
        Permissions = permissions,
        ExpiresAt = expiresAt,
        IsActive = true
    };

    private void SetupGrants(params TenantPermission[] grants)
    {
        _repoMock
            .Setup(x => x.GetByIdsInTenantAsync(TenantA, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(grants.ToList());
        _repoMock
            .Setup(x => x.UpdateAsync(It.IsAny<TenantPermission>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantPermission p, CancellationToken _) => p);
    }

    [Fact]
    public async Task SetExpiration_ClearingExpiration_WhenRuleRequiresOne_IsRejectedBeforeAnyMutation()
    {
        _options.Rules["billing:*"] = new PermissionExpirationRule { RequireExpiration = true };
        var grant = CreateGrant(new[] { "billing:refunds:approve" }, SystemClock.UtcNow.AddDays(1));
        SetupGrants(grant);

        var act = () => CreateSut().SetExpirationAsync(TenantA, new[] { grant.Id }, null);

        (await act.Should().ThrowAsync<PermissionExpirationRuleViolationException>())
            .Which.Violations.Should().ContainSingle(v => v.Constraint == "RequireExpiration")
            .Which.MatchedRuleKeys.Should().Contain("billing:*");

        grant.ExpiresAt.Should().NotBeNull();
        _repoMock.Verify(x => x.UpdateAsync(It.IsAny<TenantPermission>(), It.IsAny<CancellationToken>()), Times.Never);
        _auditMock.Verify(x => x.LogPermissionChangeAsync(
            It.IsAny<PermissionOperationType>(), It.IsAny<Guid?>(), It.IsAny<Guid>(), It.IsAny<Guid?>(),
            It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _versionStoreMock.Verify(x => x.IncrementVersionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetExpiration_BeyondMaxDuration_IsRejected()
    {
        _options.GlobalRule = new PermissionExpirationRule { MaxDuration = TimeSpan.FromDays(30) };
        var grant = CreateGrant(new[] { "courses:read" }, null);
        SetupGrants(grant);

        var act = () => CreateSut().SetExpirationAsync(TenantA, new[] { grant.Id }, SystemClock.UtcNow.AddDays(45));

        await act.Should().ThrowAsync<PermissionExpirationRuleViolationException>();
        grant.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task SetExpiration_CompliantWithRules_Proceeds()
    {
        _options.GlobalRule = new PermissionExpirationRule
        {
            MinDuration = TimeSpan.FromHours(1),
            MaxDuration = TimeSpan.FromDays(30)
        };
        var grant = CreateGrant(new[] { "courses:read" }, null);
        SetupGrants(grant);

        var updated = await CreateSut().SetExpirationAsync(TenantA, new[] { grant.Id }, SystemClock.UtcNow.AddDays(7));

        updated.Should().HaveCount(1);
        grant.ExpiresAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SetExpiration_OneViolatingGrantRejectsTheWholeBatch()
    {
        _options.Rules["sensitive:action"] = new PermissionExpirationRule { RequireExpiration = true };
        var compliant = CreateGrant(new[] { "courses:read" }, null);
        var violating = CreateGrant(new[] { "sensitive:action" }, SystemClock.UtcNow.AddDays(2));
        SetupGrants(compliant, violating);

        var act = () => CreateSut().SetExpirationAsync(TenantA, new[] { compliant.Id, violating.Id }, null);

        await act.Should().ThrowAsync<PermissionExpirationRuleViolationException>();
        compliant.ExpiresAt.Should().BeNull();
        violating.ExpiresAt.Should().NotBeNull();
        _repoMock.Verify(x => x.UpdateAsync(It.IsAny<TenantPermission>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExtendExpiration_SingleExtensionBeyondCap_IsRejected()
    {
        _options.Rules["courses:*"] = new PermissionExpirationRule { MaxExtension = TimeSpan.FromDays(7) };
        var original = SystemClock.UtcNow.AddDays(2);
        var grant = CreateGrant(new[] { "courses:read" }, original);
        SetupGrants(grant);

        var act = () => CreateSut().ExtendExpirationAsync(TenantA, new[] { grant.Id }, TimeSpan.FromDays(14));

        (await act.Should().ThrowAsync<PermissionExpirationRuleViolationException>())
            .Which.Violations.Should().Contain(v => v.Constraint == "MaxExtension");
        grant.ExpiresAt.Should().Be(original);
    }

    [Fact]
    public async Task ExtendExpiration_ResultingExpirationBeyondCap_IsRejected()
    {
        _options.Rules["courses:*"] = new PermissionExpirationRule { MaxDuration = TimeSpan.FromDays(30) };
        var current = SystemClock.UtcNow.AddDays(20);
        var grant = CreateGrant(new[] { "courses:read" }, current);
        SetupGrants(grant);

        var act = () => CreateSut().ExtendExpirationAsync(TenantA, new[] { grant.Id }, TimeSpan.FromDays(15));

        (await act.Should().ThrowAsync<PermissionExpirationRuleViolationException>())
            .Which.Violations.Should().Contain(v => v.Constraint == "MaxDuration");
        grant.ExpiresAt.Should().Be(current);
    }

    [Fact]
    public async Task ExtendExpiration_CompliantExtension_Proceeds()
    {
        _options.Rules["courses:*"] = new PermissionExpirationRule
        {
            MaxExtension = TimeSpan.FromDays(7),
            MaxDuration = TimeSpan.FromDays(30)
        };
        var current = SystemClock.UtcNow.AddDays(10);
        var grant = CreateGrant(new[] { "courses:read" }, current);
        SetupGrants(grant);

        var updated = await CreateSut().ExtendExpirationAsync(TenantA, new[] { grant.Id }, TimeSpan.FromDays(5));

        updated.Should().HaveCount(1);
        grant.ExpiresAt.Should().BeAfter(current);
    }

    [Fact]
    public async Task SetExpiration_WithoutEvaluator_ConfiguredRulesAreInert()
    {
        // Backward compatibility: services constructed without a rule evaluator (older DI
        // graphs, test doubles) keep behaving exactly as before even if options carry rules.
        _options.GlobalRule = new PermissionExpirationRule { RequireExpiration = true };
        var grant = CreateGrant(new[] { "courses:read" }, SystemClock.UtcNow.AddDays(1));
        SetupGrants(grant);

        var service = new PermissionExpirationService(
            _repoMock.Object,
            _auditMock.Object,
            _versionStoreMock.Object,
            Options.Create(_options),
            NullLogger<PermissionExpirationService>.Instance,
            new InlineExecutor());

        var updated = await service.SetExpirationAsync(TenantA, new[] { grant.Id }, null);

        updated.Should().HaveCount(1);
        grant.ExpiresAt.Should().BeNull();
    }

    private sealed class InlineExecutor : IPermissionMutationExecutor
    {
        Task<Result<T>> IPermissionMutationExecutor.ExecuteSystemAsync<T>(PermissionSystemMutation maintenance, Guid? tenantId,
            Func<CancellationToken, Task<Result<T>>> operation, CancellationToken cancellationToken,
            Func<T, bool>? shouldCommit, bool useEntityTenantScope) =>
            operation(cancellationToken);

        Task<Result<T>> IPermissionMutationExecutor.ExecuteAsync<T>(Guid actorId, Guid? tenantId,
            Func<CancellationToken, Task<Result<T>>> operation, CancellationToken cancellationToken) =>
            operation(cancellationToken);

        Task<Result<T>> IPermissionMutationExecutor.ExecuteAsync<T>(Guid actorId, Guid? tenantId,
            Func<CancellationToken, Task<Result<T>>> operation, CancellationToken cancellationToken,
            Func<T, bool>? shouldCommit) =>
            operation(cancellationToken);

        Task<Result<T>> IPermissionMutationExecutor.ExecuteAsync<T>(Guid actorId, Guid? tenantId,
            Func<CancellationToken, Task<Result<T>>> operation, CancellationToken cancellationToken,
            Func<T, bool>? shouldCommit, bool useEntityTenantScope) =>
            operation(cancellationToken);
    }
}
