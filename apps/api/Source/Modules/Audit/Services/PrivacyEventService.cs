using GameGuild.Modules.Audit;

namespace GameGuild.Modules.Audit.Services;

/// <summary>
/// Service interface for privacy and compliance event logging.
/// Handles GDPR, CCPA, and other privacy regulation requirements.
/// </summary>
public interface IPrivacyEventService
{
    Task<PrivacyEvent> LogConsentChangeAsync(Guid tenantId, Guid userId, string consentType, bool consentGranted, string? consentPurpose, string? legalBasis, DateTime? expiresAt, string ipAddress, string userAgent, string? country = null);
    Task<PrivacyEvent> LogDataExportRequestAsync(Guid tenantId, Guid userId, string exportFormat, string exportScope, string requestReference, string ipAddress, string userAgent, string? country = null);
    Task<PrivacyEvent> LogRightToBeForgottenRequestAsync(Guid tenantId, Guid userId, string deletionScope, string requestReference, string? retentionRequirement, string ipAddress, string userAgent, string? country = null);
    Task<PrivacyEvent> CreatePrivacyImpactAssessmentAsync(Guid tenantId, string piaReference, string dataCategories, string processingPurpose, string riskLevel, string? mitigationMeasures, string processedBy, string? approvedBy = null);
    Task<PrivacyEvent> CreateGdprArticle30RecordAsync(Guid tenantId, string processingActivity, string dataController, string? dataProcessor, string lawfulBasis, string dataCategories, string? dataRecipients, string? internationalTransfers, string retentionPeriod, string securityMeasures, string processedBy);
    Task<PrivacyEvent> CompleteDataExportAsync(Guid eventId, string exportLocation, long exportSizeBytes, DateTime expiresAt);
    Task<PrivacyEvent> CompleteDeletionAsync(Guid eventId, string deletionMethod, bool isAnonymized);
    Task<IEnumerable<PrivacyEvent>> GetPrivacyEventsByUserAsync(Guid userId);
    Task<IEnumerable<PrivacyEvent>> GetPrivacyEventsByTenantAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<PrivacyEvent>> GetPendingDataSubjectRequestsAsync(Guid tenantId);
}

/// <summary>
/// Implementation of privacy event service with database persistence.
/// </summary>
public sealed class PrivacyEventService : IPrivacyEventService
{
    private readonly IPrivacyEventRepository _repository;

    public PrivacyEventService(IPrivacyEventRepository repository)
    {
        _repository = repository;
    }

    public async Task<PrivacyEvent> LogConsentChangeAsync(Guid tenantId, Guid userId, string consentType, bool consentGranted, string? consentPurpose, string? legalBasis, DateTime? expiresAt, string ipAddress, string userAgent, string? country = null)
    {
        var privacyEvent = PrivacyEvent.CreateConsentChangeEvent(
            tenantId, userId, consentType, consentGranted, consentPurpose,
            legalBasis, expiresAt, ipAddress, userAgent, country);

        await _repository.AddAsync(privacyEvent);
        return privacyEvent;
    }

    public async Task<PrivacyEvent> LogDataExportRequestAsync(Guid tenantId, Guid userId, string exportFormat, string exportScope, string requestReference, string ipAddress, string userAgent, string? country = null)
    {
        var privacyEvent = PrivacyEvent.CreateDataExportRequest(
            tenantId, userId, exportFormat, exportScope, requestReference,
            ipAddress, userAgent, country);

        await _repository.AddAsync(privacyEvent);
        return privacyEvent;
    }

    public async Task<PrivacyEvent> LogRightToBeForgottenRequestAsync(Guid tenantId, Guid userId, string deletionScope, string requestReference, string? retentionRequirement, string ipAddress, string userAgent, string? country = null)
    {
        var privacyEvent = PrivacyEvent.CreateRightToBeForgottenRequest(
            tenantId, userId, deletionScope, requestReference, retentionRequirement,
            ipAddress, userAgent, country);

        await _repository.AddAsync(privacyEvent);
        return privacyEvent;
    }

    public async Task<PrivacyEvent> CreatePrivacyImpactAssessmentAsync(Guid tenantId, string piaReference, string dataCategories, string processingPurpose, string riskLevel, string? mitigationMeasures, string processedBy, string? approvedBy = null)
    {
        var privacyEvent = PrivacyEvent.CreatePrivacyImpactAssessment(
            tenantId, piaReference, dataCategories, processingPurpose, riskLevel,
            mitigationMeasures, processedBy, approvedBy);

        await _repository.AddAsync(privacyEvent);
        return privacyEvent;
    }

    public async Task<PrivacyEvent> CreateGdprArticle30RecordAsync(Guid tenantId, string processingActivity, string dataController, string? dataProcessor, string lawfulBasis, string dataCategories, string? dataRecipients, string? internationalTransfers, string retentionPeriod, string securityMeasures, string processedBy)
    {
        var privacyEvent = PrivacyEvent.CreateGdprArticle30Record(
            tenantId, processingActivity, dataController, dataProcessor, lawfulBasis,
            dataCategories, dataRecipients, internationalTransfers, retentionPeriod,
            securityMeasures, processedBy);

        await _repository.AddAsync(privacyEvent);
        return privacyEvent;
    }

    public async Task<PrivacyEvent> CompleteDataExportAsync(Guid eventId, string exportLocation, long exportSizeBytes, DateTime expiresAt)
    {
        var privacyEvent = await _repository.GetByIdAsync(eventId);
        if (privacyEvent == null)
            throw new InvalidOperationException($"Privacy event {eventId} not found");

        privacyEvent.CompleteDataExport(exportLocation, exportSizeBytes, expiresAt);
        await _repository.UpdateAsync(privacyEvent);
        return privacyEvent;
    }

    public async Task<PrivacyEvent> CompleteDeletionAsync(Guid eventId, string deletionMethod, bool isAnonymized)
    {
        var privacyEvent = await _repository.GetByIdAsync(eventId);
        if (privacyEvent == null)
            throw new InvalidOperationException($"Privacy event {eventId} not found");

        privacyEvent.CompleteDeletion(deletionMethod, isAnonymized);
        await _repository.UpdateAsync(privacyEvent);
        return privacyEvent;
    }

    public async Task<IEnumerable<PrivacyEvent>> GetPrivacyEventsByUserAsync(Guid userId)
    {
        return await _repository.GetByUserIdAsync(userId);
    }

    public async Task<IEnumerable<PrivacyEvent>> GetPrivacyEventsByTenantAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null)
    {
        return await _repository.GetByTenantIdAsync(tenantId, startDate, endDate);
    }

    public async Task<IEnumerable<PrivacyEvent>> GetPendingDataSubjectRequestsAsync(Guid tenantId)
    {
        return await _repository.GetPendingRequestsAsync(tenantId);
    }
}

/// <summary>
/// Repository interface for privacy event persistence.
/// </summary>
public interface IPrivacyEventRepository
{
    Task<PrivacyEvent> AddAsync(PrivacyEvent privacyEvent);
    Task<PrivacyEvent> UpdateAsync(PrivacyEvent privacyEvent);
    Task<PrivacyEvent?> GetByIdAsync(Guid id);
    Task<IEnumerable<PrivacyEvent>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<PrivacyEvent>> GetByTenantIdAsync(Guid tenantId, DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<PrivacyEvent>> GetPendingRequestsAsync(Guid tenantId);
}
