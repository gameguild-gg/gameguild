using GameGuild.Modules.Compliance.Entities;
using GameGuild.Modules.Compliance.Repositories;

namespace GameGuild.Modules.Compliance.Services;

public class ComplianceService : IComplianceService
{
    private readonly IConsentPolicyRepository _policyRepository;
    private readonly IPolicyVersionRepository _versionRepository;
    private readonly IComplianceAuditRepository _auditRepository;

    public ComplianceService(
        IConsentPolicyRepository policyRepository,
        IPolicyVersionRepository versionRepository,
        IComplianceAuditRepository auditRepository)
    {
        _policyRepository = policyRepository;
        _versionRepository = versionRepository;
        _auditRepository = auditRepository;
    }

    public async Task<Result<ConsentPolicyDto>> CreatePolicyAsync(CreatePolicyRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<PolicyType>(request.Type, out var policyType))
        {
            return Result<ConsentPolicyDto>.Failure($"Invalid policy type: {request.Type}");
        }

        var policy = new ConsentPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Name = request.Name,
            Type = policyType,
            Description = request.Description,
            IsActive = false,
            RequiresConsent = request.RequiresConsent,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _policyRepository.CreateAsync(policy, cancellationToken);

        return Result<ConsentPolicyDto>.Success(MapToDto(policy));
    }

    public async Task<Result<ConsentPolicyDto>> UpdatePolicyAsync(Guid policyId, UpdatePolicyRequest request, CancellationToken cancellationToken = default)
    {
        var policy = await _policyRepository.GetByIdAsync(policyId, cancellationToken);
        if (policy == null)
        {
            return Result<ConsentPolicyDto>.Failure("Policy not found");
        }

        if (request.Name != null) policy.Name = request.Name;
        if (request.Description != null) policy.Description = request.Description;
        if (request.RequiresConsent.HasValue) policy.RequiresConsent = request.RequiresConsent.Value;
        policy.UpdatedAt = DateTime.UtcNow;

        await _policyRepository.UpdateAsync(policy, cancellationToken);

        return Result<ConsentPolicyDto>.Success(MapToDto(policy));
    }

    public async Task<Result> PublishPolicyAsync(Guid policyId, Guid versionId, CancellationToken cancellationToken = default)
    {
        var policy = await _policyRepository.GetByIdAsync(policyId, cancellationToken);
        if (policy == null)
        {
            return Result.Failure("Policy not found");
        }

        var version = await _versionRepository.GetByIdAsync(versionId, cancellationToken);
        if (version == null || version.PolicyId != policyId)
        {
            return Result.Failure("Version not found or does not belong to policy");
        }

        policy.Publish(versionId);
        await _policyRepository.UpdateAsync(policy, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeactivatePolicyAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        var policy = await _policyRepository.GetByIdAsync(policyId, cancellationToken);
        if (policy == null)
        {
            return Result.Failure("Policy not found");
        }

        policy.Deactivate();
        await _policyRepository.UpdateAsync(policy, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<PolicyVersionDto>> CreatePolicyVersionAsync(Guid policyId, CreateVersionRequest request, CancellationToken cancellationToken = default)
    {
        var policy = await _policyRepository.GetByIdAsync(policyId, cancellationToken);
        if (policy == null)
        {
            return Result<PolicyVersionDto>.Failure("Policy not found");
        }

        if (!Enum.TryParse<ContentType>(request.ContentType, out var contentType))
        {
            return Result<PolicyVersionDto>.Failure($"Invalid content type: {request.ContentType}");
        }

        var version = new PolicyVersion
        {
            Id = Guid.NewGuid(),
            PolicyId = policyId,
            VersionNumber = request.VersionNumber,
            Content = request.Content,
            ContentType = contentType,
            ChangeLog = request.ChangeLog,
            EffectiveDate = request.EffectiveDate,
            ExpiresAt = request.ExpiresAt,
            IsCurrent = false,
            CreatedByUserId = request.CreatedByUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _versionRepository.CreateAsync(version, cancellationToken);

        return Result<PolicyVersionDto>.Success(MapToDto(version));
    }

    public async Task<Result<ConsentPolicyDto>> GetPolicyAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        var policy = await _policyRepository.GetByIdAsync(policyId, cancellationToken);
        if (policy == null)
        {
            return Result<ConsentPolicyDto>.Failure("Policy not found");
        }

        return Result<ConsentPolicyDto>.Success(MapToDto(policy));
    }

    public async Task<Result<List<ConsentPolicyDto>>> GetPoliciesAsync(Guid? tenantId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var policies = await _policyRepository.GetAllAsync(tenantId, includeInactive, cancellationToken);
        var dtos = policies.Select(MapToDto).ToList();

        return Result<List<ConsentPolicyDto>>.Success(dtos);
    }

    public async Task<Result<List<ComplianceAuditDto>>> GetAuditLogAsync(AuditLogRequest request, CancellationToken cancellationToken = default)
    {
        var audits = await _auditRepository.GetAuditLogAsync(
            request.TenantId,
            request.UserId,
            request.EventType != null ? Enum.Parse<AuditEventType>(request.EventType) : null,
            request.StartDate,
            request.EndDate,
            request.Skip,
            request.Take,
            cancellationToken);

        var dtos = audits.Select(MapToDto).ToList();

        return Result<List<ComplianceAuditDto>>.Success(dtos);
    }

    private static ConsentPolicyDto MapToDto(ConsentPolicy policy) =>
        new(
            policy.Id,
            policy.TenantId,
            policy.Name,
            policy.Type.ToString(),
            policy.Description,
            policy.IsActive,
            policy.RequiresConsent,
            policy.CurrentVersionId,
            policy.PublishedAt,
            policy.CreatedAt);

    private static PolicyVersionDto MapToDto(PolicyVersion version) =>
        new(
            version.Id,
            version.PolicyId,
            version.VersionNumber,
            version.Content,
            version.ContentType.ToString(),
            version.ChangeLog,
            version.EffectiveDate,
            version.ExpiresAt,
            version.IsCurrent,
            version.CreatedByUserId,
            version.CreatedAt);

    private static ComplianceAuditDto MapToDto(ComplianceAudit audit) =>
        new(
            audit.Id,
            audit.TenantId,
            audit.UserId,
            audit.EventType.ToString(),
            audit.EntityType,
            audit.EntityId,
            audit.Action,
            audit.OldValues,
            audit.NewValues,
            audit.IpAddress,
            audit.UserAgent,
            audit.OccurredAt,
            audit.Metadata,
            audit.Regulation,
            audit.Severity.ToString());
}
