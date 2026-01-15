using FluentAssertions;
using GameGuild.Localization;
using Xunit;

namespace GameGuild.Tests.Localization.Unit;

/// <summary>
/// Tests for TranslationWorkflow state transitions and lifecycle
/// </summary>
public class TranslationWorkflowStateTests
{
    #region Workflow State Transition Tests

    [Fact]
    public void TranslationWorkflow_ProgressesThroughStates_StartsAsPendingAssignment()
    {
        // Arrange & Act
        var workflow = CreateWorkflow();

        // Assert
        workflow.Status.Should().Be(TranslationWorkflowStatus.PendingAssignment);
    }

    [Fact]
    public void TranslationWorkflow_ProgressesThroughStates_CanTransitionToInProgress()
    {
        // Arrange
        var workflow = CreateWorkflow();

        // Act
        workflow.Status = TranslationWorkflowStatus.InProgress;

        // Assert
        workflow.Status.Should().Be(TranslationWorkflowStatus.InProgress);
    }

    [Fact]
    public void TranslationWorkflow_ProgressesThroughStates_CanTransitionToCompleted()
    {
        // Arrange
        var workflow = CreateWorkflow();
        workflow.Status = TranslationWorkflowStatus.InProgress;

        // Act
        workflow.Status = TranslationWorkflowStatus.Completed;

        // Assert
        workflow.Status.Should().Be(TranslationWorkflowStatus.Completed);
    }

    [Fact]
    public void TranslationWorkflow_ProgressesThroughStates_CanTransitionToCancelled()
    {
        // Arrange
        var workflow = CreateWorkflow();

        // Act
        workflow.Status = TranslationWorkflowStatus.Cancelled;

        // Assert
        workflow.Status.Should().Be(TranslationWorkflowStatus.Cancelled);
    }

    [Theory]
    [InlineData(TranslationWorkflowStatus.PendingAssignment)]
    [InlineData(TranslationWorkflowStatus.InProgress)]
    [InlineData(TranslationWorkflowStatus.Completed)]
    [InlineData(TranslationWorkflowStatus.Cancelled)]
    public void TranslationWorkflow_ProgressesThroughStates_SupportsAllStatusValues(
        TranslationWorkflowStatus status)
    {
        // Arrange
        var workflow = CreateWorkflow();

        // Act
        workflow.Status = status;

        // Assert
        workflow.Status.Should().Be(status);
    }

    #endregion

    #region Task State Transition Tests

    [Fact]
    public void TranslationTask_ProgressesThroughStates_StartsAsAssigned()
    {
        // Arrange & Act
        var task = CreateTask();

        // Assert
        task.Status.Should().Be(TranslationTaskStatus.Assigned);
    }

    [Fact]
    public void TranslationTask_ProgressesThroughStates_CanTransitionToInProgress()
    {
        // Arrange
        var task = CreateTask();

        // Act
        task.Status = TranslationTaskStatus.InProgress;

        // Assert
        task.Status.Should().Be(TranslationTaskStatus.InProgress);
    }

    [Fact]
    public void TranslationTask_ProgressesThroughStates_CanTransitionToPendingReview()
    {
        // Arrange
        var task = CreateTask();
        task.Status = TranslationTaskStatus.InProgress;

        // Act
        task.Status = TranslationTaskStatus.PendingReview;
        task.TranslatedText = "Translated content";

        // Assert
        task.Status.Should().Be(TranslationTaskStatus.PendingReview);
        task.TranslatedText.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void TranslationTask_ProgressesThroughStates_CanTransitionToApproved()
    {
        // Arrange
        var task = CreateTask();
        task.Status = TranslationTaskStatus.PendingReview;
        task.TranslatedText = "Translated content";

        // Act
        task.Status = TranslationTaskStatus.Approved;
        task.ReviewerId = Guid.NewGuid();
        task.ReviewedAt = DateTime.UtcNow;

        // Assert
        task.Status.Should().Be(TranslationTaskStatus.Approved);
        task.ReviewerId.Should().NotBeNull();
        task.ReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public void TranslationTask_ProgressesThroughStates_CanTransitionToRejected()
    {
        // Arrange
        var task = CreateTask();
        task.Status = TranslationTaskStatus.PendingReview;
        task.TranslatedText = "Bad translation";

        // Act
        task.Status = TranslationTaskStatus.Rejected;
        task.ReviewerId = Guid.NewGuid();
        task.ReviewedAt = DateTime.UtcNow;
        task.ReviewFeedback = "Translation quality is insufficient";

        // Assert
        task.Status.Should().Be(TranslationTaskStatus.Rejected);
        task.ReviewFeedback.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(TranslationTaskStatus.Assigned)]
    [InlineData(TranslationTaskStatus.InProgress)]
    [InlineData(TranslationTaskStatus.PendingReview)]
    [InlineData(TranslationTaskStatus.Approved)]
    [InlineData(TranslationTaskStatus.Rejected)]
    public void TranslationTask_ProgressesThroughStates_SupportsAllStatusValues(
        TranslationTaskStatus status)
    {
        // Arrange
        var task = CreateTask();

        // Act
        task.Status = status;

        // Assert
        task.Status.Should().Be(status);
    }

    #endregion

    #region Full Workflow Lifecycle Tests

    [Fact]
    public void TranslationWorkflow_FullLifecycle_CompletesSuccessfully()
    {
        // Arrange
        var workflow = CreateWorkflow();
        var task = CreateTask(workflow.Id);
        workflow.Tasks.Add(task);

        // Act - Progress through full lifecycle
        // 1. Assign translator (already done in CreateTask)
        workflow.Status = TranslationWorkflowStatus.InProgress;

        // 2. Translator works on translation
        task.Status = TranslationTaskStatus.InProgress;

        // 3. Translation submitted for review
        task.TranslatedText = "Texto traducido al español";
        task.Status = TranslationTaskStatus.PendingReview;
        task.SubmittedAt = DateTime.UtcNow;

        // 4. Review and approve
        task.Status = TranslationTaskStatus.Approved;
        task.ReviewerId = Guid.NewGuid();
        task.ReviewedAt = DateTime.UtcNow;

        // 5. Workflow completed
        workflow.Status = TranslationWorkflowStatus.Completed;

        // Assert - Final state
        workflow.Status.Should().Be(TranslationWorkflowStatus.Completed);
        task.Status.Should().Be(TranslationTaskStatus.Approved);
        task.TranslatedText.Should().NotBeNullOrEmpty();
        task.ReviewerId.Should().NotBeNull();
        workflow.Tasks.Should().ContainSingle();
    }

    [Fact]
    public void TranslationWorkflow_FullLifecycle_CanBeCancelled()
    {
        // Arrange
        var workflow = CreateWorkflow();
        workflow.Status = TranslationWorkflowStatus.InProgress;
        var task = CreateTask(workflow.Id);
        task.Status = TranslationTaskStatus.InProgress;
        workflow.Tasks.Add(task);

        // Act - Cancel workflow
        workflow.Status = TranslationWorkflowStatus.Cancelled;

        // Assert
        workflow.Status.Should().Be(TranslationWorkflowStatus.Cancelled);
    }

    [Fact]
    public void TranslationWorkflow_FullLifecycle_CanHaveMultipleTasks()
    {
        // Arrange
        var workflow = CreateWorkflow(targetLanguages: ["es-ES", "fr-FR", "de-DE"]);

        // Act - Create tasks for each language
        var spanishTask = CreateTask(workflow.Id, "es-ES");
        var frenchTask = CreateTask(workflow.Id, "fr-FR");
        var germanTask = CreateTask(workflow.Id, "de-DE");
        
        workflow.Tasks.Add(spanishTask);
        workflow.Tasks.Add(frenchTask);
        workflow.Tasks.Add(germanTask);

        // Assert
        workflow.Tasks.Should().HaveCount(3);
        workflow.TargetLanguages.Should().HaveCount(3);
        workflow.Tasks.Should().Contain(t => t.TargetLanguage == "es-ES");
        workflow.Tasks.Should().Contain(t => t.TargetLanguage == "fr-FR");
        workflow.Tasks.Should().Contain(t => t.TargetLanguage == "de-DE");
    }

    #endregion

    #region Priority Tests

    [Theory]
    [InlineData(TranslationPriority.Low)]
    [InlineData(TranslationPriority.Normal)]
    [InlineData(TranslationPriority.High)]
    [InlineData(TranslationPriority.Critical)]
    public void TranslationWorkflow_Priority_SupportsAllValues(TranslationPriority priority)
    {
        // Arrange
        var workflow = CreateWorkflow();

        // Act
        workflow.Priority = priority;

        // Assert
        workflow.Priority.Should().Be(priority);
    }

    [Fact]
    public void TranslationWorkflow_Priority_DefaultsToNormal()
    {
        // Arrange & Act
        var workflow = CreateWorkflow();

        // Assert
        workflow.Priority.Should().Be(TranslationPriority.Normal);
    }

    #endregion

    #region Helper Methods

    private static TranslationWorkflowEntity CreateWorkflow(
        string resourceKey = "Test.Resource.Title",
        string sourceText = "Test source text",
        string[]? targetLanguages = null)
    {
        return new TranslationWorkflowEntity
        {
            Id = Guid.NewGuid(),
            ResourceKey = resourceKey,
            SourceLanguage = "en-US",
            SourceText = sourceText,
            TargetLanguages = targetLanguages ?? ["es-ES"]
        };
    }

    private static TranslationTaskEntity CreateTask(
        Guid? workflowId = null,
        string targetLanguage = "es-ES")
    {
        return new TranslationTaskEntity
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflowId ?? Guid.NewGuid(),
            TargetLanguage = targetLanguage,
            TranslatorId = Guid.NewGuid(),
            AssignedAt = DateTime.UtcNow
        };
    }

    #endregion
}
