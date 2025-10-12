using GameGuild.Modules.DataArchival.DTOs;
using GameGuild.Modules.DataArchival.Entities;
using GameGuild.Modules.DataArchival.Repositories;


namespace GameGuild.Modules.DataArchival.Services;

/// <summary>
/// Service implementation for data archival operations.
/// </summary>
public class DataArchivalService : IDataArchivalService {
    private readonly IArchivalPolicyRepository _policyRepository;
    private readonly IArchivalJobRepository _jobRepository;
    private readonly IStorageLifecycleManager _lifecycleManager;
    private readonly ILogger<DataArchivalService> _logger;

    public DataArchivalService(
        IArchivalPolicyRepository policyRepository,
        IArchivalJobRepository jobRepository,
        IStorageLifecycleManager lifecycleManager,
        ILogger<DataArchivalService> logger) {
        _policyRepository = policyRepository;
        _jobRepository = jobRepository;
        _lifecycleManager = lifecycleManager;
        _logger = logger;
    }

    public async Task<ArchivalPolicyDto> CreatePolicyAsync(CreateArchivalPolicyRequest request, CancellationToken cancellationToken = default) {
        var policy = new ArchivalPolicy {
            TenantId = request.TenantId,
            Name = request.Name,
            Description = request.Description,
            EntityType = request.EntityType,
            RetentionDays = request.RetentionDays,
            ArchiveAfterDays = request.ArchiveAfterDays,
            DeleteAfterDays = request.DeleteAfterDays,
            StorageTier = request.StorageTier,
            CompressionEnabled = request.CompressionEnabled,
            EncryptionEnabled = request.EncryptionEnabled,
            IsEnabled = true
        };

        await _policyRepository.AddAsync(policy, cancellationToken);

        _logger.LogInformation("Created archival policy {PolicyId} for entity type {EntityType}", policy.Id, policy.EntityType);

        return MapToDto(policy);
    }

    public async Task<ArchivalPolicyDto?> GetPolicyAsync(Guid policyId, CancellationToken cancellationToken = default) {
        var policy = await _policyRepository.GetByIdAsync(policyId, cancellationToken);
        return policy == null ? null : MapToDto(policy);
    }

    public async Task<List<ArchivalPolicyDto>> GetPoliciesAsync(Guid? tenantId, string? entityType, CancellationToken cancellationToken = default) {
        var policies = await _policyRepository.GetAllAsync(tenantId, entityType, cancellationToken);
        return policies.Select(MapToDto).ToList();
    }

    public async Task UpdatePolicyAsync(Guid policyId, UpdateArchivalPolicyRequest request, CancellationToken cancellationToken = default) {
        var policy = await _policyRepository.GetByIdAsync(policyId, cancellationToken);
        if (policy == null) {
            throw new InvalidOperationException($"Policy {policyId} not found");
        }

        if (request.Name != null) policy.Name = request.Name;
        if (request.Description != null) policy.Description = request.Description;
        if (request.RetentionDays.HasValue) policy.RetentionDays = request.RetentionDays.Value;
        if (request.ArchiveAfterDays.HasValue) policy.ArchiveAfterDays = request.ArchiveAfterDays.Value;
        if (request.DeleteAfterDays.HasValue) policy.DeleteAfterDays = request.DeleteAfterDays.Value;
        if (request.StorageTier != null) policy.StorageTier = request.StorageTier;
        if (request.CompressionEnabled.HasValue) policy.CompressionEnabled = request.CompressionEnabled.Value;
        if (request.EncryptionEnabled.HasValue) policy.EncryptionEnabled = request.EncryptionEnabled.Value;

        await _policyRepository.UpdateAsync(policy, cancellationToken);

        _logger.LogInformation("Updated archival policy {PolicyId}", policyId);
    }

    public async Task DeletePolicyAsync(Guid policyId, CancellationToken cancellationToken = default) {
        await _policyRepository.DeleteAsync(policyId, cancellationToken);
        _logger.LogInformation("Deleted archival policy {PolicyId}", policyId);
    }

    public async Task<Guid> ExecutePolicyAsync(Guid policyId, CancellationToken cancellationToken = default) {
        var policy = await _policyRepository.GetByIdAsync(policyId, cancellationToken);
        if (policy == null) {
            throw new InvalidOperationException($"Policy {policyId} not found");
        }

        if (!policy.IsEnabled) {
            throw new InvalidOperationException($"Policy {policyId} is disabled");
        }

        // Create archival job
        var job = new ArchivalJob {
            PolicyId = policyId,
            TenantId = policy.TenantId,
            Status = ArchivalJobStatus.Pending,
            StartedAt = DateTime.UtcNow
        };

        await _jobRepository.AddAsync(job, cancellationToken);

        // Execute job asynchronously
        _ = Task.Run(async () => await ExecuteJobAsync(job.Id, cancellationToken), cancellationToken);

        _logger.LogInformation("Started archival job {JobId} for policy {PolicyId}", job.Id, policyId);

        return job.Id;
    }

    public async Task<ArchivalJobDto?> GetJobStatusAsync(Guid jobId, CancellationToken cancellationToken = default) {
        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
        return job == null ? null : MapJobToDto(job);
    }

    private async Task ExecuteJobAsync(Guid jobId, CancellationToken cancellationToken) {
        try {
            var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
            if (job == null) return;

            var policy = await _policyRepository.GetByIdAsync(job.PolicyId, cancellationToken);
            if (policy == null) return;

            job.Status = ArchivalJobStatus.Running;
            await _jobRepository.UpdateAsync(job, cancellationToken);

            // Execute lifecycle operations
            var result = await _lifecycleManager.ExecutePolicyAsync(policy, cancellationToken);

            job.Status = ArchivalJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.ItemsArchived = result.ItemsArchived;
            job.ItemsDeleted = result.ItemsDeleted;
            job.ErrorMessage = result.ErrorMessage;

            policy.LastExecutedAt = DateTime.UtcNow;
            policy.ExecutionCount++;

            await _jobRepository.UpdateAsync(job, cancellationToken);
            await _policyRepository.UpdateAsync(policy, cancellationToken);

            _logger.LogInformation("Completed archival job {JobId}: {ItemsArchived} archived, {ItemsDeleted} deleted",
                jobId, result.ItemsArchived, result.ItemsDeleted);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error executing archival job {JobId}", jobId);

            var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
            if (job != null) {
                job.Status = ArchivalJobStatus.Failed;
                job.CompletedAt = DateTime.UtcNow;
                job.ErrorMessage = ex.Message;
                await _jobRepository.UpdateAsync(job, cancellationToken);
            }
        }
    }

    private ArchivalPolicyDto MapToDto(ArchivalPolicy policy) {
        return new ArchivalPolicyDto(
            policy.Id,
            policy.TenantId,
            policy.Name,
            policy.Description,
            policy.EntityType,
            policy.RetentionDays,
            policy.ArchiveAfterDays,
            policy.DeleteAfterDays,
            policy.StorageTier,
            policy.CompressionEnabled,
            policy.EncryptionEnabled,
            policy.IsEnabled,
            policy.LastExecutedAt,
            policy.ExecutionCount
        );
    }

    private ArchivalJobDto MapJobToDto(ArchivalJob job) {
        return new ArchivalJobDto(
            job.Id,
            job.PolicyId,
            job.TenantId,
            job.Status.ToString(),
            job.StartedAt,
            job.CompletedAt,
            job.ItemsArchived,
            job.ItemsDeleted,
            job.ErrorMessage
        );
    }

    // TODO: Implement remaining IDataArchivalService methods
    public Task<Guid> ExecuteArchivalPolicyAsync(Guid policyId, CancellationToken cancellationToken = default) {
        throw new NotImplementedException("ExecuteArchivalPolicyAsync is not yet implemented");
    }

    public Task MoveToCoolStorageAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default) {
        throw new NotImplementedException("MoveToCoolStorageAsync is not yet implemented");
    }

    public Task MoveToArchiveStorageAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default) {
        throw new NotImplementedException("MoveToArchiveStorageAsync is not yet implemented");
    }

    public Task<bool> RestoreFromArchiveAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default) {
        throw new NotImplementedException("RestoreFromArchiveAsync is not yet implemented");
    }

    public Task<StorageTier> GetStorageTierAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default) {
        throw new NotImplementedException("GetStorageTierAsync is not yet implemented");
    }

    public Task<ArchivalJobDto?> GetArchivalJobStatusAsync(Guid jobId, CancellationToken cancellationToken = default) {
        throw new NotImplementedException("GetArchivalJobStatusAsync is not yet implemented");
    }

    public Task<ArchivalCostSavingsDto> CalculateCostSavingsAsync(Guid? tenantId = null, CancellationToken cancellationToken = default) {
        throw new NotImplementedException("CalculateCostSavingsAsync is not yet implemented");
    }
}
