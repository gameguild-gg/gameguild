namespace GameGuild.Localization;

/// <summary>
/// Repository interface for translation workflow persistence.
/// Follows the same pattern as LanguageRepository.
/// </summary>
public interface ITranslationWorkflowRepository
{
    // Workflow operations
    Task<TranslationWorkflowEntity?> GetWorkflowByIdAsync(Guid workflowId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TranslationWorkflowEntity>> GetWorkflowsByStatusAsync(TranslationWorkflowStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TranslationWorkflowEntity>> GetWorkflowsByPriorityAsync(TranslationPriority priority, CancellationToken cancellationToken = default);
    Task<TranslationWorkflowEntity> CreateWorkflowAsync(TranslationWorkflowEntity workflow, CancellationToken cancellationToken = default);
    Task UpdateWorkflowAsync(TranslationWorkflowEntity workflow, CancellationToken cancellationToken = default);

    // Task operations
    Task<TranslationTaskEntity?> GetTaskByIdAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TranslationTaskEntity>> GetTasksByWorkflowIdAsync(Guid workflowId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TranslationTaskEntity>> GetPendingTasksByTranslatorAsync(Guid translatorId, CancellationToken cancellationToken = default);
    Task<TranslationTaskEntity> CreateTaskAsync(TranslationTaskEntity task, CancellationToken cancellationToken = default);
    Task UpdateTaskAsync(TranslationTaskEntity task, CancellationToken cancellationToken = default);

    // Bulk operations
    Task<IReadOnlyList<TranslationWorkflowEntity>> GetPendingWorkflowsAsync(CancellationToken cancellationToken = default);
}
