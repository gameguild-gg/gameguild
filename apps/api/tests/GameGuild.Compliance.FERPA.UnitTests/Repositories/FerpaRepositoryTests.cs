using FluentAssertions;
using Xunit;

namespace GameGuild.Compliance.FERPA.UnitTests.Repositories;

public sealed class FerpaRepositoryTests
{
    [Fact]
    public async Task EducationRecords_FilterDeletedAndOrderStudentAndDirectoryViews()
    {
        await using var context = FerpaTestDbContext.Create();
        var studentId = Guid.NewGuid();
        var older = CreateRecord(studentId, "Zulu", createdAt: DateTime.UtcNow.AddDays(-2));
        var newer = CreateRecord(studentId, "Alpha", createdAt: DateTime.UtcNow.AddDays(-1));
        var privateRecord = CreateRecord(studentId, "Private", isDirectory: false);
        var deleted = CreateRecord(studentId, "Deleted", deletedAt: DateTime.UtcNow);
        var otherStudent = CreateRecord(Guid.NewGuid(), "Other");
        var repository = new FerpaEducationRecordRepository(context);
        (await repository.AddAsync(older)).Should().BeSameAs(older);
        context.AddRange(newer, privateRecord, deleted, otherStudent);
        await context.SaveChangesAsync();

        var studentRecords = await repository.GetByStudentAsync(studentId);
        var directoryRecords = await repository.GetDirectoryInformationAsync(studentId);

        studentRecords.Select(record => record.Id).Should().Equal(privateRecord.Id, newer.Id, older.Id);
        directoryRecords.Select(record => record.Title).Should().Equal("Alpha", "Zulu");
    }

    [Fact]
    public async Task ActiveConsent_RequiresExactStudentRecipientScopeAndValidWindow()
    {
        await using var context = FerpaTestDbContext.Create();
        var now = DateTime.UtcNow;
        var studentId = Guid.NewGuid();
        var active = CreateConsent(studentId, "Registrar", "grades", now.AddDays(-1), now.AddDays(1));
        var repository = new FerpaDisclosureConsentRepository(context);
        (await repository.AddAsync(active)).Should().BeSameAs(active);
        context.AddRange(
            CreateConsent(studentId, "Other", "grades", now.AddDays(-1), now.AddDays(1)),
            CreateConsent(studentId, "Registrar", "attendance", now.AddDays(-1), now.AddDays(1)),
            CreateConsent(studentId, "Registrar", "grades", now.AddDays(1), now.AddDays(2)),
            CreateConsent(studentId, "Registrar", "grades", now.AddDays(-2), now.AddDays(-1)),
            CreateConsent(studentId, "Registrar", "grades", now.AddDays(-2), now.AddDays(1), revokedAt: now),
            CreateConsent(studentId, "Registrar", "grades", now.AddDays(-2), now.AddDays(1), deletedAt: now));
        await context.SaveChangesAsync();

        var result = await repository.GetActiveAsync(studentId, "Registrar", "grades", now);

        result.Should().BeSameAs(active);
        (await repository.GetByIdAsync(active.Id)).Should().BeSameAs(active);
        active.Purpose = "Updated purpose";
        await repository.UpdateAsync(active);
        context.ChangeTracker.Clear();
        (await repository.GetByIdAsync(active.Id))!.Purpose.Should().Be("Updated purpose");
    }

    [Fact]
    public async Task StudentConsents_FilterDeletedAndOrderByEffectiveDateDescending()
    {
        await using var context = FerpaTestDbContext.Create();
        var studentId = Guid.NewGuid();
        var first = CreateConsent(studentId, "First", "records", DateTime.UtcNow.AddDays(-2), null);
        var second = CreateConsent(studentId, "Second", "records", DateTime.UtcNow.AddDays(-1), null);
        context.AddRange(first, second, CreateConsent(studentId, "Deleted", "records", DateTime.UtcNow, null, deletedAt: DateTime.UtcNow));
        await context.SaveChangesAsync();

        var result = await new FerpaDisclosureConsentRepository(context).GetByStudentAsync(studentId);

        result.Select(consent => consent.Id).Should().Equal(second.Id, first.Id);
    }

    [Fact]
    public async Task DisclosureLogs_FilterDeletedAndOrderNewestFirst()
    {
        await using var context = FerpaTestDbContext.Create();
        var studentId = Guid.NewGuid();
        var older = CreateLog(studentId, DateTime.UtcNow.AddDays(-2));
        var newer = CreateLog(studentId, DateTime.UtcNow.AddDays(-1));
        var deleted = CreateLog(studentId, DateTime.UtcNow);
        deleted.DeletedAt = DateTime.UtcNow;
        var repository = new FerpaDisclosureLogRepository(context);
        (await repository.AddAsync(older)).Should().BeSameAs(older);
        context.AddRange(newer, deleted, CreateLog(Guid.NewGuid(), DateTime.UtcNow));
        await context.SaveChangesAsync();

        var result = await repository.GetByStudentAsync(studentId);

        result.Select(log => log.Id).Should().Equal(newer.Id, older.Id);
    }

    [Fact]
    public async Task PendingInspectionRequests_FilterTerminalAndDeletedThenOrderByDeadline()
    {
        await using var context = FerpaTestDbContext.Create();
        var early = CreateRequest(DateTime.UtcNow.AddDays(5));
        var late = CreateRequest(DateTime.UtcNow.AddDays(20));
        var repository = new FerpaInspectionRequestRepository(context);
        (await repository.AddAsync(late)).Should().BeSameAs(late);
        context.AddRange(
            early,
            CreateRequest(DateTime.UtcNow.AddDays(1), FerpaRequestStatus.Completed),
            CreateRequest(DateTime.UtcNow.AddDays(2), deletedAt: DateTime.UtcNow));
        await context.SaveChangesAsync();

        var result = await repository.GetPendingAsync();

        result.Select(request => request.Id).Should().Equal(early.Id, late.Id);
        (await repository.GetByIdAsync(early.Id)).Should().BeSameAs(early);
        early.Complete(Guid.NewGuid(), "Released");
        await repository.UpdateAsync(early);
        context.ChangeTracker.Clear();
        (await repository.GetByIdAsync(early.Id))!.Status.Should().Be(FerpaRequestStatus.Completed);
    }

    [Fact]
    public async Task DirectoryPolicy_DistinguishesGlobalAndTenantPoliciesAndFiltersDeleted()
    {
        await using var context = FerpaTestDbContext.Create();
        var tenantId = Guid.NewGuid();
        var global = new FerpaDirectoryInformationPolicy { AllowedFieldsJson = "[\"name\"]" };
        var tenant = new FerpaDirectoryInformationPolicy { TenantId = tenantId, AllowedFieldsJson = "[\"avatar\"]" };
        var deletedTenantId = Guid.NewGuid();
        var deleted = new FerpaDirectoryInformationPolicy { TenantId = deletedTenantId, DeletedAt = DateTime.UtcNow };
        var repository = new FerpaDirectoryInformationPolicyRepository(context);
        (await repository.AddAsync(global)).Should().BeSameAs(global);
        context.AddRange(tenant, deleted);
        await context.SaveChangesAsync();

        (await repository.GetByTenantAsync(null)).Should().BeSameAs(global);
        (await repository.GetByTenantAsync(tenantId)).Should().BeSameAs(tenant);
        (await repository.GetByTenantAsync(deletedTenantId)).Should().BeNull();
        tenant.Update("[\"name\",\"avatar\"]", false, DateTime.UtcNow, null);
        await repository.UpdateAsync(tenant);
        context.ChangeTracker.Clear();
        (await repository.GetByTenantAsync(tenantId))!.OptOutEnabled.Should().BeFalse();
    }

    private static FerpaEducationRecord CreateRecord(
        Guid studentId,
        string title,
        bool isDirectory = true,
        DateTime? createdAt = null,
        DateTime? deletedAt = null) => new()
        {
            StudentUserId = studentId,
            ExternalRecordId = Guid.NewGuid().ToString(),
            Title = title,
            IsDirectoryInformation = isDirectory,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            DeletedAt = deletedAt
        };

    private static FerpaDisclosureConsent CreateConsent(
        Guid studentId,
        string recipient,
        string scope,
        DateTime effectiveFrom,
        DateTime? expiresAt,
        DateTime? revokedAt = null,
        DateTime? deletedAt = null) => new()
        {
            StudentUserId = studentId,
            Recipient = recipient,
            Purpose = "Academic review",
            Scope = scope,
            EffectiveFrom = effectiveFrom,
            ExpiresAt = expiresAt,
            RevokedAt = revokedAt,
            DeletedAt = deletedAt
        };

    private static FerpaDisclosureLog CreateLog(Guid studentId, DateTime disclosedAt) => new()
    {
        StudentUserId = studentId,
        DisclosedByUserId = Guid.NewGuid(),
        Recipient = "Registrar",
        Basis = FerpaDisclosureBasis.SchoolOfficial,
        Purpose = "Academic support",
        DisclosedAt = disclosedAt
    };

    private static FerpaInspectionRequest CreateRequest(
        DateTime deadline,
        FerpaRequestStatus status = FerpaRequestStatus.Pending,
        DateTime? deletedAt = null) => new()
        {
            StudentUserId = Guid.NewGuid(),
            RequestedByUserId = Guid.NewGuid(),
            Deadline = deadline,
            Status = status,
            DeletedAt = deletedAt
        };
}
