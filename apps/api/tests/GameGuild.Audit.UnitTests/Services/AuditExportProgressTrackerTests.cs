using FluentAssertions;
using GameGuild.Compliance.Audit;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GameGuild.Tests.Audit.Unit.Services;

public sealed class AuditExportProgressTrackerTests
{
    [Fact]
    public async Task Progress_ShouldBePollableByOwnerAndReportCompletion()
    {
        using var provider = new ServiceCollection()
            .AddDistributedMemoryCache()
            .BuildServiceProvider();
        var tracker = new DistributedAuditExportProgressTracker(provider.GetRequiredService<IDistributedCache>());
        var exportId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        await tracker.BeginAsync(exportId, ownerId, 10, CancellationToken.None);
        await tracker.ReportAsync(
            exportId,
            ownerId,
            recordsWritten: 4,
            AuditExportProgressStatus.InProgress,
            errorMessage: null,
            CancellationToken.None);

        var progress = await tracker.GetAsync(exportId, ownerId, CancellationToken.None);
        progress.Should().NotBeNull();
        progress!.Status.Should().Be(nameof(AuditExportProgressStatus.InProgress));
        progress.TotalRecords.Should().Be(10);
        progress.RecordsWritten.Should().Be(4);
        progress.PercentComplete.Should().Be(40);
        (await tracker.GetAsync(exportId, Guid.NewGuid(), CancellationToken.None)).Should().BeNull();

        await tracker.ReportAsync(
            exportId,
            ownerId,
            recordsWritten: 10,
            AuditExportProgressStatus.Completed,
            errorMessage: null,
            CancellationToken.None);

        var completed = await tracker.GetAsync(exportId, ownerId, CancellationToken.None);
        completed!.Status.Should().Be(nameof(AuditExportProgressStatus.Completed));
        completed.PercentComplete.Should().Be(100);
    }
}
