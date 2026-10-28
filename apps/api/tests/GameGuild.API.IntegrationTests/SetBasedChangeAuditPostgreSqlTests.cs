using System.Text.Json;
using GameGuild.API.Database;
using GameGuild.API.Eventing;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Commerce.Products;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GameGuild.API.IntegrationTests;

/// <summary>
///     Hosted-PostgreSQL coverage for universal set-based audit capture (issue #166 remainder):
///     <c>ExecuteUpdate</c>/<c>ExecuteDelete</c> statements — the guarded promo counter
///     increments and the bulk notification mutations cited in the reopen — must produce
///     transactionally consistent before/after evidence into <see cref="EntityChangeAuditRecord"/>,
///     roll back together with the business change, keep the guarded limits and idempotency
///     intact, redact sensitive values, and leave the tracked-entity capture untouched.
/// </summary>
[Collection(ApiPostgreSqlCollection.Name)]
public sealed class SetBasedChangeAuditPostgreSqlTests(ApiPostgreSqlFixture fixture)
{
    [Fact]
    public async Task GuardedTotalUsageIncrementStagesTransactionalBeforeAfterAudit()
    {
        var accessor = new UseCaseOperationContextAccessor();
        var (actorId, tenantId, correlationId) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await using var db = CreateContext(accessor);
        var (user, promoCode) = await SeedActivePromoCodeAsync(db, maxUses: 5, currentUses: 2, tenantId);

        using (accessor.Begin(Operation(tenantId, actorId, correlationId)))
        {
            var repository = new PromoCodeRepository(db);
            Assert.True(await repository.TryIncrementTotalUsageAsync(promoCode.Id, promoCode.MaxUses));
            // The recorder flow saves inside the same transaction; mirror it here.
            await db.SaveChangesAsync();
        }

        await using var observed = CreateContext();
        var code = await observed.Set<PromoCode>().IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(row => row.Id == promoCode.Id);
        Assert.Equal(3, code.CurrentUses);

        var record = Assert.Single(await Records(observed, correlationId).ToListAsync());
        Assert.Equal(nameof(PromoCode), record.EntityName);
        Assert.Equal(promoCode.Id.ToString(), record.KeyId);
        Assert.Equal(EntityState.Modified.ToString(), record.ChangeKind);
        Assert.Equal<Guid?>(actorId, record.ActorId);
        Assert.Equal<Guid?>(tenantId, record.TenantId);
        Assert.Equal(2, Number(record.BeforeValues, nameof(PromoCode.CurrentUses)));
        Assert.Equal(3, Number(record.AfterValues, nameof(PromoCode.CurrentUses)));
    }

    [Fact]
    public async Task GuardedTotalUsageIncrementAtLimitFailsClosedAndAuditsNothing()
    {
        var accessor = new UseCaseOperationContextAccessor();
        var (actorId, tenantId, correlationId) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await using var db = CreateContext(accessor);
        var (_, promoCode) = await SeedActivePromoCodeAsync(db, maxUses: 3, currentUses: 3, tenantId);

        using (accessor.Begin(Operation(tenantId, actorId, correlationId)))
        {
            var repository = new PromoCodeRepository(db);
            Assert.False(await repository.TryIncrementTotalUsageAsync(promoCode.Id, promoCode.MaxUses));
            await db.SaveChangesAsync();
        }

        await using var observed = CreateContext();
        var code = await observed.Set<PromoCode>().IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(row => row.Id == promoCode.Id);
        Assert.Equal(3, code.CurrentUses);
        Assert.Empty(await Records(observed, correlationId).ToListAsync());
    }

    [Fact]
    public async Task RolledBackSetBasedIncrementLeavesNoBusinessOrAuditRows()
    {
        var accessor = new UseCaseOperationContextAccessor();
        var (actorId, tenantId, correlationId) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await using var db = CreateContext(accessor);
        var (_, promoCode) = await SeedActivePromoCodeAsync(db, maxUses: 5, currentUses: 1, tenantId);

        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            using var operation = accessor.Begin(Operation(tenantId, actorId, correlationId));
            var repository = new PromoCodeRepository(db);
            Assert.True(await repository.TryIncrementTotalUsageAsync(promoCode.Id, promoCode.MaxUses));
            await db.SaveChangesAsync();
            // The staged audit rows are written inside the transaction: rolling back must
            // leave neither the counter change nor any audit evidence (no partial audit).
            await transaction.RollbackAsync();
        }

        await using var observed = CreateContext();
        var code = await observed.Set<PromoCode>().IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(row => row.Id == promoCode.Id);
        Assert.Equal(1, code.CurrentUses);
        Assert.Empty(await Records(observed, correlationId).ToListAsync());
    }

    [Fact]
    public async Task UsageRecorderCapturesCounterDeltasAndStaysIdempotent()
    {
        // The reopen's demonstrated business path: PromoCodeUsageRecorder.RecordAsync runs both
        // guarded increments inside one transaction; the total counter delta must now be audited
        // while the recording stays atomic, guarded and idempotent.
        var accessor = new UseCaseOperationContextAccessor();
        var (actorId, tenantId, correlationId) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await using var db = CreateContext(accessor);
        var (user, promoCode) = await SeedActivePromoCodeAsync(db, maxUses: 5, currentUses: 0, tenantId);
        var repository = new PromoCodeRepository(db);
        var recorder = new PromoCodeUsageRecorder(repository);
        var idempotencyKey = "sb-audit-" + Guid.NewGuid().ToString("N");

        using (accessor.Begin(Operation(tenantId, actorId, correlationId)))
        {
            var (status, usage) = await recorder.RecordAsync(promoCode, user.Id, 5m, "USD", idempotencyKey);
            Assert.Equal(PromoUsageRecordingStatus.Recorded, status);
            Assert.NotNull(usage);
        }

        // A replayed idempotency key never counts twice and never audits twice.
        var (replayStatus, replayUsage) = await recorder.RecordAsync(promoCode, user.Id, 5m, "USD", idempotencyKey);
        Assert.Equal(PromoUsageRecordingStatus.Replay, replayStatus);
        Assert.NotNull(replayUsage);

        await using var observed = CreateContext();
        var code = await observed.Set<PromoCode>().IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(row => row.Id == promoCode.Id);
        Assert.Equal(1, code.CurrentUses);
        Assert.Equal(1, await observed.Set<PromoCodeUse>().AsNoTracking().CountAsync(row => row.PromoCodeId == promoCode.Id));

        var counterDelta = await Records(observed, correlationId)
            .Where(row => row.EntityName == nameof(PromoCode))
            .ToListAsync();
        var totalIncrement = Assert.Single(counterDelta);
        Assert.Equal(EntityState.Modified.ToString(), totalIncrement.ChangeKind);
        Assert.Equal(0, Number(totalIncrement.BeforeValues, nameof(PromoCode.CurrentUses)));
        Assert.Equal(1, Number(totalIncrement.AfterValues, nameof(PromoCode.CurrentUses)));

        // The first-use per-user counter insert is a tracked addition captured by the existing pass.
        var counterInsert = await Records(observed, correlationId)
            .Where(row => row.EntityName == nameof(PromoCodeUserUsageCounter))
            .ToListAsync();
        var perUser = Assert.Single(counterInsert);
        Assert.Equal(EntityState.Added.ToString(), perUser.ChangeKind);
        Assert.Null(perUser.BeforeValues);
        Assert.Equal(1, Number(perUser.AfterValues, nameof(PromoCodeUserUsageCounter.UseCount)));
    }

    [Fact]
    public async Task PerUserGuardedIncrementAuditsExistingCounterDelta()
    {
        var accessor = new UseCaseOperationContextAccessor();
        var (actorId, tenantId, correlationId) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await using var db = CreateContext(accessor);
        var (user, promoCode) = await SeedActivePromoCodeAsync(db, maxUses: 5, currentUses: 1, tenantId);
        db.Set<PromoCodeUserUsageCounter>().Add(new PromoCodeUserUsageCounter
        {
            PromoCodeId = promoCode.Id,
            UserId = user.Id,
            UseCount = 1
        });
        await db.SaveChangesAsync();

        using (accessor.Begin(Operation(tenantId, actorId, correlationId)))
        {
            var repository = new PromoCodeRepository(db);
            Assert.True(await repository.TryIncrementUserUsageAsync(promoCode.Id, user.Id, maxUsesPerUser: 3));
            await db.SaveChangesAsync();
        }

        await using var observed = CreateContext();
        var record = Assert.Single(await Records(observed, correlationId).ToListAsync());
        Assert.Equal(nameof(PromoCodeUserUsageCounter), record.EntityName);
        Assert.Equal(promoCode.Id + ":" + user.Id, record.KeyId);
        Assert.Equal(EntityState.Modified.ToString(), record.ChangeKind);
        Assert.Equal(1, Number(record.BeforeValues, nameof(PromoCodeUserUsageCounter.UseCount)));
        Assert.Equal(2, Number(record.AfterValues, nameof(PromoCodeUserUsageCounter.UseCount)));
    }

    [Fact]
    public async Task BulkDeleteCapturesBeforeImagesWithDeletedKind()
    {
        var accessor = new UseCaseOperationContextAccessor();
        var (actorId, tenantId, correlationId) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await using var db = CreateContext(accessor);
        var user = await SeedUserAsync(db);
        var keep = NewNotification(user.Id, "keep", archived: false, daysOld: 1);
        var oldArchivedOne = NewNotification(user.Id, "old-1", archived: true, daysOld: 100);
        var oldArchivedTwo = NewNotification(user.Id, "old-2", archived: true, daysOld: 120);
        db.Set<UserNotification>().AddRange(keep, oldArchivedOne, oldArchivedTwo);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        using (accessor.Begin(Operation(tenantId, actorId, correlationId)))
        {
            var cutoff = DateTime.UtcNow.AddDays(-90);
            var deleted = await db.Set<UserNotification>()
                .Where(n => n.UserId == user.Id && n.IsArchived && n.ArchivedAt < cutoff)
                .ExecuteDeleteAsync();
            Assert.Equal(2, deleted);
            await db.SaveChangesAsync();
        }

        await using var observed = CreateContext();
        Assert.Equal(1, await observed.Set<UserNotification>().IgnoreQueryFilters().CountAsync(row => row.UserId == user.Id));
        var records = await Records(observed, correlationId).ToListAsync();
        Assert.Equal(2, records.Count);
        Assert.All(records, record =>
        {
            Assert.Equal(nameof(UserNotification), record.EntityName);
            Assert.Equal(EntityState.Deleted.ToString(), record.ChangeKind);
            Assert.Null(record.AfterValues);
            Assert.NotNull(record.BeforeValues);
            Assert.True(Bool(record.BeforeValues, nameof(UserNotification.IsArchived)));
        });
        Assert.Equal(
            new[] { oldArchivedOne.Id.ToString(), oldArchivedTwo.Id.ToString() }.OrderBy(id => id),
            records.Select(record => record.KeyId).OrderBy(id => id));
    }

    [Fact]
    public async Task SensitiveValuesAreRedactedInSetBasedEvidence()
    {
        var accessor = new UseCaseOperationContextAccessor();
        var (actorId, tenantId, correlationId) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await using var db = CreateContext(accessor);
        var user = await SeedUserAsync(db);
        var replacementHash = "sha512:" + Guid.NewGuid().ToString("N");

        using (accessor.Begin(Operation(tenantId, actorId, correlationId)))
        {
            await db.Set<User>()
                .Where(row => row.Id == user.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(row => row.Name, "Renamed")
                    .SetProperty(row => row.PasswordHash, replacementHash));
            await db.SaveChangesAsync();
        }

        await using var observed = CreateContext();
        var record = Assert.Single(await Records(observed, correlationId).ToListAsync());
        Assert.Equal(EntityState.Modified.ToString(), record.ChangeKind);
        Assert.Equal("[REDACTED]", Text(record.BeforeValues, nameof(User.PasswordHash)));
        Assert.Equal("[REDACTED]", Text(record.AfterValues, nameof(User.PasswordHash)));
        Assert.Equal("Renamed", Text(record.AfterValues, nameof(User.Name)));
        Assert.DoesNotContain(replacementHash, (record.BeforeValues ?? string.Empty) + record.AfterValues);
    }

    [Fact]
    public async Task TrackedAndSetBasedCaptureStaySingularAndCountStable()
    {
        // Regression for the tracked half: one save mixing a tracked modification with a
        // set-based update captures each exactly once, and SaveChanges keeps reporting
        // business rows only (the staged set-based audit rows are deducted).
        var accessor = new UseCaseOperationContextAccessor();
        var (actorId, tenantId, correlationId) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await using var db = CreateContext(accessor);
        var (user, promoCode) = await SeedActivePromoCodeAsync(db, maxUses: 5, currentUses: 0, tenantId);

        using (accessor.Begin(Operation(tenantId, actorId, correlationId)))
        {
            // Re-attach: the seed helper clears the tracker after saving.
            var trackedUser = await db.Set<User>().SingleAsync(row => row.Id == user.Id);
            trackedUser.Name = "Tracked Change";
            var repository = new PromoCodeRepository(db);
            Assert.True(await repository.TryIncrementTotalUsageAsync(promoCode.Id, promoCode.MaxUses));
            var saved = await db.SaveChangesAsync();
            // One tracked business row; the set-based statement reported its own count already
            // and the two staged audit rows (record + snapshot factory absent here) are deducted.
            Assert.Equal(1, saved);
        }

        await using var observed = CreateContext();
        var records = await Records(observed, correlationId).ToListAsync();
        Assert.Equal(2, records.Count);
        Assert.Single(records, record => record.EntityName == nameof(User)
            && record.ChangeKind == EntityState.Modified.ToString()
            && Text(record.AfterValues, nameof(User.Name)) == "Tracked Change");
        Assert.Single(records, record => record.EntityName == nameof(PromoCode)
            && record.ChangeKind == EntityState.Modified.ToString()
            && Number(record.AfterValues, nameof(PromoCode.CurrentUses)) == 1);
    }

    [Fact]
    public async Task AuditStoreSetBasedWritesAreNeverRecaptured()
    {
        var accessor = new UseCaseOperationContextAccessor();
        var (actorId, tenantId, correlationId) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await using var db = CreateContext(accessor);
        var (user, promoCode) = await SeedActivePromoCodeAsync(db, maxUses: 5, currentUses: 0, tenantId);
        using (accessor.Begin(Operation(tenantId, actorId, correlationId)))
        {
            await new PromoCodeRepository(db).TryIncrementTotalUsageAsync(promoCode.Id, promoCode.MaxUses);
            await db.SaveChangesAsync();
        }

        var auditRows = await db.Set<EntityChangeAuditRecord>().IgnoreQueryFilters().AsNoTracking()
            .Where(row => row.EntityName == nameof(PromoCode)).ToListAsync();
        var target = auditRows[0];

        // Mutating the audit store itself through a set-based statement must not recurse.
        var secondCorrelation = Guid.NewGuid();
        using (accessor.Begin(Operation(tenantId, actorId, secondCorrelation)))
        {
            await db.Set<EntityChangeAuditRecord>()
                .Where(row => row.Id == target.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.CorrelationId, "manual-fix"));
            await db.SaveChangesAsync();
        }

        await using var observed = CreateContext();
        Assert.Empty(await Records(observed, secondCorrelation).ToListAsync());
        Assert.Equal("manual-fix", await observed.Set<EntityChangeAuditRecord>().IgnoreQueryFilters().AsNoTracking()
            .Where(row => row.Id == target.Id).Select(row => row.CorrelationId).SingleAsync());
    }

    private ApplicationDbContext CreateContext(UseCaseOperationContextAccessor? accessor = null)
    {
        accessor ??= new UseCaseOperationContextAccessor();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(new SetBasedChangeAuditInterceptor(
                accessor,
                Options.Create(new EntityChangeAuditOptions())))
            .Options;
        var db = new ApplicationDbContext(options, null, accessor);
        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", db.Database.ProviderName);
        return db;
    }

    private async Task<User> SeedUserAsync(ApplicationDbContext db)
    {
        var marker = Guid.NewGuid().ToString("N");
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "setbased-" + marker + "@example.test",
            Username = "sb_" + marker,
            Name = "Before",
            PasswordHash = "sha512:initial"
        };
        db.Set<User>().Add(user);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return user;
    }

    private async Task<(User User, PromoCode PromoCode)> SeedActivePromoCodeAsync(
        ApplicationDbContext db, int maxUses, int currentUses, Guid tenantId)
    {
        var user = await SeedUserAsync(db);
        var promoCode = new PromoCode
        {
            Id = Guid.NewGuid(),
            Code = "SB" + Guid.NewGuid().ToString("N")[..12],
            Name = "Set-based audit probe",
            Type = PromoCodeType.PercentageOff,
            DiscountPercentage = 10m,
            MaxUses = maxUses,
            CurrentUses = currentUses,
            IsActive = true,
            CreatedBy = user.Id,
            TenantId = tenantId
        };
        db.Set<PromoCode>().Add(promoCode);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return (user, promoCode);
    }

    private static UserNotification NewNotification(
        Guid userId, string title, bool archived, int daysOld) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = "billing",
            Title = title,
            Content = "content",
            IsRead = false,
            IsArchived = archived,
            ArchivedAt = archived ? DateTime.UtcNow.AddDays(-daysOld) : null,
            CreatedAt = DateTime.UtcNow.AddDays(-daysOld - 1)
        };

    private static UseCaseOperationContext Operation(Guid tenantId, Guid actorId, Guid correlationId) =>
        new("setbased-audit", "SetBasedAuditProbeCommand", tenantId, actorId, correlationId, null);

    private static IQueryable<EntityChangeAuditRecord> Records(ApplicationDbContext db, Guid correlationId) =>
        db.Set<EntityChangeAuditRecord>().IgnoreQueryFilters().AsNoTracking()
            .Where(row => row.CorrelationId == correlationId.ToString());

    private static JsonElement Element(string? values, string property)
    {
        Assert.NotNull(values);
        using var snapshot = JsonDocument.Parse(values!);
        Assert.True(snapshot.RootElement.TryGetProperty(property, out var value), property + " missing from " + values);
        return value.Clone();
    }

    private static int? Number(string? values, string property) =>
        Element(values, property).ValueKind == JsonValueKind.Null ? null : Element(values, property).GetInt32();

    private static string? Text(string? values, string property) =>
        Element(values, property).ValueKind == JsonValueKind.Null ? null : Element(values, property).GetString();

    private static bool? Bool(string? values, string property) =>
        Element(values, property).ValueKind == JsonValueKind.Null ? null : Element(values, property).GetBoolean();
}
