using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.Compliance.Audit;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.API.UnitTests.Database;

/// <summary>
/// Field-level write tracking (issue #186 criterion 1): every tracked entity write stages one
/// FieldAccessAudit row per changed field inside the same SaveChanges call, with automatic
/// sensitive-field classification, privacy-safe masked values and trusted actor/tenant context.
/// </summary>
public sealed class FieldAccessWriteCaptureTests
{
    [Fact]
    public async Task ModifiedEntity_StagesPerFieldWriteRowsWithMaskedSensitiveValues()
    {
        var options = NewInMemoryOptions();
        var operationAccessor = new UseCaseOperationContextAccessor();
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        await using var context = new ApplicationDbContext(options, null, operationAccessor);
        var user = User.CreateOAuthUser("field-access@example.com", "Field User");
        context.Add(user);
        await context.SaveChangesAsync();

        using (operationAccessor.Begin(new UseCaseOperationContext(
            "operation-code", "TestCommand", tenantId, actorId, correlationId, null)))
        {
            user.Email = "rotated@example.com";
            user.Name = "Rotated Name";
            await context.SaveChangesAsync();
        }

        var emailRow = await UserFieldRows(context).SingleAsync(row =>
            row.FieldName == "Email" && row.AccessType == FieldAccessType.Write);
        emailRow.TenantId.Should().Be(tenantId);
        emailRow.UserId.Should().Be(actorId);
        emailRow.RequestId.Should().Be(correlationId.ToString());
        // Privacy-safe by construction: the classifier marks Email sensitive and only the masked
        // renderings persist, never the raw address.
        emailRow.IsSensitiveField.Should().BeTrue();
        emailRow.OldValue.Should().BeNull();
        emailRow.NewValue.Should().BeNull();
        emailRow.MaskedOldValue.Should().NotBeNullOrEmpty().And.NotContain("field-access@example.com");
        emailRow.MaskedNewValue.Should().NotBeNullOrEmpty().And.NotContain("rotated@example.com");

        var nameRow = await UserFieldRows(context).SingleAsync(row =>
            row.FieldName == "Name" && row.AccessType == FieldAccessType.Write);
        nameRow.IsSensitiveField.Should().BeFalse();
        // Non-sensitive fields keep plain values so write diffs stay useful.
        nameRow.OldValue.Should().Be("Field User");
        nameRow.NewValue.Should().Be("Rotated Name");
    }

    [Fact]
    public async Task AddedEntity_StagesWriteRowsForEveryField_AndDeletedEntity_StagesDeleteRows()
    {
        var options = NewInMemoryOptions();
        var operationAccessor = new UseCaseOperationContextAccessor();
        var tenantId = Guid.NewGuid();
        await using var context = new ApplicationDbContext(options, null, operationAccessor);
        var user = User.CreateOAuthUser("added@example.com", "Added User");

        using (operationAccessor.Begin(new UseCaseOperationContext(
            "operation-code", "TestCommand", tenantId, Guid.NewGuid(), Guid.NewGuid(), null)))
        {
            context.Add(user);
            await context.SaveChangesAsync();
        }

        var addedRows = await UserFieldRows(context)
            .Where(row => row.AccessType == FieldAccessType.Write)
            .ToListAsync();
        addedRows.Should().NotBeEmpty();
        addedRows.Should().Contain(row => row.FieldName == "Email" && row.IsSensitiveField);
        addedRows.Should().Contain(row => row.FieldName == "Name" && !row.IsSensitiveField);

        using (operationAccessor.Begin(new UseCaseOperationContext(
            "operation-code", "TestCommand", tenantId, Guid.NewGuid(), Guid.NewGuid(), null)))
        {
            context.Remove(user);
            await context.SaveChangesAsync();
        }

        var deleteRows = await UserFieldRows(context)
            .Where(row => row.AccessType == FieldAccessType.Delete)
            .ToListAsync();
        deleteRows.Should().NotBeEmpty();
        deleteRows.Should().OnlyContain(row => row.TenantId == tenantId);
    }

    [Fact]
    public async Task TenantScopedEntity_WithoutOperationContext_ResolvesTenantFromTheEntity()
    {
        var options = NewInMemoryOptions();
        await using var context = new ApplicationDbContext(options);
        var tenantId = Guid.NewGuid();
        var member = new TenantMember { TenantId = tenantId, UserId = Guid.NewGuid(), Role = "Member", IsActive = true };
        context.Add(member);
        await context.SaveChangesAsync();

        member.Role = "Admin";
        await context.SaveChangesAsync();

        // The initial Added-stage save already recorded the first Role value; the modification
        // adds the Member→Admin diff row.
        var roleRow = await FieldRows(context).SingleAsync(row =>
            row.EntityType == "TenantMember" && row.FieldName == "Role"
            && row.AccessType == FieldAccessType.Write && row.OldValue == "Member");
        roleRow.TenantId.Should().Be(tenantId);
        roleRow.NewValue.Should().Be("Admin");
    }

    [Fact]
    public async Task GlobalWritesWithoutTenant_ProduceNoFieldRows_AndCaptureNeverRecurses()
    {
        var options = NewInMemoryOptions();
        await using var context = new ApplicationDbContext(options);
        // User carries no TenantId and no operation context is ambient: the write stays covered
        // by the entity-change record and produces no tenant-less field observations.
        var user = User.CreateOAuthUser("global@example.com", "Global User");
        context.Add(user);
        var savedCount = await context.SaveChangesAsync();

        savedCount.Should().Be(1); // callers keep observing business rows only
        (await UserFieldRows(context).CountAsync()).Should().Be(0);

        // Staging field observations directly must not capture the audit store itself.
        context.Set<FieldAccessAudit>().Add(FieldAccessAudit.Create(
            Guid.NewGuid(), Guid.NewGuid(), "User", Guid.NewGuid(), "Email",
            FieldAccessType.Read, true, SensitivityLevel.Confidential, string.Empty, string.Empty));
        var auditOnlyCount = await context.SaveChangesAsync();

        auditOnlyCount.Should().Be(1);
        (await FieldRows(context).CountAsync()).Should().Be(1);
    }

    private static IQueryable<FieldAccessAudit> FieldRows(ApplicationDbContext context) =>
        context.Set<FieldAccessAudit>().IgnoreQueryFilters();

    private static IQueryable<FieldAccessAudit> UserFieldRows(ApplicationDbContext context) =>
        FieldRows(context).Where(row => row.EntityType == "User");

    private static DbContextOptions<ApplicationDbContext> NewInMemoryOptions() =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
}
