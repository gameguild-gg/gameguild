using Microsoft.EntityFrameworkCore;

namespace GameGuild.Localization;

/// <summary>
/// Repository implementation for translation workflow persistence.
/// Uses IApplicationDbContext for database access following project patterns.
/// </summary>
public class TranslationWorkflowRepository : ITranslationWorkflowRepository
{
    private readonly IApplicationDbContext _context;

    public TranslationWorkflowRepository(IApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    #region Workflow Operations

    public async Task<TranslationWorkflowEntity?> GetWorkflowByIdAsync(
        Guid workflowId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<TranslationWorkflowEntity>()
            .Include(w => w.Tasks)
            .FirstOrDefaultAsync(w => w.Id == workflowId && !w.IsDeleted, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TranslationWorkflowEntity>> GetWorkflowsByStatusAsync(
        TranslationWorkflowStatus status, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<TranslationWorkflowEntity>()
            .Include(w => w.Tasks)
            .Where(w => w.Status == status && !w.IsDeleted)
            .OrderByDescending(w => w.Priority)
            .ThenBy(w => w.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TranslationWorkflowEntity>> GetWorkflowsByPriorityAsync(
        TranslationPriority priority, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<TranslationWorkflowEntity>()
            .Include(w => w.Tasks)
            .Where(w => w.Priority == priority && !w.IsDeleted)
            .OrderBy(w => w.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<TranslationWorkflowEntity> CreateWorkflowAsync(
        TranslationWorkflowEntity workflow, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        await _context.Set<TranslationWorkflowEntity>().AddAsync(workflow, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        
        return workflow;
    }

    public async Task UpdateWorkflowAsync(
        TranslationWorkflowEntity workflow, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workflow);

        _context.Set<TranslationWorkflowEntity>().Update(workflow);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TranslationWorkflowEntity>> GetPendingWorkflowsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<TranslationWorkflowEntity>()
            .Include(w => w.Tasks)
            .Where(w => w.Status != TranslationWorkflowStatus.Completed && !w.IsDeleted)
            .OrderByDescending(w => w.Priority)
            .ThenBy(w => w.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Task Operations

    public async Task<TranslationTaskEntity?> GetTaskByIdAsync(
        Guid taskId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<TranslationTaskEntity>()
            .Include(t => t.Workflow)
            .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TranslationTaskEntity>> GetTasksByWorkflowIdAsync(
        Guid workflowId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<TranslationTaskEntity>()
            .Where(t => t.WorkflowId == workflowId && !t.IsDeleted)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TranslationTaskEntity>> GetPendingTasksByTranslatorAsync(
        Guid translatorId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<TranslationTaskEntity>()
            .Include(t => t.Workflow)
            .Where(t => t.TranslatorId == translatorId && 
                       t.Status == TranslationTaskStatus.Assigned && 
                       !t.IsDeleted)
            .OrderByDescending(t => t.Workflow.Priority)
            .ThenBy(t => t.AssignedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<TranslationTaskEntity> CreateTaskAsync(
        TranslationTaskEntity task, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        await _context.Set<TranslationTaskEntity>().AddAsync(task, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        
        return task;
    }

    public async Task UpdateTaskAsync(
        TranslationTaskEntity task, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        _context.Set<TranslationTaskEntity>().Update(task);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    #endregion
}
