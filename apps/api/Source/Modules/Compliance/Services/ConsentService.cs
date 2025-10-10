using GameGuild.CQRS;
using GameGuild.Modules.Compliance.Entities;
using GameGuild.Modules.Compliance.Repositories;

namespace GameGuild.Modules.Compliance.Services;

public class ConsentService : IConsentService
{
    private readonly IUserConsentRepository _consentRepository;
    private readonly IComplianceAuditRepository _auditRepository;
    private readonly IConsentPolicyRepository _policyRepository;

    public ConsentService(
        IUserConsentRepository consentRepository,
        IComplianceAuditRepository auditRepository,
        IConsentPolicyRepository policyRepository)
    {
        _consentRepository = consentRepository;
        _auditRepository = auditRepository;
        _policyRepository = policyRepository;
    }

    public async Task<Result<UserConsentDto>> GiveConsentAsync(GiveConsentRequest request, CancellationToken cancellationToken = default)
    {
        var policy = await _policyRepository.GetByIdAsync(request.PolicyId, cancellationToken);
        if (policy == null)
        {
            return Result<UserConsentDto>.Failure("Policy not found");
        }

        var consent = new UserConsent
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            PolicyId = request.PolicyId,
            PolicyVersionId = request.PolicyVersionId,
            TenantId = request.TenantId,
            IsConsented = true,
            ConsentedAt = DateTime.UtcNow,
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent,
            ConsentMethod = request.ConsentMethod,
            Metadata = request.Metadata,
            CreatedAt = DateTime.UtcNow
        };

        await _consentRepository.CreateAsync(consent, cancellationToken);

        // Record audit
        var audit = ComplianceAudit.ForConsent(
            request.UserId,
            request.PolicyId,
            request.PolicyVersionId,
            true,
            request.IpAddress,
            request.UserAgent);
        audit.TenantId = request.TenantId;
        await _auditRepository.CreateAsync(audit, cancellationToken);

        return Result<UserConsentDto>.Success(MapToDto(consent));
    }

    public async Task<Result> WithdrawConsentAsync(Guid consentId, string? reason, CancellationToken cancellationToken = default)
    {
        var consent = await _consentRepository.GetByIdAsync(consentId, cancellationToken);
        if (consent == null)
        {
            return Result.Failure("Consent not found");
        }

        consent.Withdraw(reason);
        await _consentRepository.UpdateAsync(consent, cancellationToken);

        // Record audit
        var audit = ComplianceAudit.ForWithdrawal(
            consent.UserId,
            consentId,
            reason,
            consent.IpAddress);
        audit.TenantId = consent.TenantId;
        await _auditRepository.CreateAsync(audit, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<List<UserConsentDto>>> GetUserConsentsAsync(Guid userId, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var consents = await _consentRepository.GetByUserIdAsync(userId, tenantId, cancellationToken);
        var dtos = consents.Select(MapToDto).ToList();

        return Result<List<UserConsentDto>>.Success(dtos);
    }

    public async Task<Result<bool>> HasValidConsentAsync(Guid userId, Guid policyId, CancellationToken cancellationToken = default)
    {
        var consent = await _consentRepository.GetByUserAndPolicyAsync(userId, policyId, cancellationToken);
        var isValid = consent?.IsValid() ?? false;

        return Result<bool>.Success(isValid);
    }

    public async Task<Result<List<UserConsentDto>>> GetPolicyConsentsAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        var consents = await _consentRepository.GetByPolicyIdAsync(policyId, cancellationToken);
        var dtos = consents.Select(MapToDto).ToList();

        return Result<List<UserConsentDto>>.Success(dtos);
    }

    public async Task<Result> RecordAuditAsync(RecordAuditRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<AuditEventType>(request.EventType, out var eventType))
        {
            return Result.Failure($"Invalid event type: {request.EventType}");
        }

        if (!Enum.TryParse<AuditSeverity>(request.Severity, out var severity))
        {
            return Result.Failure($"Invalid severity: {request.Severity}");
        }

        var audit = new ComplianceAudit
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            UserId = request.UserId,
            EventType = eventType,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            Action = request.Action,
            OldValues = request.OldValues,
            NewValues = request.NewValues,
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent,
            OccurredAt = DateTime.UtcNow,
            Metadata = request.Metadata,
            Regulation = request.Regulation,
            Severity = severity,
            CreatedAt = DateTime.UtcNow
        };

        await _auditRepository.CreateAsync(audit, cancellationToken);

        return Result.Success();
    }

    private static UserConsentDto MapToDto(UserConsent consent) =>
        new(
            consent.Id,
            consent.UserId,
            consent.PolicyId,
            consent.PolicyVersionId,
            consent.TenantId,
            consent.IsConsented,
            consent.ConsentedAt,
            consent.IpAddress,
            consent.UserAgent,
            consent.WithdrawnAt,
            consent.WithdrawalReason,
            consent.ExpiresAt,
            consent.ConsentMethod,
            consent.Metadata);
}
