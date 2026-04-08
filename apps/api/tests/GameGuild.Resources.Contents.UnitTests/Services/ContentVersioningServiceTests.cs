using FluentAssertions;
using GameGuild.Resources.Contents;
using Moq;
using Xunit;

namespace GameGuild.Resources.Contents.UnitTests.Services;

/// <summary>
/// Tests for ContentVersioningService facade - verifies delegation to sub-services
/// </summary>
public class ContentVersioningServiceTests
{
    private readonly Mock<IContentDraftService> _draftServiceMock;
    private readonly Mock<IContentReviewPublishingService> _reviewServiceMock;
    private readonly Mock<IContentVersionQueryService> _queryServiceMock;
    private readonly ContentVersioningService _service;

    public ContentVersioningServiceTests()
    {
        _draftServiceMock = new Mock<IContentDraftService>();
        _reviewServiceMock = new Mock<IContentReviewPublishingService>();
        _queryServiceMock = new Mock<IContentVersionQueryService>();

        _service = new ContentVersioningService(
            _draftServiceMock.Object,
            _reviewServiceMock.Object,
            _queryServiceMock.Object);
    }

    #region Draft Management Delegation

    [Fact]
    public async Task CreateDraftAsync_ShouldDelegateToDraftService()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var expected = ContentVersion.Create(entityId, "Course", 1, "Title", createdBy);
        _draftServiceMock.Setup(s => s.CreateDraftAsync(
            entityId, "Course", "Title", createdBy,
            "Summary", "Body", null, "Notes", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        // Act
        var result = await _service.CreateDraftAsync(
            entityId, "Course", "Title", createdBy,
            summary: "Summary", body: "Body", changeNotes: "Notes");

        // Assert
        result.IsSuccess.Should().BeTrue();
        _draftServiceMock.Verify(s => s.CreateDraftAsync(
            entityId, "Course", "Title", createdBy,
            "Summary", "Body", null, "Notes", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateDraftAsync_ShouldDelegateToDraftService()
    {
        // Arrange
        var versionId = Guid.NewGuid();
        var expected = ContentVersion.Create(Guid.NewGuid(), "Course", 1, "Updated", Guid.NewGuid());
        _draftServiceMock.Setup(s => s.UpdateDraftAsync(
            versionId, "Title", "Summary", "Body", null, "Notes", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        // Act
        var result = await _service.UpdateDraftAsync(versionId,
            title: "Title", summary: "Summary", body: "Body", changeNotes: "Notes");

        // Assert
        result.IsSuccess.Should().BeTrue();
        _draftServiceMock.Verify(s => s.UpdateDraftAsync(
            versionId, "Title", "Summary", "Body", null, "Notes", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDraftAsync_ShouldDelegateToDraftService()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var expected = ContentVersion.Create(entityId, "Course", 1, "Draft", Guid.NewGuid());
        _draftServiceMock.Setup(s => s.GetDraftAsync(entityId, "Course", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        // Act
        var result = await _service.GetDraftAsync(entityId, "Course");

        // Assert
        result.IsSuccess.Should().BeTrue();
        _draftServiceMock.Verify(s => s.GetDraftAsync(entityId, "Course", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RollbackAsync_ShouldDelegateToDraftService()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var expected = ContentVersion.Create(entityId, "Course", 3, "Rollback", Guid.NewGuid());
        _draftServiceMock.Setup(s => s.RollbackAsync(entityId, "Course", 1, "reason", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        // Act
        var result = await _service.RollbackAsync(entityId, "Course", 1, "reason");

        // Assert
        result.IsSuccess.Should().BeTrue();
        _draftServiceMock.Verify(s => s.RollbackAsync(entityId, "Course", 1, "reason", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ArchiveOldVersionsAsync_ShouldDelegateToDraftService()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        _draftServiceMock.Setup(s => s.ArchiveOldVersionsAsync(entityId, "Course", 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(3));

        // Act
        var result = await _service.ArchiveOldVersionsAsync(entityId, "Course", 5);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(3);
        _draftServiceMock.Verify(s => s.ArchiveOldVersionsAsync(entityId, "Course", 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Review Workflow Delegation

    [Fact]
    public async Task SubmitForReviewAsync_ShouldDelegateToReviewService()
    {
        // Arrange
        var versionId = Guid.NewGuid();
        var expected = ContentVersion.Create(Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());
        _reviewServiceMock.Setup(s => s.SubmitForReviewAsync(versionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        // Act
        var result = await _service.SubmitForReviewAsync(versionId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _reviewServiceMock.Verify(s => s.SubmitForReviewAsync(versionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveAsync_ShouldDelegateToReviewService()
    {
        // Arrange
        var versionId = Guid.NewGuid();
        var expected = ContentVersion.Create(Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());
        _reviewServiceMock.Setup(s => s.ApproveAsync(versionId, "notes", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        // Act
        var result = await _service.ApproveAsync(versionId, "notes");

        // Assert
        result.IsSuccess.Should().BeTrue();
        _reviewServiceMock.Verify(s => s.ApproveAsync(versionId, "notes", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RejectAsync_ShouldDelegateToReviewService()
    {
        // Arrange
        var versionId = Guid.NewGuid();
        var expected = ContentVersion.Create(Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());
        _reviewServiceMock.Setup(s => s.RejectAsync(versionId, "notes", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        // Act
        var result = await _service.RejectAsync(versionId, "notes");

        // Assert
        result.IsSuccess.Should().BeTrue();
        _reviewServiceMock.Verify(s => s.RejectAsync(versionId, "notes", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPendingReviewAsync_ShouldDelegateToReviewService()
    {
        // Arrange
        var versions = new List<ContentVersion>();
        _reviewServiceMock.Setup(s => s.GetPendingReviewAsync("Course", 0, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IEnumerable<ContentVersion>>(versions));

        // Act
        var result = await _service.GetPendingReviewAsync("Course", 0, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _reviewServiceMock.Verify(s => s.GetPendingReviewAsync("Course", 0, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddReviewAsync_ShouldDelegateToReviewService()
    {
        // Arrange
        var versionId = Guid.NewGuid();
        var expected = ContentVersionReview.Create(versionId, Guid.NewGuid(), ContentReviewDecision.Approve);
        _reviewServiceMock.Setup(s => s.AddReviewAsync(versionId, ContentReviewDecision.Approve, "feedback", "suggestions", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        // Act
        var result = await _service.AddReviewAsync(versionId, ContentReviewDecision.Approve, "feedback", "suggestions");

        // Assert
        result.IsSuccess.Should().BeTrue();
        _reviewServiceMock.Verify(s => s.AddReviewAsync(versionId, ContentReviewDecision.Approve, "feedback", "suggestions", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Publishing Delegation

    [Fact]
    public async Task PublishAsync_ShouldDelegateToReviewService()
    {
        // Arrange
        var versionId = Guid.NewGuid();
        var expected = ContentVersion.Create(Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());
        _reviewServiceMock.Setup(s => s.PublishAsync(versionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        // Act
        var result = await _service.PublishAsync(versionId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _reviewServiceMock.Verify(s => s.PublishAsync(versionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SchedulePublishAsync_ShouldDelegateToReviewService()
    {
        // Arrange
        var versionId = Guid.NewGuid();
        var scheduledAt = DateTime.UtcNow.AddDays(7);
        var expected = ContentVersion.Create(Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());
        _reviewServiceMock.Setup(s => s.SchedulePublishAsync(versionId, scheduledAt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        // Act
        var result = await _service.SchedulePublishAsync(versionId, scheduledAt);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _reviewServiceMock.Verify(s => s.SchedulePublishAsync(versionId, scheduledAt, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelScheduledPublishAsync_ShouldDelegateToReviewService()
    {
        // Arrange
        var versionId = Guid.NewGuid();
        var expected = ContentVersion.Create(Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());
        _reviewServiceMock.Setup(s => s.CancelScheduledPublishAsync(versionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        // Act
        var result = await _service.CancelScheduledPublishAsync(versionId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _reviewServiceMock.Verify(s => s.CancelScheduledPublishAsync(versionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessScheduledPublishingAsync_ShouldDelegateToReviewService()
    {
        // Arrange
        _reviewServiceMock.Setup(s => s.ProcessScheduledPublishingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(5));

        // Act
        var result = await _service.ProcessScheduledPublishingAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(5);
        _reviewServiceMock.Verify(s => s.ProcessScheduledPublishingAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Version History Delegation

    [Fact]
    public async Task GetVersionHistoryAsync_ShouldDelegateToQueryService()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var versions = new List<ContentVersion>();
        _queryServiceMock.Setup(s => s.GetVersionHistoryAsync(entityId, "Course", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IEnumerable<ContentVersion>>(versions));

        // Act
        var result = await _service.GetVersionHistoryAsync(entityId, "Course");

        // Assert
        result.IsSuccess.Should().BeTrue();
        _queryServiceMock.Verify(s => s.GetVersionHistoryAsync(entityId, "Course", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetVersionAsync_ShouldDelegateToQueryService()
    {
        // Arrange
        var versionId = Guid.NewGuid();
        var expected = ContentVersion.Create(Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());
        _queryServiceMock.Setup(s => s.GetVersionAsync(versionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        // Act
        var result = await _service.GetVersionAsync(versionId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _queryServiceMock.Verify(s => s.GetVersionAsync(versionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetVersionByNumberAsync_ShouldDelegateToQueryService()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var expected = ContentVersion.Create(entityId, "Course", 2, "Title", Guid.NewGuid());
        _queryServiceMock.Setup(s => s.GetVersionByNumberAsync(entityId, "Course", 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        // Act
        var result = await _service.GetVersionByNumberAsync(entityId, "Course", 2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _queryServiceMock.Verify(s => s.GetVersionByNumberAsync(entityId, "Course", 2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCurrentVersionAsync_ShouldDelegateToQueryService()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var expected = ContentVersion.Create(entityId, "Course", 1, "Title", Guid.NewGuid());
        _queryServiceMock.Setup(s => s.GetCurrentVersionAsync(entityId, "Course", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        // Act
        var result = await _service.GetCurrentVersionAsync(entityId, "Course");

        // Assert
        result.IsSuccess.Should().BeTrue();
        _queryServiceMock.Verify(s => s.GetCurrentVersionAsync(entityId, "Course", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompareVersionsAsync_ShouldDelegateToQueryService()
    {
        // Arrange
        var v1Id = Guid.NewGuid();
        var v2Id = Guid.NewGuid();
        var expected = new ContentVersionDiff(v1Id, v2Id, 1, 2, true, false, false, false, "diff", null, null);
        _queryServiceMock.Setup(s => s.CompareVersionsAsync(v1Id, v2Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        // Act
        var result = await _service.CompareVersionsAsync(v1Id, v2Id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _queryServiceMock.Verify(s => s.CompareVersionsAsync(v1Id, v2Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
