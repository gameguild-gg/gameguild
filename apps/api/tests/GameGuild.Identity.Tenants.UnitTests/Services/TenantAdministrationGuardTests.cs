using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Services;

/// <summary>
///     Ownership/role semantics of the tenant administration gate (issue #1287 T1).
///     Tenancy model (TenantRole): Owner carries full control INCLUDING deletion;
///     Admin carries full management EXCEPT deletion; SystemAdmin administers any
///     tenant; everyone else — including ordinary members and non-members — is denied.
/// </summary>
public class TenantAdministrationGuardTests : IDisposable
{
    private readonly Mock<ITenantMemberRepository> _memberRepositoryMock = new();
    private readonly ActorContextAccessor _actorAccessor = new();
    private readonly TenantAdministrationGuard _guard;
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly Guid _tenantId = Guid.NewGuid();

    public TenantAdministrationGuardTests()
    {
        _guard = new TenantAdministrationGuard(_actorAccessor, _memberRepositoryMock.Object);
    }

    public void Dispose() => _actorAccessor.ClearActorContext();

    [Fact]
    public async Task EnsureCanMutateAsync_AnonymousActor_Should_Throw()
    {
        // No actor scope set — the accessor reports Anonymous; the guard fails closed.
        var act = () => _guard.EnsureCanMutateAsync(_tenantId);

        await act.Should().ThrowAsync<AccessDeniedException>();
    }

    [Fact]
    public async Task EnsureCanPurgeAsync_AnonymousActor_Should_Throw()
    {
        var act = () => _guard.EnsureCanPurgeAsync(_tenantId);

        await act.Should().ThrowAsync<AccessDeniedException>();
    }

    [Fact]
    public async Task EnsureCanMutateAsync_NonMember_Should_Throw()
    {
        BeginScope(roles: []);
        SetupMembership(null);

        var act = () => _guard.EnsureCanMutateAsync(_tenantId);

        await act.Should().ThrowAsync<AccessDeniedException>(
            "an authenticated principal with no membership of the target tenant must not mutate it (the #1287 takeover class)");
    }

    [Theory]
    [InlineData("Member")]
    [InlineData("Moderator")]
    [InlineData("Guest")]
    public async Task EnsureCanMutateAsync_NonAdministrativeMember_Should_Throw(string role)
    {
        BeginScope(roles: []);
        SetupMembership(role);

        var act = () => _guard.EnsureCanMutateAsync(_tenantId);

        await act.Should().ThrowAsync<AccessDeniedException>(
            "ordinary members are not tenant administrators");
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Owner")]
    [InlineData("TenantAdmin")]
    public async Task EnsureCanMutateAsync_AdministrativeMember_Should_BeAllowed(string role)
    {
        BeginScope(roles: []);
        SetupMembership(role);

        var act = () => _guard.EnsureCanMutateAsync(_tenantId);

        await act.Should().NotThrowAsync("active Owner/Admin memberships carry tenant management");
    }

    [Fact]
    public async Task EnsureCanMutateAsync_InactiveMembership_Should_Throw()
    {
        BeginScope(roles: []);
        SetupMembership("Admin", isActive: false);

        var act = () => _guard.EnsureCanMutateAsync(_tenantId);

        await act.Should().ThrowAsync<AccessDeniedException>("inactive memberships never confer authority");
    }

    [Fact]
    public async Task EnsureCanMutateAsync_SoftDeletedMembership_Should_Throw()
    {
        BeginScope(roles: []);
        var membership = new TenantMember
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            UserId = _actorId,
            Role = "Admin",
            IsActive = true,
            DeletedAt = DateTime.UtcNow
        };
        _memberRepositoryMock
            .Setup(r => r.GetByUserAndTenantAsync(_actorId, _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);

        var act = () => _guard.EnsureCanMutateAsync(_tenantId);

        await act.Should().ThrowAsync<AccessDeniedException>();
    }

    [Fact]
    public async Task EnsureCanPurgeAsync_AdminMembership_Should_Throw()
    {
        // Tenancy model: "Administrator - Full management capabilities except tenant deletion".
        BeginScope(roles: []);
        SetupMembership("Admin");

        var act = () => _guard.EnsureCanPurgeAsync(_tenantId);

        await act.Should().ThrowAsync<AccessDeniedException>(
            "the Admin membership role explicitly excludes tenant deletion");
    }

    [Fact]
    public async Task EnsureCanPurgeAsync_OwnerMembership_Should_BeAllowed()
    {
        BeginScope(roles: []);
        SetupMembership("Owner");

        var act = () => _guard.EnsureCanPurgeAsync(_tenantId);

        await act.Should().NotThrowAsync("the Owner membership carries deletion rights");
    }

    [Fact]
    public async Task EnsureCanMutateAsync_SystemAdminRole_Should_BeAllowedWithoutMembership()
    {
        BeginScope(roles: ["SystemAdmin"]);
        _memberRepositoryMock
            .Setup(r => r.GetByUserAndTenantAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantMember?)null);

        var act = () => _guard.EnsureCanMutateAsync(_tenantId);

        await act.Should().NotThrowAsync("platform administrators administer any tenant");
    }

    [Fact]
    public async Task EnsureCanPurgeAsync_SystemAdminRole_Should_BeAllowedWithoutMembership()
    {
        BeginScope(roles: ["SystemAdmin"]);
        _memberRepositoryMock
            .Setup(r => r.GetByUserAndTenantAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantMember?)null);

        var act = () => _guard.EnsureCanPurgeAsync(_tenantId);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnsureCanPurgeAsync_NonMember_Should_Throw()
    {
        BeginScope(roles: []);
        SetupMembership(null);

        var act = () => _guard.EnsureCanPurgeAsync(_tenantId);

        await act.Should().ThrowAsync<AccessDeniedException>();
    }

    [Fact]
    public async Task EnsureCanPurgeAsync_UnresolvableSubjectIdentity_Should_Throw()
    {
        // Authenticated actor whose subject cannot be parsed as a user id.
        _actorAccessor.SetActorContext(new ActorContext
        {
            ActorKind = ActorKind.Service,
            SubjectId = "not-a-guid",
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            IsAuthenticated = true
        });

        var act = () => _guard.EnsureCanPurgeAsync(_tenantId);

        await act.Should().ThrowAsync<AccessDeniedException>();
    }

    private void BeginScope(string[] roles) =>
        _actorAccessor.BeginActorContextScope(
            ActorContextBuilder.ForUser(_actorId)
                .WithTenantId(_tenantId)
                .WithRoles(roles)
                .Build());

    private void SetupMembership(string? role, bool isActive = true)
    {
        var membership = role is null
            ? null
            : new TenantMember
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                UserId = _actorId,
                Role = role,
                IsActive = isActive
            };
        _memberRepositoryMock
            .Setup(r => r.GetByUserAndTenantAsync(_actorId, _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);
    }
}
