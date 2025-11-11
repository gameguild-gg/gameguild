namespace GameGuild.Modules.Localization.Translation;

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
/// </summary>
public sealed class TranslationWorkflowService : ITranslationWorkflowService
{
    private readonly ILogger<TranslationWorkflowService> _logger;
    private readonly Dictionary<Guid, TranslationWorkflow> _workflows;
    private readonly Dictionary<Guid, TranslationTask> _tasks;

    public TranslationWorkflowService(ILogger<TranslationWorkflowService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _workflows = new Dictionary<Guid, TranslationWorkflow>();
        _tasks = new Dictionary<Guid, TranslationTask>();
    }

    public Task<TranslationWorkflow> CreateWorkflowAsync(
        string resourceKey,
        string sourceLanguage,
        string[] targetLanguages,
        string sourceText,
        TranslationPriority priority = TranslationPriority.Normal,
        CancellationToken cancellationToken = default)
    {
        var workflow = new TranslationWorkflow
        {
            Id = Guid.NewGuid(),
            ResourceKey = resourceKey,
            SourceLanguage = sourceLanguage,
            TargetLanguages = targetLanguages,
            SourceText = sourceText,
            Priority = priority,
            Status = TranslationWorkflowStatus.PendingAssignment,
            CreatedAt = DateTime.UtcNow,
            Tasks = new List<TranslationTask>()
        };

        _workflows[workflow.Id] = workflow;
        _logger.LogInformation("Created translation workflow {WorkflowId} for resource {ResourceKey}",
            workflow.Id, resourceKey);

        return Task.FromResult(workflow);
    }

    public Task<TranslationTask> AssignTranslationTaskAsync(
        Guid workflowId,
        string targetLanguage,
        Guid translatorId,
        CancellationToken cancellationToken = default)
    {
        if (!_workflows.TryGetValue(workflowId, out var workflow))
        {
            throw new InvalidOperationException($"Workflow {workflowId} not found");
        }

        var task = new TranslationTask
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflowId,
            TargetLanguage = targetLanguage,
            TranslatorId = translatorId,
            Status = TranslationTaskStatus.Assigned,
            AssignedAt = DateTime.UtcNow
        };

        _tasks[task.Id] = task;
        workflow.Tasks.Add(task);
        workflow.Status = TranslationWorkflowStatus.InProgress;

        _logger.LogInformation("Assigned task {TaskId} to translator {TranslatorId} for language {Language}",
            task.Id, translatorId, targetLanguage);

        return Task.FromResult(task);
    }

    public Task<TranslationTask> SubmitTranslationAsync(
        Guid taskId,
        string translatedText,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (!_tasks.TryGetValue(taskId, out var task))
        {
            throw new InvalidOperationException($"Task {taskId} not found");
        }

        task.TranslatedText = translatedText;
        task.Metadata = metadata;
        task.Status = TranslationTaskStatus.PendingReview;
        task.SubmittedAt = DateTime.UtcNow;

        _logger.LogInformation("Translation submitted for task {TaskId}", taskId);

        return Task.FromResult(task);
    }

    public Task<TranslationTask> ReviewTranslationAsync(
        Guid taskId,
        Guid reviewerId,
        TranslationReviewDecision decision,
        string? feedback = null,
        CancellationToken cancellationToken = default)
    {
        if (!_tasks.TryGetValue(taskId, out var task))
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

        _logger.LogInformation("Task {TaskId} reviewed by {ReviewerId}: {Decision}",
            taskId, reviewerId, decision);

        return Task.FromResult(task);
    }

    public Task<TranslationWorkflow> ApproveWorkflowAsync(
        Guid workflowId,
        Guid approverId,
        CancellationToken cancellationToken = default)
    {
        if (!_workflows.TryGetValue(workflowId, out var workflow))
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

        _logger.LogInformation("Workflow {WorkflowId} approved by {ApproverId}", workflowId, approverId);

        return Task.FromResult(workflow);
    }

    public Task<IEnumerable<TranslationTask>> GetPendingTasksAsync(
        Guid? translatorId = null,
        string? targetLanguage = null,
        CancellationToken cancellationToken = default)
    {
        var tasks = _tasks.Values
            .Where(t => t.Status == TranslationTaskStatus.Assigned ||
                       t.Status == TranslationTaskStatus.PendingReview)
            .Where(t => !translatorId.HasValue || t.TranslatorId == translatorId.Value)
            .Where(t => string.IsNullOrEmpty(targetLanguage) || t.TargetLanguage == targetLanguage);

        return Task.FromResult<IEnumerable<TranslationTask>>(tasks.ToList());
    }

    public Task<TranslationWorkflow> GetWorkflowAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default)
    {
        if (!_workflows.TryGetValue(workflowId, out var workflow))
        {
            throw new InvalidOperationException($"Workflow {workflowId} not found");
        }

        return Task.FromResult(workflow);
    }
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
