using GameGuild.API.Database;
using GameGuild.Identity.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Tests.UserProfiles.Unit;

/// <summary>
/// Test-specific ApplicationDbContext that ignores problematic entities
/// for in-memory database testing
/// </summary>
public class TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore entities with Dictionary properties that can't be mapped by in-memory provider
        modelBuilder.Ignore<PermissionAuditLog>();
        modelBuilder.Ignore<PermissionDelegation>();
            modelBuilder.Ignore<PermissionTemplate>();
    }
}
