using FluentAssertions;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Authorization.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests;

public sealed class TenantPermissionHistoryTests
{
    [Fact]
    public async Task DeleteAsync_SoftDeletesAndPersistsThePermissionRow()
    {
        var permission = new TenantPermission
        {
            Id = Guid.NewGuid(),
            Version = 1,
            UserId = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Permissions = ["tenant:read"]
        };
        var set = new[] { permission }.AsQueryable().BuildMockDbSet();
        var context = new Mock<IApplicationDbContext>();
        context.Setup(db => db.Set<TenantPermission>()).Returns(set.Object);
        context.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var repository = new TenantPermissionRepository(context.Object);

        var deleted = await repository.DeleteAsync(permission.Id);

        deleted.Should().BeTrue();
        permission.DeletedAt.Should().NotBeNull();
        set.Verify(db => db.Remove(It.IsAny<TenantPermission>()), Times.Never);
        context.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByUserAndTenantAsync_HidesSoftDeletedPermissionRows()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var active = new TenantPermission { UserId = userId, TenantId = tenantId };
        var deleted = new TenantPermission { UserId = userId, TenantId = tenantId, DeletedAt = SystemClock.UtcNow };
        var context = new Mock<IApplicationDbContext>();
        context.Setup(db => db.Set<TenantPermission>())
            .Returns(new[] { deleted, active }.AsQueryable().BuildMockDbSet().Object);

        var repository = new TenantPermissionRepository(context.Object);

        var result = await repository.GetByUserAndTenantAsync(userId, tenantId);

        result.Should().BeSameAs(active);
    }

    [Fact]
    public void TenantPermissionUniqueIndex_OnlyConstrainsActiveRows()
    {
        var modelBuilder = new ModelBuilder();
        var entityBuilder = modelBuilder.Entity<TenantPermission>();
        new TenantPermissionConfiguration().Configure(entityBuilder);

        var index = modelBuilder.Model.FindEntityType(typeof(TenantPermission))!
            .GetIndexes()
            .Single(candidate => candidate.Name == "IX_TenantPermissions_User_Tenant");

        index.IsUnique.Should().BeTrue();
        index.GetFilter().Should().Be("\"DeletedAt\" IS NULL");
        modelBuilder.Model.FindEntityType(typeof(TenantPermission))!
            .GetDeclaredQueryFilters()
            .Should()
            .NotBeEmpty();
    }
}
