using GameGuild.API.Data;
using GameGuild.Audit;
using GameGuild.Permissions.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Tests.Audit.Unit.Services;

/// <summary>
/// Test-specific ApplicationDbContext that ignores problematic entities
/// for in-memory database testing
/// </summary>
public class TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Explicitly configure AuditLog entity for testing
        modelBuilder.Entity<AuditLog>();

        // Ignore entities with Dictionary properties that can't be mapped by in-memory provider
        modelBuilder.Ignore<PermissionAuditLog>();
        modelBuilder.Ignore<PermissionDelegation>();
        modelBuilder.Ignore<PermissionTemplate>();
    }
}