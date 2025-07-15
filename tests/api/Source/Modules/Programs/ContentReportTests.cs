using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Programs;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GameGuild.Tests.Modules.Programs;

/// <summary>
/// Comprehensive tests for Content Reporting system functionality
/// </summary>
public class ContentReportTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly IContentReportService _reportService;

    public ContentReportTests()
    {
        var services = new ServiceCollection();
        
        // Add database context
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add Content Report service
        services.AddScoped<IContentReportService, ContentReportService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _reportService = _serviceProvider.GetRequiredService<IContentReportService>();
    }

    [Fact]
    public async Task ContentReport_Entity_Can_Be_Created_And_Saved()
    {
        // Arrange
        var reporterId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        await SeedTestData(reporterId, contentId);

        var report = new ContentReport
        {
            Id = Guid.NewGuid(),
            ContentId = contentId,
            ReporterId = reporterId,
            ReportType = ReportType.InappropriateContent,
            Status = ReportStatus.Pending,
            Description = "This content contains inappropriate language",
            ReportedDate = DateTime.UtcNow
        };

        // Act
        _context.ContentReports.Add(report);
        await _context.SaveChangesAsync();

        // Assert
        var savedReport = await _context.ContentReports.FindAsync(report.Id);
        Assert.NotNull(savedReport);
        Assert.Equal(contentId, savedReport.ContentId);
        Assert.Equal(reporterId, savedReport.ReporterId);
        Assert.Equal(ReportType.InappropriateContent, savedReport.ReportType);
        Assert.Equal(ReportStatus.Pending, savedReport.Status);
        Assert.Equal("This content contains inappropriate language", savedReport.Description);
    }

    [Fact]
    public async Task ReportService_Can_Create_Content_Report()
    {
        // Arrange
        var reporterId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        await SeedTestData(reporterId, contentId);

        var reportRequest = new CreateReportRequest
        {
            ContentId = contentId,
            ReporterId = reporterId,
            ReportType = ReportType.Copyright,
            Description = "This content violates copyright laws",
            Evidence = "URL to original copyrighted material: https://example.com/original"
        };

        // Act
        var report = await _reportService.CreateReportAsync(reportRequest);

        // Assert
        Assert.NotNull(report);
        Assert.Equal(contentId, report.ContentId);
        Assert.Equal(reporterId, report.ReporterId);
        Assert.Equal(ReportType.Copyright, report.ReportType);
        Assert.Equal(ReportStatus.Pending, report.Status);
        Assert.Equal("This content violates copyright laws", report.Description);
        Assert.Equal("URL to original copyrighted material: https://example.com/original", report.Evidence);
        Assert.True(report.ReportedDate <= DateTime.UtcNow);
    }

    [Fact]
    public async Task ReportService_Can_Get_Reports_By_Content()
    {
        // Arrange
        var contentId = Guid.NewGuid();
        var reporter1Id = Guid.NewGuid();
        var reporter2Id = Guid.NewGuid();

        await SeedTestData(reporter1Id, contentId);
        await SeedTestUser(reporter2Id, "Reporter 2");

        // Create multiple reports for the same content
        await _reportService.CreateReportAsync(new CreateReportRequest
        {
            ContentId = contentId,
            ReporterId = reporter1Id,
            ReportType = ReportType.InappropriateContent,
            Description = "Contains offensive language"
        });

        await _reportService.CreateReportAsync(new CreateReportRequest
        {
            ContentId = contentId,
            ReporterId = reporter2Id,
            ReportType = ReportType.Spam,
            Description = "Promotional spam content"
        });

        // Act
        var reports = await _reportService.GetReportsByContentAsync(contentId);

        // Assert
        Assert.NotNull(reports);
        var reportList = reports.ToList();
        Assert.Equal(2, reportList.Count);
        Assert.All(reportList, r => Assert.Equal(contentId, r.ContentId));
        
        var inappropriateReport = reportList.FirstOrDefault(r => r.ReportType == ReportType.InappropriateContent);
        var spamReport = reportList.FirstOrDefault(r => r.ReportType == ReportType.Spam);
        
        Assert.NotNull(inappropriateReport);
        Assert.NotNull(spamReport);
    }

    [Fact]
    public async Task ReportService_Can_Get_Reports_By_Reporter()
    {
        // Arrange
        var reporterId = Guid.NewGuid();
        await SeedTestUser(reporterId, "Active Reporter");

        // Create multiple reports by the same reporter
        for (int i = 0; i < 3; i++)
        {
            var contentId = Guid.NewGuid();
            await SeedTestContent(contentId, $"Content {i + 1}");

            await _reportService.CreateReportAsync(new CreateReportRequest
            {
                ContentId = contentId,
                ReporterId = reporterId,
                ReportType = ReportType.InappropriateContent,
                Description = $"Report {i + 1} description"
            });
        }

        // Act
        var reports = await _reportService.GetReportsByReporterAsync(reporterId);

        // Assert
        Assert.NotNull(reports);
        var reportList = reports.ToList();
        Assert.Equal(3, reportList.Count);
        Assert.All(reportList, r => Assert.Equal(reporterId, r.ReporterId));
    }

    [Fact]
    public async Task ReportService_Can_Update_Report_Status()
    {
        // Arrange
        var reporterId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        await SeedTestData(reporterId, contentId);

        var report = await _reportService.CreateReportAsync(new CreateReportRequest
        {
            ContentId = contentId,
            ReporterId = reporterId,
            ReportType = ReportType.Misinformation,
            Description = "Contains false information"
        });

        var moderatorId = Guid.NewGuid();
        await SeedTestUser(moderatorId, "Moderator");

        // Act
        var updatedReport = await _reportService.UpdateReportStatusAsync(
            report.Id,
            ReportStatus.UnderReview,
            moderatorId,
            "Report is being investigated"
        );

        // Assert
        Assert.NotNull(updatedReport);
        Assert.Equal(ReportStatus.UnderReview, updatedReport.Status);
        Assert.Equal(moderatorId, updatedReport.ModeratorId);
        Assert.Equal("Report is being investigated", updatedReport.ModeratorNotes);
        Assert.True(updatedReport.ReviewedDate.HasValue);
    }

    [Fact]
    public async Task ReportService_Can_Resolve_Report()
    {
        // Arrange
        var reporterId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var moderatorId = Guid.NewGuid();
        
        await SeedTestData(reporterId, contentId);
        await SeedTestUser(moderatorId, "Moderator");

        var report = await _reportService.CreateReportAsync(new CreateReportRequest
        {
            ContentId = contentId,
            ReporterId = reporterId,
            ReportType = ReportType.TechnicalIssue,
            Description = "Video won't load properly"
        });

        // Act
        var resolvedReport = await _reportService.ResolveReportAsync(
            report.Id,
            moderatorId,
            ReportResolution.ContentFixed,
            "Video encoding issue has been resolved"
        );

        // Assert
        Assert.NotNull(resolvedReport);
        Assert.Equal(ReportStatus.Resolved, resolvedReport.Status);
        Assert.Equal(moderatorId, resolvedReport.ModeratorId);
        Assert.Equal(ReportResolution.ContentFixed, resolvedReport.Resolution);
        Assert.Equal("Video encoding issue has been resolved", resolvedReport.ModeratorNotes);
        Assert.True(resolvedReport.ResolvedDate.HasValue);
    }

    [Fact]
    public async Task ReportService_Can_Dismiss_Report()
    {
        // Arrange
        var reporterId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var moderatorId = Guid.NewGuid();
        
        await SeedTestData(reporterId, contentId);
        await SeedTestUser(moderatorId, "Moderator");

        var report = await _reportService.CreateReportAsync(new CreateReportRequest
        {
            ContentId = contentId,
            ReporterId = reporterId,
            ReportType = ReportType.InappropriateContent,
            Description = "I don't like this content"
        });

        // Act
        var dismissedReport = await _reportService.DismissReportAsync(
            report.Id,
            moderatorId,
            "Content is appropriate and follows community guidelines"
        );

        // Assert
        Assert.NotNull(dismissedReport);
        Assert.Equal(ReportStatus.Dismissed, dismissedReport.Status);
        Assert.Equal(moderatorId, dismissedReport.ModeratorId);
        Assert.Equal(ReportResolution.NoActionRequired, dismissedReport.Resolution);
        Assert.Equal("Content is appropriate and follows community guidelines", dismissedReport.ModeratorNotes);
        Assert.True(dismissedReport.ResolvedDate.HasValue);
    }

    [Fact]
    public async Task ReportService_Can_Get_Pending_Reports()
    {
        // Arrange
        await SeedMultipleReports();

        // Act
        var pendingReports = await _reportService.GetPendingReportsAsync();

        // Assert
        Assert.NotNull(pendingReports);
        var reportList = pendingReports.ToList();
        Assert.True(reportList.Count >= 2); // Should have at least 2 pending from seed
        Assert.All(reportList, r => Assert.Equal(ReportStatus.Pending, r.Status));
    }

    [Fact]
    public async Task ReportService_Can_Get_Reports_By_Type()
    {
        // Arrange
        await SeedMultipleReports();

        // Act
        var copyrightReports = await _reportService.GetReportsByTypeAsync(ReportType.Copyright);

        // Assert
        Assert.NotNull(copyrightReports);
        var reportList = copyrightReports.ToList();
        Assert.True(reportList.Count >= 1);
        Assert.All(reportList, r => Assert.Equal(ReportType.Copyright, r.ReportType));
    }

    [Fact]
    public async Task ReportService_Can_Get_Report_Statistics()
    {
        // Arrange
        await SeedMultipleReports();

        // Act
        var stats = await _reportService.GetReportStatisticsAsync(
            DateTime.UtcNow.AddDays(-7),
            DateTime.UtcNow.AddDays(1)
        );

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.TotalReports >= 5); // Should have at least 5 from seed
        Assert.True(stats.PendingReports >= 2);
        Assert.True(stats.ResolvedReports >= 1);
        Assert.True(stats.DismissedReports >= 1);
        Assert.Contains(stats.ReportsByType, kvp => kvp.Key == ReportType.InappropriateContent);
        Assert.Contains(stats.ReportsByType, kvp => kvp.Key == ReportType.Copyright);
    }

    [Fact]
    public async Task ReportService_Can_Escalate_Report()
    {
        // Arrange
        var reporterId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var moderatorId = Guid.NewGuid();
        
        await SeedTestData(reporterId, contentId);
        await SeedTestUser(moderatorId, "Moderator");

        var report = await _reportService.CreateReportAsync(new CreateReportRequest
        {
            ContentId = contentId,
            ReporterId = reporterId,
            ReportType = ReportType.HateSpeech,
            Description = "Contains hate speech and threats"
        });

        // Act
        var escalatedReport = await _reportService.EscalateReportAsync(
            report.Id,
            moderatorId,
            ReportPriority.High,
            "Report contains serious violations requiring immediate attention"
        );

        // Assert
        Assert.NotNull(escalatedReport);
        Assert.Equal(ReportStatus.Escalated, escalatedReport.Status);
        Assert.Equal(ReportPriority.High, escalatedReport.Priority);
        Assert.Equal(moderatorId, escalatedReport.ModeratorId);
        Assert.Equal("Report contains serious violations requiring immediate attention", escalatedReport.ModeratorNotes);
        Assert.True(escalatedReport.EscalatedDate.HasValue);
    }

    [Fact]
    public async Task ReportService_Can_Prevent_Duplicate_Reports()
    {
        // Arrange
        var reporterId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        await SeedTestData(reporterId, contentId);

        await _reportService.CreateReportAsync(new CreateReportRequest
        {
            ContentId = contentId,
            ReporterId = reporterId,
            ReportType = ReportType.Spam,
            Description = "This is spam content"
        });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _reportService.CreateReportAsync(new CreateReportRequest
            {
                ContentId = contentId,
                ReporterId = reporterId,
                ReportType = ReportType.Spam,
                Description = "Another spam report"
            })
        );
    }

    [Fact]
    public async Task ReportService_Can_Get_Content_Report_Summary()
    {
        // Arrange
        var contentId = Guid.NewGuid();
        await SeedTestContent(contentId, "Controversial Content");

        // Create multiple reports for the same content
        for (int i = 0; i < 3; i++)
        {
            var reporterId = Guid.NewGuid();
            await SeedTestUser(reporterId, $"Reporter {i + 1}");

            await _reportService.CreateReportAsync(new CreateReportRequest
            {
                ContentId = contentId,
                ReporterId = reporterId,
                ReportType = ReportType.InappropriateContent,
                Description = $"Inappropriate content report {i + 1}"
            });
        }

        // Act
        var summary = await _reportService.GetContentReportSummaryAsync(contentId);

        // Assert
        Assert.NotNull(summary);
        Assert.Equal(contentId, summary.ContentId);
        Assert.Equal(3, summary.TotalReports);
        Assert.Equal(3, summary.PendingReports);
        Assert.Equal(0, summary.ResolvedReports);
        Assert.Contains(summary.ReportTypes, rt => rt == ReportType.InappropriateContent);
        Assert.True(summary.RequiresAttention); // Should be true with 3 reports
    }

    #region Helper Methods

    private async Task SeedTestData(Guid reporterId, Guid contentId)
    {
        await SeedTestUser(reporterId, "Test Reporter");
        await SeedTestContent(contentId, "Test Content");
    }

    private async Task SeedTestUser(Guid userId, string name)
    {
        var user = new User
        {
            Id = userId,
            Name = name,
            Email = $"test_{userId:N}@example.com",
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }

    private async Task SeedTestContent(Guid contentId, string title)
    {
        // Note: This would typically be a Content entity
        // For now, we'll use a placeholder approach with a Program
        var content = new Program
        {
            Id = contentId,
            Title = title,
            Description = $"Description for {title}",
            Visibility = AccessLevel.Public,
            AuthorId = Guid.NewGuid()
        };

        _context.Programs.Add(content);
        await _context.SaveChangesAsync();
    }

    private async Task SeedMultipleReports()
    {
        var reportTypes = new[]
        {
            ReportType.InappropriateContent,
            ReportType.Copyright,
            ReportType.Spam,
            ReportType.TechnicalIssue,
            ReportType.Misinformation
        };

        var statuses = new[]
        {
            ReportStatus.Pending,
            ReportStatus.Pending,
            ReportStatus.UnderReview,
            ReportStatus.Resolved,
            ReportStatus.Dismissed
        };

        for (int i = 0; i < 5; i++)
        {
            var reporterId = Guid.NewGuid();
            var contentId = Guid.NewGuid();
            
            await SeedTestUser(reporterId, $"Reporter {i + 1}");
            await SeedTestContent(contentId, $"Content {i + 1}");

            var report = new ContentReport
            {
                Id = Guid.NewGuid(),
                ContentId = contentId,
                ReporterId = reporterId,
                ReportType = reportTypes[i],
                Status = statuses[i],
                Description = $"Test report {i + 1}",
                ReportedDate = DateTime.UtcNow.AddDays(-i)
            };

            if (statuses[i] != ReportStatus.Pending)
            {
                var moderatorId = Guid.NewGuid();
                await SeedTestUser(moderatorId, $"Moderator {i + 1}");
                
                report.ModeratorId = moderatorId;
                report.ReviewedDate = DateTime.UtcNow.AddDays(-i + 1);
                
                if (statuses[i] == ReportStatus.Resolved || statuses[i] == ReportStatus.Dismissed)
                {
                    report.ResolvedDate = DateTime.UtcNow.AddDays(-i + 2);
                    report.Resolution = statuses[i] == ReportStatus.Resolved 
                        ? ReportResolution.ContentRemoved 
                        : ReportResolution.NoActionRequired;
                }
            }

            _context.ContentReports.Add(report);
        }

        await _context.SaveChangesAsync();
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }
}
