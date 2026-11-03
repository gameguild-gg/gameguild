using GameGuild.Identity.Context.Actors;

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
    Task<int> ExpireOverdueInspectionRequestsAsync(DateTime utcNow, CancellationToken ct = default);
}

public sealed class FerpaService(
    IFerpaEducationRecordRepository recordRepository,
    IFerpaDirectoryInformationPolicyRepository policyRepository,
    IFerpaDisclosureConsentRepository consentRepository,
    IFerpaDisclosureLogRepository disclosureLogRepository,
    IFerpaInspectionRequestRepository requestRepository,
    IActorContextAccessor? actorContextAccessor = null) : IFerpaService
{
    private FerpaRequestActor? CurrentActor => FerpaRequestActor.From(actorContextAccessor?.ActorContext);

    public async Task<FerpaEducationRecordDto> RegisterEducationRecordAsync(RegisterEducationRecordCommand command, CancellationToken ct = default)
    {
        // Issue #1220 N5: education records may only be registered by administrators,
        // and a tenant administrator is pinned to its own tenant (a null/global tenant
        // is reserved for the system administrator).
        var actor = RequireActor();
        if (!actor.IsSystemAdmin &&
            !(actor.IsTenantAdmin && command.TenantId.HasValue && command.TenantId.Value == actor.TenantId))
        {
            throw new FerpaAuthorizationException(
                $"FERPA education records may only be registered by an administrator of tenant {command.TenantId}.");
        }

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
    {
        var actor = RequireActor();
        RequireSelfOrAdmin(actor, studentUserId, "read-student-records");
        var records = await recordRepository.GetByStudentAsync(studentUserId, ct).ConfigureAwait(false);
        return ClampRecordsToTenantScope(actor, studentUserId, records).Select(record => record.ToDto()).ToList();
    }

    public async Task<List<FerpaEducationRecordDto>> GetDirectoryInformationAsync(Guid studentUserId, CancellationToken ct = default)
    {
        var actor = RequireActor();
        RequireSelfOrAdmin(actor, studentUserId, "read-directory-information");
        var records = ClampRecordsToTenantScope(
            actor,
            studentUserId,
            await recordRepository.GetDirectoryInformationAsync(studentUserId, ct).ConfigureAwait(false));

        // Issue #1220 N5 (spec N6): the directory-information read must honor the owning
        // tenant's opt-out regime — directory information of a tenant that runs no
        // opt-out policy is not disclosed. Grouped per owning tenant so one lookup
        // covers each distinct tenant.
        var visible = new List<FerpaEducationRecord>();
        foreach (var group in records.GroupBy(record => record.TenantId))
        {
            var policy = await policyRepository.GetByTenantAsync(group.Key, ct).ConfigureAwait(false);
            if (policy?.OptOutEnabled == true)
            {
                visible.AddRange(group);
            }
        }

        return visible.Select(record => record.ToDto()).ToList();
    }

    public async Task<FerpaDirectoryInformationPolicyDto> UpsertDirectoryPolicyAsync(UpsertDirectoryInformationPolicyCommand command, CancellationToken ct = default)
    {
        // Issue #1220 N5: the tenant's FERPA notice regime is administrative configuration.
        var actor = RequireActor();
        if (!actor.IsSystemAdmin && !(actor.IsTenantAdmin && command.TenantId == actor.TenantId))
        {
            throw new FerpaAuthorizationException(
                $"The directory-information policy of tenant {command.TenantId} may only be configured by that tenant's administrators.");
        }

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
    {
        // Issue #1220 N5: non-administrators see only their own tenant's (or the global)
        // notice; cross-tenant reads fail closed.
        var actor = RequireActor();
        if (actor.IsSystemAdmin || actor.IsTenantAdmin)
        {
            return (await policyRepository.GetByTenantAsync(tenantId, ct).ConfigureAwait(false))?.ToDto();
        }

        if (tenantId.HasValue && tenantId.Value != actor.TenantId)
        {
            throw new FerpaAuthorizationException("Directory-information policies of other tenants are not visible to this actor.");
        }

        return (await policyRepository.GetByTenantAsync(actor.TenantId, ct).ConfigureAwait(false))?.ToDto();
    }

    public async Task<FerpaDisclosureConsentDto> GrantDisclosureConsentAsync(GrantFerpaDisclosureConsentCommand command, CancellationToken ct = default)
    {
        // Issue #1220 N5: the student (or an administrator) grants disclosure consent.
        var actor = RequireActor();
        RequireSelfOrAdmin(actor, command.StudentUserId, "grant-disclosure-consent");

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
        var actor = RequireActor();
        var consent = await consentRepository.GetByIdAsync(consentId, ct).ConfigureAwait(false);
        if (consent is null)
        {
            return false;
        }

        // Issue #1220 N5: revocation belongs to the consenting student; administrators
        // additionally need a matching tenant scope.
        if (actor.ActorUserId != consent.StudentUserId)
        {
            RequireTenantScope(actor, consent.TenantId, "revoke-disclosure-consent");
        }

        consent.Revoke();
        await consentRepository.UpdateAsync(consent, ct).ConfigureAwait(false);
        return true;
    }

    public async Task<List<FerpaDisclosureConsentDto>> GetStudentConsentsAsync(Guid studentUserId, CancellationToken ct = default)
    {
        var actor = RequireActor();
        RequireSelfOrAdmin(actor, studentUserId, "read-disclosure-consents");
        var consents = await consentRepository.GetByStudentAsync(studentUserId, ct).ConfigureAwait(false);
        consents = ClampTenantScopedRows(actor, studentUserId, consents, consent => consent.TenantId);
        return consents.Select(consent => consent.ToDto()).ToList();
    }

    public async Task<FerpaDisclosureLogDto> RecordDisclosureAsync(RecordFerpaDisclosureCommand command, CancellationToken ct = default)
    {
        // Issue #1220 N5: disclosures are made by the student or an administrator.
        var actor = RequireActor();
        RequireSelfOrAdmin(actor, command.StudentUserId, "record-disclosure");

        // Issue #1220 N4: the disclosing party is resolved server-side from the
        // authenticated actor; a body-supplied DisclosedByUserId is never trusted.
        command = command with { DisclosedByUserId = actor.ActorUserId };

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

        if (command.Basis is FerpaDisclosureBasis.DirectoryInformation)
        {
            // Issue #1220 N5 (spec N6): the directory-information opt-out policy is now
            // enforced on the disclosure path. A tenant that runs no opt-out regime (no
            // policy row, or OptOutEnabled = false) may not disclose directory
            // information without an explicit consent basis — fail closed.
            var policy = await policyRepository.GetByTenantAsync(actor.TenantId, ct).ConfigureAwait(false);
            if (policy is null || !policy.OptOutEnabled)
            {
                throw new FerpaAuthorizationException(
                    "FERPA directory-information disclosure requires the acting tenant to run a directory-information opt-out policy; use an explicit consent basis instead.");
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
    {
        var actor = RequireActor();
        RequireSelfOrAdmin(actor, studentUserId, "read-disclosure-logs");
        var logs = await disclosureLogRepository.GetByStudentAsync(studentUserId, ct).ConfigureAwait(false);
        logs = ClampTenantScopedRows(actor, studentUserId, logs, log => log.TenantId);
        return logs.Select(log => log.ToDto()).ToList();
    }

    public async Task<FerpaInspectionRequestDto> SubmitInspectionRequestAsync(SubmitFerpaInspectionRequestCommand command, CancellationToken ct = default)
    {
        // Issue #1220 N5: the student (or an administrator) requests inspection of the
        // student's own records.
        var actor = RequireActor();
        RequireSelfOrAdmin(actor, command.StudentUserId, "submit-inspection-request");

        // Issue #1220 N4: the requester is resolved server-side from the authenticated
        // actor; a body-supplied RequestedByUserId is never trusted.
        command = command with { RequestedByUserId = actor.ActorUserId };

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
        // Issue #1220 N5: completing (or denying) an inspection request is an
        // administrative action on the stored request.
        var actor = RequireActor();
        var request = await requestRepository.GetByIdAsync(command.RequestId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"FERPA inspection request {command.RequestId} not found.");
        RequireTenantScope(actor, request.TenantId, "complete-inspection-request");

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
    {
        var actor = RequireActor();
        if (!actor.IsSystemAdmin && !actor.IsTenantAdmin)
        {
            throw new FerpaAuthorizationException("Pending FERPA inspection requests are visible to administrators only.");
        }

        var requests = await requestRepository.GetPendingAsync(ct).ConfigureAwait(false);
        if (!actor.IsSystemAdmin)
        {
            requests = requests.Where(request => request.TenantId.HasValue && request.TenantId.Value == actor.TenantId).ToList();
        }

        return requests.Select(request => request.ToDto()).ToList();
    }

    /// <summary>
    ///     Expires every live inspection request whose 45-day statutory deadline has
    ///     passed (issue #1219 N3). Called by the host's compliance deadline sweeper;
    ///     safe to run repeatedly (idempotent state transition).
    /// </summary>
    public async Task<int> ExpireOverdueInspectionRequestsAsync(DateTime utcNow, CancellationToken ct = default)
    {
        var overdue = await requestRepository.GetOverdueAsync(utcNow, ct).ConfigureAwait(false);
        foreach (var request in overdue)
        {
            request.Expire();
            await requestRepository.UpdateAsync(request, ct).ConfigureAwait(false);
        }

        return overdue.Count;
    }

    private FerpaRequestActor RequireActor()
        => CurrentActor ?? throw new FerpaAuthorizationException("FERPA operations require an authenticated actor.");

    private static void RequireSelfOrAdmin(FerpaRequestActor actor, Guid studentUserId, string operation)
    {
        if (actor.ActorUserId == studentUserId)
        {
            return;
        }

        if (actor.IsSystemAdmin || actor.IsTenantAdmin)
        {
            return;
        }

        throw new FerpaAuthorizationException(
            $"FERPA operation '{operation}' on student {studentUserId} is reserved for the student or an administrator.");
    }

    private static void RequireTenantScope(FerpaRequestActor actor, Guid? resourceTenantId, string operation)
    {
        if (actor.IsSystemAdmin)
        {
            return;
        }

        if (actor.IsTenantAdmin && resourceTenantId.HasValue && resourceTenantId.Value == actor.TenantId)
        {
            return;
        }

        throw new FerpaAuthorizationException(
            $"FERPA operation '{operation}' requires an administrator of the resource's tenant.");
    }

    private static List<FerpaEducationRecord> ClampRecordsToTenantScope(
        FerpaRequestActor actor,
        Guid studentUserId,
        List<FerpaEducationRecord> records)
    {
        if (actor.ActorUserId == studentUserId || actor.IsSystemAdmin)
        {
            return records;
        }

        return records.Where(record => record.TenantId.HasValue && record.TenantId.Value == actor.TenantId).ToList();
    }

    private static List<T> ClampTenantScopedRows<T>(FerpaRequestActor actor, Guid studentUserId, List<T> rows, Func<T, Guid?> tenantSelector)
    {
        if (actor.ActorUserId == studentUserId || actor.IsSystemAdmin)
        {
            return rows;
        }

        return rows.Where(row => tenantSelector(row).HasValue && tenantSelector(row)!.Value == actor.TenantId).ToList();
    }
}
