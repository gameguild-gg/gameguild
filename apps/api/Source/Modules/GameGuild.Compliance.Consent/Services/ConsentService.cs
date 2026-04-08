namespace GameGuild.Compliance.Consent;

public class ConsentService(
    IConsentPolicyRepository policyRepository,
    IPolicyVersionRepository versionRepository,
    IUserConsentRepository consentRepository,
    IDataSubjectRequestRepository dsrRepository) : IConsentService
{
    public async Task<ConsentPolicyDto> CreatePolicyAsync(string name, PolicyType type, bool isMandatory, string? description, CancellationToken ct = default)
    {
        var policy = new ConsentPolicy
        {
            Name = name,
            PolicyType = type,
            IsMandatory = isMandatory,
            Description = description
        };
        policy = await policyRepository.AddAsync(policy, ct).ConfigureAwait(false);
        return ToDto(policy);
    }

    public async Task<PolicyVersionDto> PublishVersionAsync(Guid policyId, string versionNumber, string content, ContentType contentType, CancellationToken ct = default)
    {
        var currentVersion = await versionRepository.GetCurrentVersionAsync(policyId, ct).ConfigureAwait(false);
        if (currentVersion != null)
        {
            currentVersion.IsCurrent = false;
            currentVersion.EffectiveUntil = DateTime.UtcNow;
        }

        var version = new PolicyVersion
        {
            ConsentPolicyId = policyId,
            VersionNumber = versionNumber,
            Content = content,
            ContentType = contentType,
            EffectiveFrom = DateTime.UtcNow,
            IsCurrent = true
        };
        version = await versionRepository.AddAsync(version, ct).ConfigureAwait(false);
        return new PolicyVersionDto(version.Id, policyId, version.VersionNumber, version.ContentType, version.EffectiveFrom, version.IsCurrent);
    }

    public async Task<List<ConsentPolicyDto>> GetActivePoliciesAsync(Guid? tenantId, CancellationToken ct = default)
    {
        var policies = await policyRepository.GetAllActiveAsync(tenantId, ct).ConfigureAwait(false);
        return policies.Select(ToDto).ToList();
    }

    public async Task<UserConsentDto> GrantConsentAsync(Guid userId, Guid policyVersionId, string? ipAddress, string? userAgent, string? method, CancellationToken ct = default)
    {
        var existing = await consentRepository.GetAsync(userId, policyVersionId, ct).ConfigureAwait(false);
        if (existing != null && existing.IsGranted)
            return ToDto(existing);

        var consent = new UserConsent
        {
            UserId = userId,
            PolicyVersionId = policyVersionId,
            IsGranted = true,
            ConsentGivenAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ConsentMethod = method
        };
        consent = await consentRepository.AddAsync(consent, ct).ConfigureAwait(false);
        return ToDto(consent);
    }

    public async Task RevokeConsentAsync(Guid userId, Guid policyVersionId, CancellationToken ct = default)
    {
        var consent = await consentRepository.GetAsync(userId, policyVersionId, ct).ConfigureAwait(false);
        if (consent == null || !consent.IsGranted) return;
        consent.Revoke();
        await consentRepository.UpdateAsync(consent, ct).ConfigureAwait(false);
    }

    public async Task<List<UserConsentDto>> GetUserConsentsAsync(Guid userId, CancellationToken ct = default)
    {
        var consents = await consentRepository.GetByUserAsync(userId, ct).ConfigureAwait(false);
        return consents.Select(ToDto).ToList();
    }

    public async Task<DataSubjectRequestDto> SubmitDataSubjectRequestAsync(Guid userId, DataSubjectRequestType type, string? description, CancellationToken ct = default)
    {
        var request = new DataSubjectRequest
        {
            UserId = userId,
            RequestType = type,
            Description = description,
            Deadline = DateTime.UtcNow.AddDays(30)
        };
        request = await dsrRepository.AddAsync(request, ct).ConfigureAwait(false);
        return ToDto(request);
    }

    public async Task<DataSubjectRequestDto> ProcessDataSubjectRequestAsync(Guid requestId, Guid processedBy, string? notes, CancellationToken ct = default)
    {
        var request = await dsrRepository.GetByIdAsync(requestId, ct).ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"Data subject request {requestId} not found.");
        request.Complete(processedBy, notes);
        await dsrRepository.UpdateAsync(request, ct).ConfigureAwait(false);
        return ToDto(request);
    }

    public async Task<List<DataSubjectRequestDto>> GetPendingRequestsAsync(CancellationToken ct = default)
    {
        var requests = await dsrRepository.GetPendingAsync(ct).ConfigureAwait(false);
        return requests.Select(ToDto).ToList();
    }

    private static ConsentPolicyDto ToDto(ConsentPolicy p) =>
        new(p.Id, p.Name, p.PolicyType, p.IsMandatory, p.IsActive,
            p.Versions?.FirstOrDefault(v => v.IsCurrent)?.VersionNumber);

    private static UserConsentDto ToDto(UserConsent c) =>
        new(c.Id, c.UserId, c.PolicyVersionId, c.IsGranted, c.ConsentGivenAt, c.ConsentRevokedAt, c.ConsentMethod);

    private static DataSubjectRequestDto ToDto(DataSubjectRequest r) =>
        new(r.Id, r.UserId, r.RequestType, r.Status, r.Deadline, r.ProcessedAt, r.ProcessingNotes);
}
