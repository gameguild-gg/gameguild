using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using GameGuild.Resources.Contents;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace GameGuild.Resources.Contents.UnitTests.Services;

/// <summary>
/// Tests for ContentDraftService - draft lifecycle management
/// </summary>
public class ContentDraftServiceTests
{
    private readonly Mock<IApplicationDbContext> _dbMock;
    private readonly Mock<IActorContextAccessor> _actorMock;
    private readonly Mock<ILogger<ContentDraftService>> _loggerMock;
    private readonly List<ContentVersion> _versions;
    private readonly ContentDraftService _service;

    public ContentDraftServiceTests()
    {
        _dbMock = new Mock<IApplicationDbContext>();
        _actorMock = new Mock<IActorContextAccessor>();
        _loggerMock = new Mock<ILogger<ContentDraftService>>();
        _versions = new List<ContentVersion>();

        var userId = Guid.NewGuid();
        _actorMock.Setup(a => a.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = userId.ToString(),
            TenantId = Guid.NewGuid(),
            IsAuthenticated = true,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>()
        });

        _dbMock.Setup(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new ContentDraftService(_dbMock.Object, _actorMock.Object, _loggerMock.Object);
    }

    private void SetupDbSet()
    {
        var mockDbSet = _versions.AsQueryable().BuildMockDbSet();
        mockDbSet.Setup(d => d.Add(It.IsAny<ContentVersion>()))
            .Callback<ContentVersion>(v => _versions.Add(v));
        _dbMock.Setup(d => d.Set<ContentVersion>()).Returns(mockDbSet.Object);
    }

    #region CreateDraftAsync Tests

    [Fact]
    public async Task CreateDraftAsync_WithValidParameters_ShouldCreateDraft()
    {
        // Arrange
        SetupDbSet();
        var entityId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();

        // Act
        var result = await _service.CreateDraftAsync(
            entityId, "Course", "My Course", createdBy,
            summary: "Course summary",
            body: "<p>Content</p>",
            changeNotes: "Initial draft");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EntityId.Should().Be(entityId);
        result.Value.EntityType.Should().Be("Course");
        result.Value.Title.Should().Be("My Course");
        result.Value.VersionNumber.Should().Be(1);
        result.Value.Status.Should().Be(ContentVersionStatus.Draft);
    }

    [Fact]
    public async Task CreateDraftAsync_WhenVersionsExist_ShouldIncrementVersionNumber()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var existingVersion = ContentVersion.Create(entityId, "Course", 1, "V1", Guid.NewGuid());
        _versions.Add(existingVersion);
        SetupDbSet();

        // Act
        var result = await _service.CreateDraftAsync(
            entityId, "Course", "V2", Guid.NewGuid());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.VersionNumber.Should().Be(2);
    }

    [Fact]
    public async Task CreateDraftAsync_ShouldCallSaveChanges()
    {
        // Arrange
        SetupDbSet();

        // Act
        await _service.CreateDraftAsync(Guid.NewGuid(), "Course", "Title", Guid.NewGuid());

        // Assert
        _dbMock.Verify(d => d.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateDraftAsync Tests

    [Fact]
    public async Task UpdateDraftAsync_WhenDraftExists_ShouldUpdateFields()
    {
        // Arrange
        var version = ContentVersion.Create(Guid.NewGuid(), "Course", 1, "Original", Guid.NewGuid());
        _versions.Add(version);
        SetupDbSet();

        // Act
        var result = await _service.UpdateDraftAsync(
            version.Id,
            title: "Updated Title",
            summary: "New Summary",
            changeNotes: "Updated content");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Updated Title");
        result.Value.Summary.Should().Be("New Summary");
        result.Value.ChangeNotes.Should().Be("Updated content");
    }

    [Fact]
    public async Task UpdateDraftAsync_WhenVersionNotFound_ShouldReturnFailure()
    {
        // Arrange
        SetupDbSet();

        // Act
        var result = await _service.UpdateDraftAsync(Guid.NewGuid(), title: "New Title");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ContentVersioning.NotFound");
    }

    [Fact]
    public async Task UpdateDraftAsync_WhenNotDraft_ShouldReturnFailure()
    {
        // Arrange
        var version = ContentVersion.Create(Guid.NewGuid(), "Course", 1, "Title", Guid.NewGuid());
        version.SubmitForReview(Guid.NewGuid()); // Now PendingReview status
        _versions.Add(version);
        SetupDbSet();

        // Act
        var result = await _service.UpdateDraftAsync(version.Id, title: "New Title");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ContentVersioning.CanOnlyUpdateDrafts");
    }

    #endregion

    #region GetDraftAsync Tests

    [Fact]
    public async Task GetDraftAsync_WhenDraftExists_ShouldReturnDraft()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var draft = ContentVersion.Create(entityId, "Course", 1, "My Draft", Guid.NewGuid());
        _versions.Add(draft);
        SetupDbSet();

        // Act
        var result = await _service.GetDraftAsync(entityId, "Course");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(draft.Id);
    }

    [Fact]
    public async Task GetDraftAsync_WhenNoDraftExists_ShouldReturnFailure()
    {
        // Arrange
        SetupDbSet();

        // Act
        var result = await _service.GetDraftAsync(Guid.NewGuid(), "Course");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ContentVersioning.NotFound");
    }

    [Fact]
    public async Task GetDraftAsync_ShouldReturnLatestDraftByVersionNumber()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var draft1 = ContentVersion.Create(entityId, "Course", 1, "Draft 1", Guid.NewGuid());
        var draft2 = ContentVersion.Create(entityId, "Course", 2, "Draft 2", Guid.NewGuid());
        _versions.AddRange(new[] { draft1, draft2 });
        SetupDbSet();

        // Act
        var result = await _service.GetDraftAsync(entityId, "Course");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.VersionNumber.Should().Be(2);
        result.Value.Title.Should().Be("Draft 2");
    }

    #endregion

    #region RollbackAsync Tests

    [Fact]
    public async Task RollbackAsync_WhenTargetVersionExists_ShouldCreateNewDraftFromTarget()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var v1 = ContentVersion.Create(entityId, "Course", 1, "Version 1", Guid.NewGuid(),
            summary: "V1 Summary", body: "V1 Body");
        var v2 = ContentVersion.Create(entityId, "Course", 2, "Version 2", Guid.NewGuid());
        _versions.AddRange(new[] { v1, v2 });
        SetupDbSet();

        // Act
        var result = await _service.RollbackAsync(entityId, "Course", 1, "Rolling back to V1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.VersionNumber.Should().Be(3);
        result.Value.Title.Should().Be("Version 1");
        result.Value.Summary.Should().Be("V1 Summary");
        result.Value.ChangeNotes.Should().Contain("Rollback to v1");
    }

    [Fact]
    public async Task RollbackAsync_WhenTargetVersionNotFound_ShouldReturnFailure()
    {
        // Arrange
        SetupDbSet();

        // Act
        var result = await _service.RollbackAsync(Guid.NewGuid(), "Course", 99);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ContentVersioning.NotFound");
    }

    #endregion

    #region ArchiveOldVersionsAsync Tests

    [Fact]
    public async Task ArchiveOldVersionsAsync_ShouldArchiveOldPublishedVersions()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var publisherId = Guid.NewGuid();

        // Create 5 published versions (not current)
        for (int i = 1; i <= 5; i++)
        {
            var v = ContentVersion.Create(entityId, "Course", i, $"V{i}", Guid.NewGuid());
            v.SubmitForReview(Guid.NewGuid());
            v.Approve(Guid.NewGuid());
            v.Publish(publisherId);
            v.SetAsCurrent(false);
            _versions.Add(v);
        }
        SetupDbSet();

        // Act - keep only 2
        var result = await _service.ArchiveOldVersionsAsync(entityId, "Course", keepCount: 2);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(3); // 5 - 2 = 3 archived
    }

    [Fact]
    public async Task ArchiveOldVersionsAsync_ShouldNotArchiveCurrentVersion()
    {
        // Arrange
        var entityId = Guid.NewGuid();
        var v1 = ContentVersion.Create(entityId, "Course", 1, "V1", Guid.NewGuid());
        v1.SubmitForReview(Guid.NewGuid());
        v1.Approve(Guid.NewGuid());
        v1.Publish(Guid.NewGuid());
        // v1 is current (IsCurrentVersion = true after Publish)
        _versions.Add(v1);
        SetupDbSet();

        // Act
        var result = await _service.ArchiveOldVersionsAsync(entityId, "Course", keepCount: 0);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0); // Current version not archived
    }

    #endregion
}
