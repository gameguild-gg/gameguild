using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Localization.UnitTests.Translation;

/// <summary>
/// Unit tests for TranslationWorkflowService.
/// </summary>
public class TranslationWorkflowServiceTests
{
    private readonly Mock<ILogger<TranslationWorkflowService>> _loggerMock;
    private readonly Mock<ITranslationWorkflowRepository> _repositoryMock;
    private readonly TranslationWorkflowService _service;

    public TranslationWorkflowServiceTests()
    {
        _loggerMock = new Mock<ILogger<TranslationWorkflowService>>();
        _repositoryMock = new Mock<ITranslationWorkflowRepository>();
        _service = new TranslationWorkflowService(_loggerMock.Object, _repositoryMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new TranslationWorkflowService(null!, _repositoryMock.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new TranslationWorkflowService(_loggerMock.Object, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Act
        var service = new TranslationWorkflowService(_loggerMock.Object, _repositoryMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region CreateWorkflowAsync Tests

    [Fact]
    public async Task CreateWorkflowAsync_CreatesWorkflowWithCorrectProperties()
    {
        // Arrange
        const string resourceKey = "Course.123.Title";
        const string sourceLanguage = "en";
        var targetLanguages = new[] { "es", "fr" };
        const string sourceText = "Hello World";
        const TranslationPriority priority = TranslationPriority.High;

        _repositoryMock
            .Setup(r => r.CreateWorkflowAsync(It.IsAny<TranslationWorkflowEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TranslationWorkflowEntity entity, CancellationToken _) => entity);

        // Act
        var result = await _service.CreateWorkflowAsync(
            resourceKey, sourceLanguage, targetLanguages, sourceText, priority);

        // Assert
        result.Should().NotBeNull();
        result.ResourceKey.Should().Be(resourceKey);
        result.SourceLanguage.Should().Be(sourceLanguage);
        result.TargetLanguages.Should().BeEquivalentTo(targetLanguages);
        result.SourceText.Should().Be(sourceText);
        result.Priority.Should().Be(priority);
        result.Status.Should().Be(TranslationWorkflowStatus.PendingAssignment);
        result.Tasks.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateWorkflowAsync_WithDefaultPriority_SetsNormal()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.CreateWorkflowAsync(It.IsAny<TranslationWorkflowEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TranslationWorkflowEntity entity, CancellationToken _) => entity);

        // Act
        var result = await _service.CreateWorkflowAsync(
            "key", "en", new[] { "es" }, "text");

        // Assert
        result.Priority.Should().Be(TranslationPriority.Normal);
    }

    [Fact]
    public async Task CreateWorkflowAsync_CallsRepositoryCreate()
    {
        // Arrange
        TranslationWorkflowEntity? capturedEntity = null;
        _repositoryMock
            .Setup(r => r.CreateWorkflowAsync(It.IsAny<TranslationWorkflowEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TranslationWorkflowEntity e, CancellationToken _) =>
            {
                capturedEntity = e;
                return e;
            });

        // Act
        await _service.CreateWorkflowAsync("key", "en", new[] { "es" }, "text");

        // Assert
        _repositoryMock.Verify(r => r.CreateWorkflowAsync(It.IsAny<TranslationWorkflowEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        capturedEntity.Should().NotBeNull();
        capturedEntity!.ResourceKey.Should().Be("key");
    }

    #endregion

    #region AssignTranslationTaskAsync Tests

    [Fact]
    public async Task AssignTranslationTaskAsync_WithValidWorkflow_CreatesTask()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var translatorId = Guid.NewGuid();
        const string targetLanguage = "es";

        var workflow = CreateTestWorkflowEntity(workflowId);
        
        _repositoryMock
            .Setup(r => r.GetWorkflowByIdAsync(workflowId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflow);
        _repositoryMock
            .Setup(r => r.CreateTaskAsync(It.IsAny<TranslationTaskEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TranslationTaskEntity entity, CancellationToken _) => entity);
        _repositoryMock
            .Setup(r => r.UpdateWorkflowAsync(It.IsAny<TranslationWorkflowEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.AssignTranslationTaskAsync(workflowId, targetLanguage, translatorId);

        // Assert
        result.Should().NotBeNull();
        result.WorkflowId.Should().Be(workflowId);
        result.TargetLanguage.Should().Be(targetLanguage);
        result.TranslatorId.Should().Be(translatorId);
        result.Status.Should().Be(TranslationTaskStatus.Assigned);
    }

    [Fact]
    public async Task AssignTranslationTaskAsync_UpdatesWorkflowStatus()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var translatorId = Guid.NewGuid();

        var workflow = CreateTestWorkflowEntity(workflowId);
        
        _repositoryMock
            .Setup(r => r.GetWorkflowByIdAsync(workflowId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflow);
        _repositoryMock
            .Setup(r => r.CreateTaskAsync(It.IsAny<TranslationTaskEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TranslationTaskEntity entity, CancellationToken _) => entity);
        _repositoryMock
            .Setup(r => r.UpdateWorkflowAsync(It.IsAny<TranslationWorkflowEntity>(), It.IsAny<CancellationToken>()))
            .Callback<TranslationWorkflowEntity, CancellationToken>((w, _) =>
            {
                w.Status.Should().Be(TranslationWorkflowStatus.InProgress);
            })
            .Returns(Task.CompletedTask);

        // Act
        await _service.AssignTranslationTaskAsync(workflowId, "es", translatorId);

        // Assert
        _repositoryMock.Verify(r => r.UpdateWorkflowAsync(It.IsAny<TranslationWorkflowEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignTranslationTaskAsync_WithNonExistentWorkflow_ThrowsException()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        
        _repositoryMock
            .Setup(r => r.GetWorkflowByIdAsync(workflowId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TranslationWorkflowEntity?)null);

        // Act & Assert
        var act = () => _service.AssignTranslationTaskAsync(workflowId, "es", Guid.NewGuid());
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{workflowId}*not found*");
    }

    #endregion

    #region SubmitTranslationAsync Tests

    [Fact]
    public async Task SubmitTranslationAsync_WithValidTask_UpdatesTask()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = CreateTestTaskEntity(taskId);
        const string translatedText = "Hola Mundo";
        var metadata = new Dictionary<string, string> { ["key"] = "value" };

        _repositoryMock
            .Setup(r => r.GetTaskByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _repositoryMock
            .Setup(r => r.UpdateTaskAsync(It.IsAny<TranslationTaskEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SubmitTranslationAsync(taskId, translatedText, metadata);

        // Assert
        result.Should().NotBeNull();
        result.TranslatedText.Should().Be(translatedText);
        result.Metadata.Should().ContainKey("key");
        result.Status.Should().Be(TranslationTaskStatus.PendingReview);
    }

    [Fact]
    public async Task SubmitTranslationAsync_SetsSubmittedAt()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = CreateTestTaskEntity(taskId);

        _repositoryMock
            .Setup(r => r.GetTaskByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _repositoryMock
            .Setup(r => r.UpdateTaskAsync(It.IsAny<TranslationTaskEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SubmitTranslationAsync(taskId, "Hola");

        // Assert
        result.SubmittedAt.Should().NotBeNull();
        result.SubmittedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SubmitTranslationAsync_WithNonExistentTask_ThrowsException()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        
        _repositoryMock
            .Setup(r => r.GetTaskByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TranslationTaskEntity?)null);

        // Act & Assert
        var act = () => _service.SubmitTranslationAsync(taskId, "text");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{taskId}*not found*");
    }

    #endregion

    #region ReviewTranslationAsync Tests

    [Fact]
    public async Task ReviewTranslationAsync_WithApprovalDecision_SetsApprovedStatus()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var task = CreateTestTaskEntity(taskId);

        _repositoryMock
            .Setup(r => r.GetTaskByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _repositoryMock
            .Setup(r => r.UpdateTaskAsync(It.IsAny<TranslationTaskEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ReviewTranslationAsync(
            taskId, reviewerId, TranslationReviewDecision.Approved, "Looks good!");

        // Assert
        result.Status.Should().Be(TranslationTaskStatus.Approved);
        result.ReviewerId.Should().Be(reviewerId);
        result.ReviewFeedback.Should().Be("Looks good!");
    }

    [Fact]
    public async Task ReviewTranslationAsync_WithRejectionDecision_SetsRejectedStatus()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var task = CreateTestTaskEntity(taskId);

        _repositoryMock
            .Setup(r => r.GetTaskByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _repositoryMock
            .Setup(r => r.UpdateTaskAsync(It.IsAny<TranslationTaskEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ReviewTranslationAsync(
            taskId, reviewerId, TranslationReviewDecision.Rejected);

        // Assert
        result.Status.Should().Be(TranslationTaskStatus.Rejected);
    }

    [Fact]
    public async Task ReviewTranslationAsync_WithNeedsRevisionDecision_SetsNeedsRevisionStatus()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var task = CreateTestTaskEntity(taskId);

        _repositoryMock
            .Setup(r => r.GetTaskByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _repositoryMock
            .Setup(r => r.UpdateTaskAsync(It.IsAny<TranslationTaskEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ReviewTranslationAsync(
            taskId, reviewerId, TranslationReviewDecision.NeedsRevision, "Please fix grammar");

        // Assert
        result.Status.Should().Be(TranslationTaskStatus.NeedsRevision);
        result.ReviewFeedback.Should().Be("Please fix grammar");
    }

    [Fact]
    public async Task ReviewTranslationAsync_SetsReviewedAt()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = CreateTestTaskEntity(taskId);

        _repositoryMock
            .Setup(r => r.GetTaskByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);
        _repositoryMock
            .Setup(r => r.UpdateTaskAsync(It.IsAny<TranslationTaskEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ReviewTranslationAsync(
            taskId, Guid.NewGuid(), TranslationReviewDecision.Approved);

        // Assert
        result.ReviewedAt.Should().NotBeNull();
        result.ReviewedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ReviewTranslationAsync_WithNonExistentTask_ThrowsException()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        
        _repositoryMock
            .Setup(r => r.GetTaskByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TranslationTaskEntity?)null);

        // Act & Assert
        var act = () => _service.ReviewTranslationAsync(taskId, Guid.NewGuid(), TranslationReviewDecision.Approved);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{taskId}*not found*");
    }

    [Fact]
    public async Task ReviewTranslationAsync_WithUnknownDecision_ThrowsArgumentException()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = CreateTestTaskEntity(taskId);

        _repositoryMock
            .Setup(r => r.GetTaskByIdAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        // Act & Assert
        var act = () => _service.ReviewTranslationAsync(taskId, Guid.NewGuid(), (TranslationReviewDecision)999);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Unknown decision*");
    }

    #endregion

    #region ApproveWorkflowAsync Tests

    [Fact]
    public async Task ApproveWorkflowAsync_WithAllTasksApproved_ApprovesWorkflow()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var approverId = Guid.NewGuid();
        var workflow = CreateTestWorkflowEntity(workflowId);
        workflow.Tasks.Add(CreateTestTaskEntity(Guid.NewGuid(), workflowId, TranslationTaskStatus.Approved));
        workflow.Tasks.Add(CreateTestTaskEntity(Guid.NewGuid(), workflowId, TranslationTaskStatus.Approved));

        _repositoryMock
            .Setup(r => r.GetWorkflowByIdAsync(workflowId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflow);
        _repositoryMock
            .Setup(r => r.UpdateWorkflowAsync(It.IsAny<TranslationWorkflowEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ApproveWorkflowAsync(workflowId, approverId);

        // Assert
        result.Status.Should().Be(TranslationWorkflowStatus.Completed);
        result.ApprovedBy.Should().Be(approverId);
        result.ApprovedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ApproveWorkflowAsync_WithPendingTasks_ThrowsException()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var workflow = CreateTestWorkflowEntity(workflowId);
        workflow.Tasks.Add(CreateTestTaskEntity(Guid.NewGuid(), workflowId, TranslationTaskStatus.Approved));
        workflow.Tasks.Add(CreateTestTaskEntity(Guid.NewGuid(), workflowId, TranslationTaskStatus.PendingReview)); // Not approved

        _repositoryMock
            .Setup(r => r.GetWorkflowByIdAsync(workflowId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflow);

        // Act & Assert
        var act = () => _service.ApproveWorkflowAsync(workflowId, Guid.NewGuid());
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Not all tasks are approved*");
    }

    [Fact]
    public async Task ApproveWorkflowAsync_WithNonExistentWorkflow_ThrowsException()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        
        _repositoryMock
            .Setup(r => r.GetWorkflowByIdAsync(workflowId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TranslationWorkflowEntity?)null);

        // Act & Assert
        var act = () => _service.ApproveWorkflowAsync(workflowId, Guid.NewGuid());
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{workflowId}*not found*");
    }

    [Fact]
    public async Task ApproveWorkflowAsync_WithNoTasks_ApprovesWorkflow()
    {
        // Arrange - workflow with no tasks should be approvable
        var workflowId = Guid.NewGuid();
        var workflow = CreateTestWorkflowEntity(workflowId);

        _repositoryMock
            .Setup(r => r.GetWorkflowByIdAsync(workflowId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflow);
        _repositoryMock
            .Setup(r => r.UpdateWorkflowAsync(It.IsAny<TranslationWorkflowEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ApproveWorkflowAsync(workflowId, Guid.NewGuid());

        // Assert - All() on empty collection returns true
        result.Status.Should().Be(TranslationWorkflowStatus.Completed);
    }

    #endregion

    #region GetPendingTasksAsync Tests

    [Fact]
    public async Task GetPendingTasksAsync_WithTranslatorId_GetsPendingTasksForTranslator()
    {
        // Arrange
        var translatorId = Guid.NewGuid();
        var tasks = new List<TranslationTaskEntity>
        {
            CreateTestTaskEntity(Guid.NewGuid(), Guid.NewGuid(), TranslationTaskStatus.Assigned),
            CreateTestTaskEntity(Guid.NewGuid(), Guid.NewGuid(), TranslationTaskStatus.PendingReview)
        };

        _repositoryMock
            .Setup(r => r.GetPendingTasksByTranslatorAsync(translatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        // Act
        var result = await _service.GetPendingTasksAsync(translatorId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPendingTasksAsync_WithoutTranslatorId_GetsPendingTasksFromAllWorkflows()
    {
        // Arrange
        var workflows = new List<TranslationWorkflowEntity>
        {
            CreateTestWorkflowEntity(Guid.NewGuid()),
            CreateTestWorkflowEntity(Guid.NewGuid())
        };
        workflows[0].Tasks.Add(CreateTestTaskEntity(Guid.NewGuid(), workflows[0].Id, TranslationTaskStatus.Assigned));
        workflows[1].Tasks.Add(CreateTestTaskEntity(Guid.NewGuid(), workflows[1].Id, TranslationTaskStatus.PendingReview));
        workflows[1].Tasks.Add(CreateTestTaskEntity(Guid.NewGuid(), workflows[1].Id, TranslationTaskStatus.Approved)); // Should be filtered out

        _repositoryMock
            .Setup(r => r.GetPendingWorkflowsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflows);

        // Act
        var result = await _service.GetPendingTasksAsync();

        // Assert
        result.Should().HaveCount(2); // Only Assigned and PendingReview
    }

    [Fact]
    public async Task GetPendingTasksAsync_WithTargetLanguageFilter_FiltersCorrectly()
    {
        // Arrange
        var translatorId = Guid.NewGuid();
        var tasks = new List<TranslationTaskEntity>
        {
            CreateTestTaskEntity(Guid.NewGuid(), Guid.NewGuid(), TranslationTaskStatus.Assigned, "es"),
            CreateTestTaskEntity(Guid.NewGuid(), Guid.NewGuid(), TranslationTaskStatus.Assigned, "fr")
        };

        _repositoryMock
            .Setup(r => r.GetPendingTasksByTranslatorAsync(translatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        // Act
        var result = await _service.GetPendingTasksAsync(translatorId, targetLanguage: "es");

        // Assert
        result.Should().HaveCount(1);
        result.First().TargetLanguage.Should().Be("es");
    }

    #endregion

    #region GetWorkflowAsync Tests

    [Fact]
    public async Task GetWorkflowAsync_WithExistingWorkflow_ReturnsWorkflow()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var workflow = CreateTestWorkflowEntity(workflowId);

        _repositoryMock
            .Setup(r => r.GetWorkflowByIdAsync(workflowId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflow);

        // Act
        var result = await _service.GetWorkflowAsync(workflowId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(workflowId);
    }

    [Fact]
    public async Task GetWorkflowAsync_WithNonExistentWorkflow_ThrowsException()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        
        _repositoryMock
            .Setup(r => r.GetWorkflowByIdAsync(workflowId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TranslationWorkflowEntity?)null);

        // Act & Assert
        var act = () => _service.GetWorkflowAsync(workflowId);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{workflowId}*not found*");
    }

    #endregion

    #region Test Helpers

    private static TranslationWorkflowEntity CreateTestWorkflowEntity(
        Guid? id = null,
        TranslationWorkflowStatus status = TranslationWorkflowStatus.PendingAssignment)
    {
        var entity = new TranslationWorkflowEntity
        {
            ResourceKey = "test.resource.key",
            SourceLanguage = "en",
            TargetLanguagesJson = "[\"es\",\"fr\"]",
            SourceText = "Test source text",
            Priority = TranslationPriority.Normal,
            Status = status
        };

        if (id.HasValue)
        {
            entity.Id = id.Value;
        }

        return entity;
    }

    private static TranslationTaskEntity CreateTestTaskEntity(
        Guid? id = null,
        Guid? workflowId = null,
        TranslationTaskStatus status = TranslationTaskStatus.Assigned,
        string targetLanguage = "es")
    {
        var entity = new TranslationTaskEntity
        {
            WorkflowId = workflowId ?? Guid.NewGuid(),
            TargetLanguage = targetLanguage,
            TranslatorId = Guid.NewGuid(),
            Status = status,
            AssignedAt = DateTime.UtcNow
        };

        if (id.HasValue)
        {
            entity.Id = id.Value;
        }

        return entity;
    }

    #endregion
}
