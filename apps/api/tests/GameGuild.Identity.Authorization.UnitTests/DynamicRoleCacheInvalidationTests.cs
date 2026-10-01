using FluentAssertions;
using GameGuild;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Authorization.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests;

public class DynamicRoleCacheInvalidationTests
{
    [Fact]
    public async Task CreateTenantRole_SavesBeforeInvalidatingTenantCaches()
    {
        var tenantId = Guid.NewGuid();
        var role = new DynamicRole { TenantId = tenantId };
        var order = new List<string>();
        var context = CreateContext(Array.Empty<DynamicRole>().AsQueryable().BuildMockDbSet(), order);
        var invalidation = new Mock<ICacheInvalidationService>(MockBehavior.Strict);
        invalidation
            .Setup(service => service.InvalidateTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .Callback(() => order.Add("invalidate"))
            .Returns(Task.CompletedTask);

        var repository = new DynamicRoleRepository(context.Object, invalidation.Object);

        await repository.CreateAsync(role);

        order.Should().ContainInOrder("save", "invalidate");
        invalidation.Verify(service => service.InvalidateTenantAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
        invalidation.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRoleMovingBetweenTenants_InvalidatesBothScopes()
    {
        var previousTenantId = Guid.NewGuid();
        var currentTenantId = Guid.NewGuid();
        var persistedRole = new DynamicRole { Id = Guid.NewGuid(), TenantId = previousTenantId };
        var updatedRole = new DynamicRole { Id = persistedRole.Id, TenantId = currentTenantId };
        var context = CreateContext(new[] { persistedRole }.AsQueryable().BuildMockDbSet());
        var invalidation = new Mock<ICacheInvalidationService>(MockBehavior.Strict);
        invalidation
            .Setup(service => service.InvalidateTenantAsync(previousTenantId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        invalidation
            .Setup(service => service.InvalidateTenantAsync(currentTenantId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var repository = new DynamicRoleRepository(context.Object, invalidation.Object);

        await repository.UpdateAsync(updatedRole);

        invalidation.Verify(service => service.InvalidateTenantAsync(previousTenantId, It.IsAny<CancellationToken>()), Times.Once);
        invalidation.Verify(service => service.InvalidateTenantAsync(currentTenantId, It.IsAny<CancellationToken>()), Times.Once);
        invalidation.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ChangingParentRole_InvalidatesTenantAfterPersistingTheHierarchyChange()
    {
        var tenantId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var oldParentRoleId = Guid.NewGuid();
        var newParentRoleId = Guid.NewGuid();
        var persistedRole = new DynamicRole
        {
            Id = roleId,
            TenantId = tenantId,
            ParentRoleId = oldParentRoleId
        };
        var updatedRole = new DynamicRole
        {
            Id = roleId,
            TenantId = tenantId,
            ParentRoleId = newParentRoleId
        };
        var order = new List<string>();
        var roleSet = new[] { persistedRole }.AsQueryable().BuildMockDbSet();
        roleSet
            .Setup(set => set.Update(It.Is<DynamicRole>(role => role.Id == roleId && role.ParentRoleId == newParentRoleId)))
            .Callback(() => order.Add("update"))
            .Returns((EntityEntry<DynamicRole>)null!);
        var context = CreateContext(roleSet, order);
        var invalidation = new Mock<ICacheInvalidationService>(MockBehavior.Strict);
        invalidation
            .Setup(service => service.InvalidateTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .Callback(() => order.Add("invalidate"))
            .Returns(Task.CompletedTask);

        var repository = new DynamicRoleRepository(context.Object, invalidation.Object);

        await repository.UpdateAsync(updatedRole);

        roleSet.Verify(
            set => set.Update(It.Is<DynamicRole>(role => role.Id == roleId && role.ParentRoleId == newParentRoleId)),
            Times.Once);
        order.Should().ContainInOrder("update", "save", "invalidate");
        invalidation.Verify(service => service.InvalidateTenantAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
        invalidation.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateGlobalRole_InvalidatesGlobalCaches()
    {
        var roleId = Guid.NewGuid();
        var persistedRole = new DynamicRole { Id = roleId };
        var updatedRole = new DynamicRole { Id = roleId, TenantId = Guid.NewGuid() };
        var context = CreateContext(new[] { persistedRole }.AsQueryable().BuildMockDbSet());
        var invalidation = new Mock<ICacheInvalidationService>(MockBehavior.Strict);
        invalidation
            .Setup(service => service.InvalidateGlobalAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var repository = new DynamicRoleRepository(context.Object, invalidation.Object);

        await repository.UpdateAsync(updatedRole);

        invalidation.Verify(service => service.InvalidateGlobalAsync(It.IsAny<CancellationToken>()), Times.Once);
        invalidation.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteTenantRole_InvalidatesTenantCachesAfterSave()
    {
        var tenantId = Guid.NewGuid();
        var role = new DynamicRole { Id = Guid.NewGuid(), TenantId = tenantId };
        var order = new List<string>();
        var context = CreateContext(new[] { role }.AsQueryable().BuildMockDbSet(), order);
        var invalidation = new Mock<ICacheInvalidationService>(MockBehavior.Strict);
        invalidation
            .Setup(service => service.InvalidateTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .Callback(() => order.Add("invalidate"))
            .Returns(Task.CompletedTask);

        var repository = new DynamicRoleRepository(context.Object, invalidation.Object);

        await repository.DeleteAsync(role.Id);

        order.Should().ContainInOrder("save", "invalidate");
        invalidation.Verify(service => service.InvalidateTenantAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
        invalidation.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateTenantRoleAssignment_InvalidatesUserInTenant()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var assignment = new DynamicRoleAssignment { UserId = userId, TenantId = tenantId };
        var order = new List<string>();
        var context = CreateContext(Array.Empty<DynamicRoleAssignment>().AsQueryable().BuildMockDbSet(), order);
        var invalidation = new Mock<ICacheInvalidationService>(MockBehavior.Strict);
        invalidation
            .Setup(service => service.InvalidateUserAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .Callback(() => order.Add("invalidate"))
            .Returns(Task.CompletedTask);

        var repository = new DynamicRoleAssignmentRepository(context.Object, invalidation.Object);

        await repository.CreateAsync(assignment);

        order.Should().ContainInOrder("save", "invalidate");
        invalidation.Verify(service => service.InvalidateUserAsync(userId, tenantId, It.IsAny<CancellationToken>()), Times.Once);
        invalidation.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateGlobalRoleAssignment_InvalidatesGlobalCaches()
    {
        var assignment = new DynamicRoleAssignment { UserId = Guid.NewGuid() };
        var context = CreateContext(Array.Empty<DynamicRoleAssignment>().AsQueryable().BuildMockDbSet());
        var invalidation = new Mock<ICacheInvalidationService>(MockBehavior.Strict);
        invalidation
            .Setup(service => service.InvalidateGlobalAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var repository = new DynamicRoleAssignmentRepository(context.Object, invalidation.Object);

        await repository.CreateAsync(assignment);

        invalidation.Verify(service => service.InvalidateGlobalAsync(It.IsAny<CancellationToken>()), Times.Once);
        invalidation.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteRoleAssignment_InvalidatesUserInAssignmentTenant()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var assignment = new DynamicRoleAssignment { UserId = userId, RoleId = roleId, TenantId = tenantId };
        var context = CreateContext(new[] { assignment }.AsQueryable().BuildMockDbSet());
        var invalidation = new Mock<ICacheInvalidationService>(MockBehavior.Strict);
        invalidation
            .Setup(service => service.InvalidateUserAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var repository = new DynamicRoleAssignmentRepository(context.Object, invalidation.Object);

        await repository.DeleteAsync(userId, roleId);

        invalidation.Verify(service => service.InvalidateUserAsync(userId, tenantId, It.IsAny<CancellationToken>()), Times.Once);
        invalidation.VerifyNoOtherCalls();
    }

    private static Mock<IApplicationDbContext> CreateContext<T>(
        Mock<DbSet<T>> set,
        List<string>? order = null)
        where T : class
    {
        var context = new Mock<IApplicationDbContext>(MockBehavior.Strict);
        context.Setup(database => database.Set<T>()).Returns(set.Object);
        context
            .Setup(database => database.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => order?.Add("save"))
            .ReturnsAsync(1);
        return context;
    }
}
