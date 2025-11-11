namespace GameGuild.Modules.Audit.Entities;

/// <summary>
/// Privacy and compliance event logging entity for GDPR, CCPA, and other privacy regulations.
/// Tracks consent changes, data export requests, right to be forgotten (RTBF), data portability, and privacy impact assessments.
/// </summary>
public sealed class PrivacyEvent : EntityBase {
  public Guid UserId { get; private set; }
  public PrivacyEventType EventType { get; private set; }
  public string EventAction { get; private set; } = string.Empty;
  public string? Description { get; private set; }
  public DateTime EventTimestamp { get; private set; }

  // Consent management
  public string? ConsentType { get; private set; }
  public string? ConsentVersion { get; private set; }
  public bool? ConsentGranted { get; private set; }
  public DateTime? ConsentExpiresAt { get; private set; }
  public string? ConsentPurpose { get; private set; }
  public string? LegalBasis { get; private set; }

  // Data subject rights
  public DataSubjectRight? DataSubjectRight { get; set; }
  public string? RequestType { get; set; }
  public RequestStatus? RequestStatus { get; set; }
  public DateTime? RequestedAt { get; set; }
  public DateTime? CompletedAt { get; set; }
  public string? RequestReference { get; set; }

  // Data export/portability
  public string? ExportFormat { get; set; }
  public string? ExportScope { get; set; }
  public long? ExportSizeBytes { get; set; }
  public string? ExportLocation { get; set; }
  public DateTime? ExportExpiresAt { get; set; }

  // Right to be forgotten (RTBF)
  public string? DeletionScope { get; set; }
  public bool? IsAnonymized { get; set; }
  public DateTime? AnonymizedAt { get; set; }
  public string? RetentionRequirement { get; set; }
  public string? DeletionMethod { get; set; }

  // Privacy Impact Assessment (PIA)
  public string? PiaReference { get; private set; }
  public string? DataCategories { get; private set; }
  public string? ProcessingPurpose { get; private set; }
  public string? RiskLevel { get; private set; }
  public string? MitigationMeasures { get; private set; }

  // GDPR Article 30 record keeping
  public string? ProcessingActivity { get; private set; }
  public string? DataController { get; private set; }
  public string? DataProcessor { get; private set; }
  public string? LawfulBasis { get; private set; }
  public string? DataRecipients { get; private set; }
  public string? InternationalTransfers { get; private set; }
  public string? RetentionPeriod { get; private set; }
  public string? SecurityMeasures { get; private set; }

  // Tracking and audit
  public string? ProcessedBy { get; private set; }
  public string? ApprovedBy { get; private set; }
  public DateTime? ApprovedAt { get; private set; }
  public string? Notes { get; private set; }
  public string IpAddress { get; private set; } = string.Empty;
  public string UserAgent { get; private set; } = string.Empty;
  public string? Country { get; private set; }

  // Compliance metadata
  public string[] ApplicableRegulations { get; private set; } = Array.Empty<string>();
  public bool RequiresNotification { get; private set; }
  public DateTime? NotificationSentAt { get; private set; }
  public string? NotificationMethod { get; private set; }

  private PrivacyEvent() { }

  public static PrivacyEvent CreateConsentChangeEvent(
      Guid tenantId,
      Guid userId,
      string consentType,
      bool consentGranted,
      string? consentPurpose,
      string? legalBasis,
      DateTime? expiresAt,
      string ipAddress,
      string userAgent,
      string? country = null) {
    return new PrivacyEvent {
      Id = Guid.NewGuid(),
      TenantId = tenantId,
      UserId = userId,
      EventType = PrivacyEventType.ConsentChange,
      EventAction = consentGranted ? "ConsentGranted" : "ConsentRevoked",
      Description = $"User {(consentGranted ? "granted" : "revoked")} consent for {consentType}",
      EventTimestamp = DateTime.UtcNow,
      ConsentType = consentType,
      ConsentGranted = consentGranted,
      ConsentPurpose = consentPurpose,
      LegalBasis = legalBasis,
      ConsentExpiresAt = expiresAt,
      IpAddress = ipAddress,
      UserAgent = userAgent,
      Country = country,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };
  }

  public static PrivacyEvent CreateDataExportRequest(
      Guid tenantId,
      Guid userId,
      string exportFormat,
      string exportScope,
      string requestReference,
      string ipAddress,
      string userAgent,
      string? country = null) {
    return new PrivacyEvent {
      Id = Guid.NewGuid(),
      TenantId = tenantId,
      UserId = userId,
      EventType = PrivacyEventType.DataExportRequest,
      EventAction = "DataExportRequested",
      Description = $"User requested data export in {exportFormat} format",
      EventTimestamp = DateTime.UtcNow,
      DataSubjectRight = DataSubjectRight.RightToDataPortability,
      RequestType = "DataExport",
      RequestStatus = RequestStatus.Pending,
      RequestedAt = DateTime.UtcNow,
      RequestReference = requestReference,
      ExportFormat = exportFormat,
      ExportScope = exportScope,
      IpAddress = ipAddress,
      UserAgent = userAgent,
      Country = country,
      ApplicableRegulations = new[] { "GDPR Article 20", "CCPA Section 1798.100" },
      RequiresNotification = false,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };
  }

  public static PrivacyEvent CreateRightToBeForgottenRequest(
      Guid tenantId,
      Guid userId,
      string deletionScope,
      string requestReference,
      string? retentionRequirement,
      string ipAddress,
      string userAgent,
      string? country = null) {
    return new PrivacyEvent {
      Id = Guid.NewGuid(),
      TenantId = tenantId,
      UserId = userId,
      EventType = PrivacyEventType.RightToBeForgotten,
      EventAction = "DeletionRequested",
      Description = "User requested right to be forgotten (RTBF)",
      EventTimestamp = DateTime.UtcNow,
      DataSubjectRight = DataSubjectRight.RightToErasure,
      RequestType = "Deletion",
      RequestStatus = RequestStatus.Pending,
      RequestedAt = DateTime.UtcNow,
      RequestReference = requestReference,
      DeletionScope = deletionScope,
      RetentionRequirement = retentionRequirement,
      IpAddress = ipAddress,
      UserAgent = userAgent,
      Country = country,
      ApplicableRegulations = new[] { "GDPR Article 17", "CCPA Section 1798.105" },
      RequiresNotification = true,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };
  }

  public static PrivacyEvent CreatePrivacyImpactAssessment(
      Guid tenantId,
      string piaReference,
      string dataCategories,
      string processingPurpose,
      string riskLevel,
      string? mitigationMeasures,
      string processedBy,
      string? approvedBy = null) {
    return new PrivacyEvent {
      Id = Guid.NewGuid(),
      TenantId = tenantId,
      UserId = Guid.Empty, // System-level event
      EventType = PrivacyEventType.PrivacyImpactAssessment,
      EventAction = "PIACreated",
      Description = "Privacy Impact Assessment created",
      EventTimestamp = DateTime.UtcNow,
      PiaReference = piaReference,
      DataCategories = dataCategories,
      ProcessingPurpose = processingPurpose,
      RiskLevel = riskLevel,
      MitigationMeasures = mitigationMeasures,
      ProcessedBy = processedBy,
      ApprovedBy = approvedBy,
      ApprovedAt = approvedBy != null ? DateTime.UtcNow : null,
      IpAddress = "System",
      UserAgent = "System",
      ApplicableRegulations = new[] { "GDPR Article 35" },
      RequiresNotification = false,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };
  }

  public static PrivacyEvent CreateGdprArticle30Record(
      Guid tenantId,
      string processingActivity,
      string dataController,
      string? dataProcessor,
      string lawfulBasis,
      string dataCategories,
      string? dataRecipients,
      string? internationalTransfers,
      string retentionPeriod,
      string securityMeasures,
      string processedBy) {
    return new PrivacyEvent {
      Id = Guid.NewGuid(),
      TenantId = tenantId,
      UserId = Guid.Empty, // System-level event
      EventType = PrivacyEventType.GdprArticle30Record,
      EventAction = "ProcessingRecordCreated",
      Description = "GDPR Article 30 processing record created",
      EventTimestamp = DateTime.UtcNow,
      ProcessingActivity = processingActivity,
      DataController = dataController,
      DataProcessor = dataProcessor,
      LawfulBasis = lawfulBasis,
      DataCategories = dataCategories,
      DataRecipients = dataRecipients,
      InternationalTransfers = internationalTransfers,
      RetentionPeriod = retentionPeriod,
      SecurityMeasures = securityMeasures,
      ProcessedBy = processedBy,
      IpAddress = "System",
      UserAgent = "System",
      ApplicableRegulations = new[] { "GDPR Article 30" },
      RequiresNotification = false,
      CreatedAt = DateTime.UtcNow,
      UpdatedAt = DateTime.UtcNow
    };
  }

  public void CompleteDataExport(string exportLocation, long exportSizeBytes, DateTime expiresAt) {
    RequestStatus = RequestStatus.Completed;
    CompletedAt = DateTime.UtcNow;
    ExportLocation = exportLocation;
    ExportSizeBytes = exportSizeBytes;
    ExportExpiresAt = expiresAt;
    UpdatedAt = DateTime.UtcNow;
  }

  public void CompleteDeletion(string deletionMethod, bool isAnonymized) {
    RequestStatus = RequestStatus.Completed;
    CompletedAt = DateTime.UtcNow;
    DeletionMethod = deletionMethod;
    IsAnonymized = isAnonymized;
    AnonymizedAt = isAnonymized ? DateTime.UtcNow : null;
    UpdatedAt = DateTime.UtcNow;
  }

  public void Approve(string approvedBy) {
    ApprovedBy = approvedBy;
    ApprovedAt = DateTime.UtcNow;
    UpdatedAt = DateTime.UtcNow;
  }

  public void SendNotification(string notificationMethod) {
    NotificationSentAt = DateTime.UtcNow;
    NotificationMethod = notificationMethod;
    UpdatedAt = DateTime.UtcNow;
  }
}

public enum PrivacyEventType {
  ConsentChange = 0,
  DataExportRequest = 1,
  RightToBeForgotten = 2,
  DataPortability = 3,
  DataAccess = 4,
  DataRectification = 5,
  DataRestriction = 6,
  PrivacyImpactAssessment = 7,
  GdprArticle30Record = 8,
  DataBreachNotification = 9,
  ConsentWithdrawal = 10,
  AutomatedDecisionOptOut = 11
}

public enum DataSubjectRight {
  RightToAccess = 0,
  RightToRectification = 1,
  RightToErasure = 2,
  RightToRestriction = 3,
  RightToDataPortability = 4,
  RightToObject = 5,
  RightToWithdrawConsent = 6,
  RightToAutomatedDecisionOptOut = 7
}

public enum RequestStatus {
  Pending = 0,
  InProgress = 1,
  Completed = 2,
  Rejected = 3,
  Cancelled = 4,
  Expired = 5
}
