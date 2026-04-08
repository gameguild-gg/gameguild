namespace GameGuild.Compliance.Consent;

public interface IConsentService
{
    Task<ConsentPolicyDto> CreatePolicyAsync(string name, PolicyType type, bool isMandatory, string? description, CancellationToken ct = default);
    Task<PolicyVersionDto> PublishVersionAsync(Guid policyId, string versionNumber, string content, ContentType contentType, CancellationToken ct = default);
    Task<List<ConsentPolicyDto>> GetActivePoliciesAsync(Guid? tenantId, CancellationToken ct = default);
    Task<UserConsentDto> GrantConsentAsync(Guid userId, Guid policyVersionId, string? ipAddress, string? userAgent, string? method, CancellationToken ct = default);
    Task RevokeConsentAsync(Guid userId, Guid policyVersionId, CancellationToken ct = default);
    Task<List<UserConsentDto>> GetUserConsentsAsync(Guid userId, CancellationToken ct = default);
    Task<DataSubjectRequestDto> SubmitDataSubjectRequestAsync(Guid userId, DataSubjectRequestType type, string? description, CancellationToken ct = default);
    Task<DataSubjectRequestDto> ProcessDataSubjectRequestAsync(Guid requestId, Guid processedBy, string? notes, CancellationToken ct = default);
    Task<List<DataSubjectRequestDto>> GetPendingRequestsAsync(CancellationToken ct = default);
}

public record ConsentPolicyDto(Guid Id, string Name, PolicyType PolicyType, bool IsMandatory, bool IsActive, string? CurrentVersion);
public record PolicyVersionDto(Guid Id, Guid PolicyId, string VersionNumber, ContentType ContentType, DateTime EffectiveFrom, bool IsCurrent);
public record UserConsentDto(Guid Id, Guid UserId, Guid PolicyVersionId, bool IsGranted, DateTime ConsentGivenAt, DateTime? ConsentRevokedAt, string? ConsentMethod);
public record DataSubjectRequestDto(Guid Id, Guid UserId, DataSubjectRequestType RequestType, DataSubjectRequestStatus Status, DateTime Deadline, DateTime? ProcessedAt, string? ProcessingNotes);
