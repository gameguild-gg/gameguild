using Microsoft.Extensions.Logging;

namespace GameGuild.Localization;

/// <summary>
/// Translation workflow service interface.
/// </summary>
public interface ITranslationWorkflowService
{
    Task<TranslationWorkflow> CreateWorkflowAsync(
        string resourceKey,
        string sourceLanguage,
        string[] targetLanguages,
        string sourceText,
        TranslationPriority priority = TranslationPriority.Normal,
        CancellationToken cancellationToken = default);

    Task<TranslationTask> AssignTranslationTaskAsync(
        Guid workflowId,
        string targetLanguage,
        Guid translatorId,
        CancellationToken cancellationToken = default);

    Task<TranslationTask> SubmitTranslationAsync(
        Guid taskId,
        string translatedText,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    Task<TranslationTask> ReviewTranslationAsync(
        Guid taskId,
        Guid reviewerId,
        TranslationReviewDecision decision,
        string? feedback = null,
        CancellationToken cancellationToken = default);

    Task<TranslationWorkflow> ApproveWorkflowAsync(
        Guid workflowId,
        Guid approverId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<TranslationTask>> GetPendingTasksAsync(
        Guid? translatorId = null,
        string? targetLanguage = null,
        CancellationToken cancellationToken = default);

    Task<TranslationWorkflow> GetWorkflowAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Translation workflow service implementation.
/// Uses repository for persistence instead of in-memory storage.
/// </summary>
public sealed class TranslationWorkflowService : ITranslationWorkflowService
{
    private readonly ILogger<TranslationWorkflowService> _logger;
    private readonly ITranslationWorkflowRepository _repository;

    public TranslationWorkflowService(
        ILogger<TranslationWorkflowService> logger,
        ITranslationWorkflowRepository repository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<TranslationWorkflow> CreateWorkflowAsync(
        string resourceKey,
        string sourceLanguage,
        string[] targetLanguages,
        string sourceText,
        TranslationPriority priority = TranslationPriority.Normal,
        CancellationToken cancellationToken = default)
    {
        var entity = new TranslationWorkflowEntity
        {
            Id = Guid.NewGuid(),
            ResourceKey = resourceKey,
            SourceLanguage = sourceLanguage,
            TargetLanguages = targetLanguages,
            SourceText = sourceText,
            Priority = priority,
            Status = TranslationWorkflowStatus.PendingAssignment,
            Tasks = new List<TranslationTaskEntity>()
        };

        await _repository.CreateWorkflowAsync(entity, cancellationToken).ConfigureAwait(false);
        
        _logger.LogInformation("Created translation workflow {WorkflowId} for resource {ResourceKey}",
            entity.Id, resourceKey);

        return MapToDto(entity);
    }

    public async Task<TranslationTask> AssignTranslationTaskAsync(
        Guid workflowId,
        string targetLanguage,
        Guid translatorId,
        CancellationToken cancellationToken = default)
    {
        var workflow = await _repository.GetWorkflowByIdAsync(workflowId, cancellationToken).ConfigureAwait(false);
        if (workflow == null)
        {
            throw new InvalidOperationException($"Workflow {workflowId} not found");
        }

        var taskEntity = new TranslationTaskEntity
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflowId,
            TargetLanguage = targetLanguage,
            TranslatorId = translatorId,
            Status = TranslationTaskStatus.Assigned,
            AssignedAt = DateTime.UtcNow
        };

        await _repository.CreateTaskAsync(taskEntity, cancellationToken).ConfigureAwait(false);
        
        workflow.Status = TranslationWorkflowStatus.InProgress;
        await _repository.UpdateWorkflowAsync(workflow, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Assigned task {TaskId} to translator {TranslatorId} for language {Language}",
            taskEntity.Id, translatorId, targetLanguage);

        return MapTaskToDto(taskEntity);
    }

    public async Task<TranslationTask> SubmitTranslationAsync(
        Guid taskId,
        string translatedText,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var task = await _repository.GetTaskByIdAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (task == null)
        {
            throw new InvalidOperationException($"Task {taskId} not found");
        }

        task.TranslatedText = translatedText;
        task.Metadata = metadata;
        task.Status = TranslationTaskStatus.PendingReview;
        task.SubmittedAt = DateTime.UtcNow;

        await _repository.UpdateTaskAsync(task, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Translation submitted for task {TaskId}", taskId);

        return MapTaskToDto(task);
    }

    public async Task<TranslationTask> ReviewTranslationAsync(
        Guid taskId,
        Guid reviewerId,
        TranslationReviewDecision decision,
        string? feedback = null,
        CancellationToken cancellationToken = default)
    {
        var task = await _repository.GetTaskByIdAsync(taskId, cancellationToken).ConfigureAwait(false);
        if (task == null)
        {
            throw new InvalidOperationException($"Task {taskId} not found");
        }

        task.ReviewerId = reviewerId;
        task.ReviewFeedback = feedback;
        task.ReviewedAt = DateTime.UtcNow;

        task.Status = decision switch
        {
            TranslationReviewDecision.Approved => TranslationTaskStatus.Approved,
            TranslationReviewDecision.Rejected => TranslationTaskStatus.Rejected,
            TranslationReviewDecision.NeedsRevision => TranslationTaskStatus.NeedsRevision,
            _ => throw new ArgumentException($"Unknown decision: {decision}")
        };

        await _repository.UpdateTaskAsync(task, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Task {TaskId} reviewed by {ReviewerId}: {Decision}",
            taskId, reviewerId, decision);

        return MapTaskToDto(task);
    }

    public async Task<TranslationWorkflow> ApproveWorkflowAsync(
        Guid workflowId,
        Guid approverId,
        CancellationToken cancellationToken = default)
    {
        var workflow = await _repository.GetWorkflowByIdAsync(workflowId, cancellationToken).ConfigureAwait(false);
        if (workflow == null)
        {
            throw new InvalidOperationException($"Workflow {workflowId} not found");
        }

        var allApproved = workflow.Tasks.All(t => t.Status == TranslationTaskStatus.Approved);
        if (!allApproved)
        {
            throw new InvalidOperationException("Not all tasks are approved");
        }

        workflow.Status = TranslationWorkflowStatus.Completed;
        workflow.ApprovedBy = approverId;
        workflow.ApprovedAt = DateTime.UtcNow;

        await _repository.UpdateWorkflowAsync(workflow, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Workflow {WorkflowId} approved by {ApproverId}", workflowId, approverId);

        return MapToDto(workflow);
    }

    public async Task<IEnumerable<TranslationTask>> GetPendingTasksAsync(
        Guid? translatorId = null,
        string? targetLanguage = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TranslationTaskEntity> tasks;

        if (translatorId.HasValue)
        {
            tasks = await _repository.GetPendingTasksByTranslatorAsync(translatorId.Value, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Get all pending workflows and extract their tasks
            var workflows = await _repository.GetPendingWorkflowsAsync(cancellationToken).ConfigureAwait(false);
            tasks = workflows.SelectMany(w => w.Tasks)
                .Where(t => t.Status == TranslationTaskStatus.Assigned ||
                           t.Status == TranslationTaskStatus.PendingReview)
                .ToList();
        }

        var filtered = tasks
            .Where(t => string.IsNullOrEmpty(targetLanguage) || t.TargetLanguage == targetLanguage);

        return filtered.Select(MapTaskToDto).ToList();
    }

    public async Task<TranslationWorkflow> GetWorkflowAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default)
    {
        var workflow = await _repository.GetWorkflowByIdAsync(workflowId, cancellationToken).ConfigureAwait(false);
        if (workflow == null)
        {
            throw new InvalidOperationException($"Workflow {workflowId} not found");
        }

        return MapToDto(workflow);
    }

    #region Mapping Helpers

    private static TranslationWorkflow MapToDto(TranslationWorkflowEntity entity)
    {
        return new TranslationWorkflow
        {
            Id = entity.Id,
            ResourceKey = entity.ResourceKey,
            SourceLanguage = entity.SourceLanguage,
            TargetLanguages = entity.TargetLanguages,
            SourceText = entity.SourceText,
            Priority = entity.Priority,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt,
            ApprovedAt = entity.ApprovedAt,
            ApprovedBy = entity.ApprovedBy,
            Tasks = entity.Tasks.Select(MapTaskToDto).ToList()
        };
    }

    private static TranslationTask MapTaskToDto(TranslationTaskEntity entity)
    {
        return new TranslationTask
        {
            Id = entity.Id,
            WorkflowId = entity.WorkflowId,
            TargetLanguage = entity.TargetLanguage,
            TranslatorId = entity.TranslatorId,
            Status = entity.Status,
            TranslatedText = entity.TranslatedText,
            Metadata = entity.Metadata,
            AssignedAt = entity.AssignedAt,
            SubmittedAt = entity.SubmittedAt,
            ReviewedAt = entity.ReviewedAt,
            ReviewerId = entity.ReviewerId,
            ReviewFeedback = entity.ReviewFeedback
        };
    }

    #endregion
}

/// <summary>
/// Translation workflow entity.
/// </summary>
public sealed class TranslationWorkflow
{
    public required Guid Id { get; init; }
    public required string ResourceKey { get; init; }
    public required string SourceLanguage { get; init; }
    public required string[] TargetLanguages { get; init; }
    public required string SourceText { get; init; }
    public required TranslationPriority Priority { get; init; }
    public required TranslationWorkflowStatus Status { get; set; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public required List<TranslationTask> Tasks { get; init; }
}

/// <summary>
/// Translation task entity.
/// </summary>
public sealed class TranslationTask
{
    public required Guid Id { get; init; }
    public required Guid WorkflowId { get; init; }
    public required string TargetLanguage { get; init; }
    public required Guid TranslatorId { get; init; }
    public required TranslationTaskStatus Status { get; set; }
    public string? TranslatedText { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public required DateTime AssignedAt { get; init; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewerId { get; set; }
    public string? ReviewFeedback { get; set; }
}

/// <summary>
/// Translation workflow status.
/// </summary>
public enum TranslationWorkflowStatus
{
    PendingAssignment,
    InProgress,
    PendingApproval,
    Completed,
    Cancelled
}

/// <summary>
/// Translation task status.
/// </summary>
public enum TranslationTaskStatus
{
    Assigned,
    InProgress,
    PendingReview,
    NeedsRevision,
    Approved,
    Rejected
}

/// <summary>
/// Translation priority.
/// </summary>
public enum TranslationPriority
{
    Low,
    Normal,
    High,
    Critical
}

/// <summary>
/// Translation review decision.
/// </summary>
public enum TranslationReviewDecision
{
    Approved,
    Rejected,
    NeedsRevision
}
