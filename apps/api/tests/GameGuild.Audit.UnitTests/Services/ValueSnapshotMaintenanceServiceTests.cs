using System.Text.Json;
using GameGuild.Compliance.Audit;
using GameGuild.Identity.Context.Actors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameGuild.Tests.Audit.Unit.Services;

public sealed class ValueSnapshotRetentionServiceTests : ValueSnapshotServiceTestBase
{
    private ValueSnapshotRetentionService Service() => new(
        Actors.Object, Context, TimeProvider.System, NullLogger<ValueSnapshotRetentionService>.Instance);

    [Fact]
    public async Task DryRunRecordsItsDecisionWithoutRemovingSnapshotPayloads()
    {
        var snapshot = SeedSnapshot("Record", "r1", "Added", null, """{"Value":1}""", DateTimeOffset.UtcNow.AddDays(-90));
        await Service().ConfigurePolicyAsync(new ConfigureValueSnapshotRetentionRequest
        {
            RetentionDays = 30, ArchiveAfterDays = 7
        }, CancellationToken.None);

        var result = await Service().EnforceAsync(new EnforceValueSnapshotRetentionRequest { DryRun = true }, CancellationToken.None);

        Assert.True(result.DryRun);
        Assert.Equal(1, result.DeletedCount);
        Context.ChangeTracker.Clear();
        var preserved = await Context.Set<MutationValueSnapshot>().SingleAsync(item => item.Id == snapshot.Id);
        Assert.Equal(snapshot.AfterPayload, preserved.AfterPayload);
        Assert.Null(preserved.ArchivedAtUtc);
        Assert.True(await Context.Set<ValueSnapshotRetentionExecution>().AnyAsync(item => item.Id == result.Id && item.DryRun));
    }

    [Fact]
    public async Task PoliciesRequireValidBoundsAndAnAuthenticatedAdministrator()
    {
        await Assert.ThrowsAsync<ValueSnapshotValidationException>(() => Service().ConfigurePolicyAsync(
            new ConfigureValueSnapshotRetentionRequest { RetentionDays = 1, ArchiveAfterDays = 7 }, CancellationToken.None));
        Assert.Empty(await Context.Set<ValueSnapshotRetentionPolicy>().ToListAsync());
        Actors.SetupGet(accessor => accessor.ActorContext).Returns(ActorContext.Anonymous);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service().GetPolicyAsync(CancellationToken.None));
    }

    private async Task<ValueSnapshotRetentionService> ConfigureAsync(int retentionDays, int archiveAfterDays, DateTime? legalHoldUntilUtc = null)
    {
        var service = Service();
        await service.ConfigurePolicyAsync(new ConfigureValueSnapshotRetentionRequest
        {
            RetentionDays = retentionDays,
            ArchiveAfterDays = archiveAfterDays,
            LegalHoldUntilUtc = legalHoldUntilUtc
        }, CancellationToken.None);
        return service;
    }

    [Fact]
    public async Task EnforceUnderActiveLegalHoldKeepsHeldSnapshotValuesIntact()
    {
        var before = """{"Title":"Held","Secret":"before-value"}""";
        var after = """{"Title":"Held","Secret":"after-value"}""";
        var snapshot = SeedSnapshot("Record", "held-1", "Modified", before, after, DateTimeOffset.UtcNow.AddDays(-30));

        var service = await ConfigureAsync(retentionDays: 30, archiveAfterDays: 7, legalHoldUntilUtc: DateTime.UtcNow.AddDays(10));
        var result = await service.EnforceAsync(new EnforceValueSnapshotRetentionRequest(), CancellationToken.None);

        Assert.True(result.LegalHoldActive);
        Assert.Equal(0, result.DeletedCount);
        Assert.Equal(0, result.ArchivedCount);

        Context.ChangeTracker.Clear();
        var stored = await Context.Set<MutationValueSnapshot>().SingleAsync(item => item.Id == snapshot.Id);
        Assert.Equal(before, stored.BeforePayload);
        Assert.Equal(after, stored.AfterPayload);
        Assert.Null(stored.ArchivedAtUtc);
        Assert.Empty(await Context.Set<MutationValueSnapshotArchive>().ToListAsync());
    }

    [Fact]
    public async Task EnforcePersistsTheArchiveCopyBeforeStrippingTheHotRepresentation()
    {
        var before = """{"Title":"Archived","Secret":"before-value"}""";
        var after = """{"Title":"Archived","Secret":"after-value"}""";
        var snapshot = SeedSnapshot("Record", "arch-1", "Modified", before, after, DateTimeOffset.UtcNow.AddDays(-30));

        var service = await ConfigureAsync(retentionDays: 90, archiveAfterDays: 7);
        var result = await service.EnforceAsync(new EnforceValueSnapshotRetentionRequest(), CancellationToken.None);

        Assert.Equal(1, result.ArchivedCount);
        Context.ChangeTracker.Clear();
        var stored = await Context.Set<MutationValueSnapshot>().SingleAsync(item => item.Id == snapshot.Id);
        Assert.NotNull(stored.ArchivedAtUtc);
        Assert.Null(stored.BeforePayload);
        Assert.Null(stored.AfterPayload);
        Assert.Null(stored.BeforeChunkId);
        Assert.Null(stored.AfterChunkId);
        Assert.NotNull(stored.BeforeIntegritySha256);
        Assert.NotNull(stored.AfterIntegritySha256);

        // The durable archive copy carries the complete stored values and checksums.
        var archive = await Context.Set<MutationValueSnapshotArchive>().SingleAsync(item => item.SnapshotId == snapshot.Id);
        Assert.Equal(stored.TenantId, archive.TenantId);
        Assert.Equal("Modified", archive.ChangeKind);
        Assert.Equal(before, archive.BeforePayload);
        Assert.Equal(after, archive.AfterPayload);
        Assert.Equal(stored.BeforeIntegritySha256, archive.BeforeIntegritySha256);
        Assert.Equal(stored.AfterIntegritySha256, archive.AfterIntegritySha256);
    }

    [Fact]
    public async Task EnforceCarriesChunkContentIntoTheArchiveCopyAndDropsTheChunk()
    {
        var large = """{"Title":"Chunked","Blob":"""" + new string('x', 400) + """"}""";
        var snapshot = SeedSnapshot("Record", "chunk-1", "Added", null, large, DateTimeOffset.UtcNow.AddDays(-30));

        // Deduplicate the payload into a chunk so the hot row only keeps the chunk reference.
        var retention = await ConfigureAsync(retentionDays: 90, archiveAfterDays: 90);
        var (compacted, _) = await retention.CompactAsync(
            (await Context.Set<MutationValueSnapshot>().FirstAsync(item => item.Id == snapshot.Id)).TenantId, CancellationToken.None);
        Assert.Equal(1, compacted);
        Context.ChangeTracker.Clear();
        var chunked = await Context.Set<MutationValueSnapshot>().SingleAsync(item => item.Id == snapshot.Id);
        Assert.Null(chunked.AfterPayload);
        Assert.NotNull(chunked.AfterChunkId);
        var chunkId = chunked.AfterChunkId!.Value;

        // Lower the archive cutoff so the snapshot becomes archivable and enforce.
        var service = await ConfigureAsync(retentionDays: 90, archiveAfterDays: 7);
        var result = await service.EnforceAsync(new EnforceValueSnapshotRetentionRequest(), CancellationToken.None);
        Assert.Equal(1, result.ArchivedCount);

        Context.ChangeTracker.Clear();
        var archive = await Context.Set<MutationValueSnapshotArchive>().SingleAsync(item => item.SnapshotId == snapshot.Id);
        Assert.Equal(large, archive.AfterPayload);
        // The archive copy is the last value representation, so the shared chunk is released.
        Assert.False(await Context.Set<MutationValuePayloadChunk>().AnyAsync(chunk => chunk.Id == chunkId));
    }
}

public sealed class ValueSnapshotArchiveReadabilityTests : ValueSnapshotServiceTestBase
{
    private async Task ArchiveAsync(params MutationValueSnapshot[] snapshots)
    {
        var retention = new ValueSnapshotRetentionService(
            Actors.Object, Context, TimeProvider.System, NullLogger<ValueSnapshotRetentionService>.Instance);
        await retention.ConfigurePolicyAsync(new ConfigureValueSnapshotRetentionRequest
        {
            RetentionDays = 90, ArchiveAfterDays = 7
        }, CancellationToken.None);
        await retention.EnforceAsync(new EnforceValueSnapshotRetentionRequest(), CancellationToken.None);
    }

    [Fact]
    public async Task ArchivedSnapshotsStayReadableThroughDetailSearchAndHistory()
    {
        var t0 = DateTimeOffset.UtcNow.AddDays(-30);
        var added = SeedSnapshot("Order", "o-1", "Added", null, """{"Amount":42,"Note":"golden-ticket"}""", t0);
        SeedSnapshot("Order", "o-1", "Modified", """{"Amount":42}""", """{"Amount":50}""", t0.AddDays(1));
        await ArchiveAsync();

        var query = CreateQueryService();

        var detail = await query.GetAsync(added.Id, CancellationToken.None);
        Assert.NotNull(detail);
        Assert.NotNull(detail.ArchivedAtUtc);
        Assert.Equal("""{"Amount":42,"Note":"golden-ticket"}""", detail.AfterPayload);

        var search = await query.SearchAsync(new ValueSnapshotSearchRequest
        {
            EntityName = "Order", ValuePattern = "golden-ticket"
        }, CancellationToken.None);
        Assert.Equal(1, search.TotalCount);
        Assert.Equal(added.Id, search.Items[0].Id);
        Assert.Contains("golden-ticket", search.Items[0].AfterPayload);

        var history = await query.GetHistoryAsync("Order", "o-1", "Amount", CancellationToken.None);
        Assert.NotNull(history);
        Assert.Equal(2, history.Entries.Count);
        Assert.Equal("42", history.Entries[0].Value);
        Assert.Equal("50", history.Entries[1].Value);
    }

    [Fact]
    public async Task RollbackReconstructsStateFromArchivedPayloads()
    {
        var t0 = DateTimeOffset.UtcNow.AddDays(-30);
        SeedSnapshot("Record", "rb-1", "Added", null, """{"Title":"Original","Count":1}""", t0);
        SeedSnapshot("Record", "rb-1", "Modified", """{"Count":1}""", """{"Count":9}""", t0.AddDays(1));
        await ArchiveAsync();

        var service = new ValueSnapshotRollbackService(
            Actors.Object, Context, TimeProvider.System, NullLogger<ValueSnapshotRollbackService>.Instance);
        var plan = await service.CreatePlanAsync(new CreateValueSnapshotRollbackPlanRequest
        {
            EntityName = "Record", KeyId = "rb-1", AsOfUtc = t0.AddHours(1).UtcDateTime
        }, CancellationToken.None);

        using var state = JsonDocument.Parse(plan.BasisStateJson);
        Assert.Equal("Original", state.RootElement.GetProperty("Title").GetString());
        Assert.Equal(1, state.RootElement.GetProperty("Count").GetInt32());
    }
}

public sealed class ValueSnapshotRollbackServiceTests : ValueSnapshotServiceTestBase
{
    private ValueSnapshotRollbackService Service() => new(
        Actors.Object, Context, TimeProvider.System, NullLogger<ValueSnapshotRollbackService>.Instance);

    [Fact]
    public async Task PlanReconstructsOnlyTheStateAtTheRequestedInstant()
    {
        var initialTime = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var basis = SeedSnapshot("Record", "r1", "Added", null, """{"Title":"Original","Count":1}""", initialTime);
        SeedSnapshot("Record", "r1", "Modified", """{"Title":"Original"}""", """{"Title":"New"}""", initialTime.AddDays(1));

        var plan = await Service().CreatePlanAsync(new CreateValueSnapshotRollbackPlanRequest
        {
            EntityName = "Record", KeyId = "r1", AsOfUtc = initialTime.AddHours(1).UtcDateTime
        }, CancellationToken.None);

        Assert.Equal(basis.Id, plan.BasisSnapshotId);
        Assert.Equal("Proposed", plan.Status);
        using var state = JsonDocument.Parse(plan.BasisStateJson);
        Assert.Equal("Original", state.RootElement.GetProperty("Title").GetString());
        Assert.Equal(1, state.RootElement.GetProperty("Count").GetInt32());
        Assert.Equal(plan.BasisStateSha256, (await Service().GetPlanAsync(plan.Id, CancellationToken.None))!.BasisStateSha256);
        Assert.Equal(2, await Context.Set<MutationValueSnapshot>().CountAsync());
    }

    [Fact]
    public async Task PlanRejectsMissingHistoryAndAnonymousActors()
    {
        await Assert.ThrowsAsync<ValueSnapshotRollbackException>(() => Service().CreatePlanAsync(new CreateValueSnapshotRollbackPlanRequest
        {
            EntityName = "Record", KeyId = "missing", AsOfUtc = DateTime.UtcNow
        }, CancellationToken.None));
        Assert.Empty(await Context.Set<ValueSnapshotRollbackPlan>().ToListAsync());
        Actors.SetupGet(accessor => accessor.ActorContext).Returns(ActorContext.Anonymous);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service().GetPlanAsync(Guid.NewGuid(), CancellationToken.None));
    }
}
