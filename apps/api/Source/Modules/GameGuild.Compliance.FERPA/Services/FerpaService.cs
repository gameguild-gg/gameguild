namespace GameGuild.Compliance.FERPA;

public interface IFerpaService
{
    Task<FerpaEducationRecordDto> RegisterEducationRecordAsync(RegisterEducationRecordCommand command, CancellationToken ct = default);
    Task<List<FerpaEducationRecordDto>> GetStudentRecordsAsync(Guid studentUserId, CancellationToken ct = default);
    Task<List<FerpaEducationRecordDto>> GetDirectoryInformationAsync(Guid studentUserId, CancellationToken ct = default);
    Task<FerpaDirectoryInformationPolicyDto> UpsertDirectoryPolicyAsync(UpsertDirectoryInformationPolicyCommand command, CancellationToken ct = default);
    Task<FerpaDirectoryInformationPolicyDto?> GetDirectoryPolicyAsync(Guid? tenantId, CancellationToken ct = default);
    Task<FerpaDisclosureConsentDto> GrantDisclosureConsentAsync(GrantFerpaDisclosureConsentCommand command, CancellationToken ct = default);
    Task<bool> RevokeDisclosureConsentAsync(Guid consentId, CancellationToken ct = default);
    Task<List<FerpaDisclosureConsentDto>> GetStudentConsentsAsync(Guid studentUserId, CancellationToken ct = default);
    Task<FerpaDisclosureLogDto> RecordDisclosureAsync(RecordFerpaDisclosureCommand command, CancellationToken ct = default);
    Task<List<FerpaDisclosureLogDto>> GetDisclosureLogsAsync(Guid studentUserId, CancellationToken ct = default);
    Task<FerpaInspectionRequestDto> SubmitInspectionRequestAsync(SubmitFerpaInspectionRequestCommand command, CancellationToken ct = default);
    Task<FerpaInspectionRequestDto> CompleteInspectionRequestAsync(CompleteFerpaInspectionRequestCommand command, CancellationToken ct = default);
    Task<List<FerpaInspectionRequestDto>> GetPendingInspectionRequestsAsync(CancellationToken ct = default);
}

public sealed class FerpaService(
    IFerpaEducationRecordRepository recordRepository,
    IFerpaDirectoryInformationPolicyRepository policyRepository,
    IFerpaDisclosureConsentRepository consentRepository,
    IFerpaDisclosureLogRepository disclosureLogRepository,
    IFerpaInspectionRequestRepository requestRepository) : IFerpaService
{
    public async Task<FerpaEducationRecordDto> RegisterEducationRecordAsync(RegisterEducationRecordCommand command, CancellationToken ct = default)
    {
        var record = new FerpaEducationRecord
        {
            StudentUserId = command.StudentUserId,
            TenantId = command.TenantId,
            RecordKind = command.RecordKind,
            ExternalRecordId = command.ExternalRecordId,
            Title = command.Title,
            ProtectionLevel = command.ProtectionLevel,
            IsDirectoryInformation = command.IsDirectoryInformation,
            RetentionUntil = command.RetentionUntil,
            MetadataJson = command.MetadataJson
        };

        return (await recordRepository.AddAsync(record, ct).ConfigureAwait(false)).ToDto();
    }

    public async Task<List<FerpaEducationRecordDto>> GetStudentRecordsAsync(Guid studentUserId, CancellationToken ct = default)
        => (await recordRepository.GetByStudentAsync(studentUserId, ct).ConfigureAwait(false)).Select(record => record.ToDto()).ToList();

    public async Task<List<FerpaEducationRecordDto>> GetDirectoryInformationAsync(Guid studentUserId, CancellationToken ct = default)
        => (await recordRepository.GetDirectoryInformationAsync(studentUserId, ct).ConfigureAwait(false)).Select(record => record.ToDto()).ToList();

    public async Task<FerpaDirectoryInformationPolicyDto> UpsertDirectoryPolicyAsync(UpsertDirectoryInformationPolicyCommand command, CancellationToken ct = default)
    {
        var existing = await policyRepository.GetByTenantAsync(command.TenantId, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            existing.Update(command.AllowedFieldsJson, command.OptOutEnabled, command.AnnualNoticeSentAt, command.NoticeUrl);
            await policyRepository.UpdateAsync(existing, ct).ConfigureAwait(false);
            return existing.ToDto();
        }

        var policy = new FerpaDirectoryInformationPolicy
        {
            TenantId = command.TenantId,
            AllowedFieldsJson = command.AllowedFieldsJson,
            OptOutEnabled = command.OptOutEnabled,
            AnnualNoticeSentAt = command.AnnualNoticeSentAt,
            NoticeUrl = command.NoticeUrl
        };

        return (await policyRepository.AddAsync(policy, ct).ConfigureAwait(false)).ToDto();
    }

    public async Task<FerpaDirectoryInformationPolicyDto?> GetDirectoryPolicyAsync(Guid? tenantId, CancellationToken ct = default)
        => (await policyRepository.GetByTenantAsync(tenantId, ct).ConfigureAwait(false))?.ToDto();

    public async Task<FerpaDisclosureConsentDto> GrantDisclosureConsentAsync(GrantFerpaDisclosureConsentCommand command, CancellationToken ct = default)
    {
        var consent = new FerpaDisclosureConsent
        {
            StudentUserId = command.StudentUserId,
            GuardianUserId = command.GuardianUserId,
            Recipient = command.Recipient,
            Purpose = command.Purpose,
            Scope = command.Scope,
            EffectiveFrom = command.EffectiveFrom,
            ExpiresAt = command.ExpiresAt
        };

        return (await consentRepository.AddAsync(consent, ct).ConfigureAwait(false)).ToDto();
    }

    public async Task<bool> RevokeDisclosureConsentAsync(Guid consentId, CancellationToken ct = default)
    {
        var consent = await consentRepository.GetByIdAsync(consentId, ct).ConfigureAwait(false);
        if (consent is null)
        {
            return false;
        }

        consent.Revoke();
        await consentRepository.UpdateAsync(consent, ct).ConfigureAwait(false);
        return true;
    }

    public async Task<List<FerpaDisclosureConsentDto>> GetStudentConsentsAsync(Guid studentUserId, CancellationToken ct = default)
        => (await consentRepository.GetByStudentAsync(studentUserId, ct).ConfigureAwait(false)).Select(consent => consent.ToDto()).ToList();

    public async Task<FerpaDisclosureLogDto> RecordDisclosureAsync(RecordFerpaDisclosureCommand command, CancellationToken ct = default)
    {
        if (command.Basis is FerpaDisclosureBasis.StudentConsent or FerpaDisclosureBasis.GuardianConsent)
        {
            var consent = await consentRepository.GetActiveAsync(
                command.StudentUserId,
                command.Recipient,
                command.Scope,
                command.DisclosedAt,
                ct).ConfigureAwait(false);

            if (consent is null)
            {
                throw new InvalidOperationException("FERPA disclosure requires an active matching consent.");
            }
        }

        var log = new FerpaDisclosureLog
        {
            StudentUserId = command.StudentUserId,
            DisclosedByUserId = command.DisclosedByUserId,
            Recipient = command.Recipient,
            Basis = command.Basis,
            Purpose = command.Purpose,
            RecordIdsJson = command.RecordIdsJson,
            DisclosedAt = command.DisclosedAt
        };

        return (await disclosureLogRepository.AddAsync(log, ct).ConfigureAwait(false)).ToDto();
    }

    public async Task<List<FerpaDisclosureLogDto>> GetDisclosureLogsAsync(Guid studentUserId, CancellationToken ct = default)
        => (await disclosureLogRepository.GetByStudentAsync(studentUserId, ct).ConfigureAwait(false)).Select(log => log.ToDto()).ToList();

    public async Task<FerpaInspectionRequestDto> SubmitInspectionRequestAsync(SubmitFerpaInspectionRequestCommand command, CancellationToken ct = default)
    {
        var request = new FerpaInspectionRequest
        {
            StudentUserId = command.StudentUserId,
            RequestedByUserId = command.RequestedByUserId,
            Description = command.Description,
            Deadline = command.Deadline
        };

        return (await requestRepository.AddAsync(request, ct).ConfigureAwait(false)).ToDto();
    }

    public async Task<FerpaInspectionRequestDto> CompleteInspectionRequestAsync(CompleteFerpaInspectionRequestCommand command, CancellationToken ct = default)
    {
        var request = await requestRepository.GetByIdAsync(command.RequestId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"FERPA inspection request {command.RequestId} not found.");

        if (command.Approved)
        {
            request.Complete(command.ProcessedByUserId, command.Notes);
        }
        else
        {
            request.Deny(command.ProcessedByUserId, command.Notes ?? "Denied");
        }

        await requestRepository.UpdateAsync(request, ct).ConfigureAwait(false);
        return request.ToDto();
    }

    public async Task<List<FerpaInspectionRequestDto>> GetPendingInspectionRequestsAsync(CancellationToken ct = default)
        => (await requestRepository.GetPendingAsync(ct).ConfigureAwait(false)).Select(request => request.ToDto()).ToList();
}
