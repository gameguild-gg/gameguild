using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddFieldLevelPrivacyAndAuditEvidenceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_entity_change_audit_tenant_time",
                schema: "gameguild.audit",
                table: "entity_change_audit_records");

            migrationBuilder.CreateTable(
                name: "BulkExportEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    RecordCount = table.Column<long>(type: "bigint", nullable: false),
                    Threshold = table.Column<long>(type: "bigint", nullable: false),
                    ThresholdExceeded = table.Column<bool>(type: "boolean", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Destination = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AlertRaised = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkExportEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrossBorderTransferLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DestinationJurisdiction = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    DataCategories = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RecordCount = table.Column<long>(type: "bigint", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrossBorderTransferLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustodyAccessLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Decision = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustodyAccessLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustodyEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    FromCustodianId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToCustodianId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TransferId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProofJson = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustodyEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustodyEvidenceItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceKind = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    SourceId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false),
                    ContentSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CollectedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CollectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CustodyState = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CurrentCustodianId = table.Column<Guid>(type: "uuid", nullable: false),
                    SealBrokenAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LegalHoldId = table.Column<Guid>(type: "uuid", nullable: true),
                    Isolated = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustodyEvidenceItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceCustodians",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ScopesJson = table.Column<string>(type: "text", nullable: false),
                    AuthorizedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorizedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceCustodians", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceTransfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromCustodianId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToCustodianId = table.Column<Guid>(type: "uuid", nullable: false),
                    InitiatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InitiatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChallengeNonce = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EvidenceHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProofJson = table.Column<string>(type: "text", nullable: false),
                    Signature = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    SigningKeyId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    State = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AcknowledgedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcknowledgedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcknowledgmentSignature = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Purpose = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceTransfers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FieldAccessAnomalies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnomalyType = table.Column<int>(type: "integer", nullable: false),
                    ObservedValue = table.Column<double>(type: "double precision", nullable: false),
                    BaselineValue = table.Column<double>(type: "double precision", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Detail = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    WindowStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WindowEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DetectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcknowledgedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldAccessAnomalies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FieldAccessAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AccessType = table.Column<int>(type: "integer", nullable: false),
                    AccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsSensitiveField = table.Column<bool>(type: "boolean", nullable: false),
                    OldValue = table.Column<string>(type: "text", nullable: true),
                    NewValue = table.Column<string>(type: "text", nullable: true),
                    MaskedOldValue = table.Column<string>(type: "text", nullable: true),
                    MaskedNewValue = table.Column<string>(type: "text", nullable: true),
                    SensitivityLevel = table.Column<int>(type: "integer", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RequestId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SessionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ApiEndpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LegalBasis = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ConsentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RequiresNotification = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationSent = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldAccessAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GdprProcessingActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActivityName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    LegalBasis = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CategoriesOfDataSubjects = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CategoriesOfPersonalData = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Recipients = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    InvolvesCrossBorderTransfers = table.Column<bool>(type: "boolean", nullable: false),
                    TransferDestinations = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RetentionPeriod = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SecurityMeasures = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsAutoDerived = table.Column<bool>(type: "boolean", nullable: false),
                    LastReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GdprProcessingActivities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegalHoldNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LegalHoldId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetKind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Target = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DispatchedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalHoldNotifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegalHolds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseNumber = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PreservationTargetsJson = table.Column<string>(type: "text", nullable: false),
                    NotificationTargetsJson = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReleasedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReleasedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReleaseReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalHolds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MutationValuePayloadChunks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    LengthBytes = table.Column<int>(type: "integer", nullable: false),
                    RefCount = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MutationValuePayloadChunks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MutationValueSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    KeyId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ChangeKind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BeforeIntegritySha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AfterIntegritySha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    BeforeStoredSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AfterStoredSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    BeforeBytes = table.Column<int>(type: "integer", nullable: false),
                    AfterBytes = table.Column<int>(type: "integer", nullable: false),
                    BeforePayload = table.Column<string>(type: "text", nullable: true),
                    AfterPayload = table.Column<string>(type: "text", nullable: true),
                    BeforePayloadTruncated = table.Column<bool>(type: "boolean", nullable: false),
                    AfterPayloadTruncated = table.Column<bool>(type: "boolean", nullable: false),
                    BeforeChunkId = table.Column<Guid>(type: "uuid", nullable: true),
                    AfterChunkId = table.Column<Guid>(type: "uuid", nullable: true),
                    StructuredDiffJson = table.Column<string>(type: "text", nullable: false),
                    DiffCount = table.Column<int>(type: "integer", nullable: false),
                    StorageMode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MutationValueSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SensitiveDataRetentionPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConfiguredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastEnforcedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastEnforcedCount = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensitiveDataRetentionPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SensitiveFieldClassifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    EntityType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FieldName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Sensitivity = table.Column<int>(type: "integer", nullable: false),
                    DetectionRule = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    IsConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    ObservationCount = table.Column<int>(type: "integer", nullable: false),
                    FirstSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensitiveFieldClassifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ValueSnapshotRetentionExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TriggeredByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DryRun = table.Column<bool>(type: "boolean", nullable: false),
                    LegalHoldActive = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedCount = table.Column<int>(type: "integer", nullable: false),
                    ArchivedCount = table.Column<int>(type: "integer", nullable: false),
                    CompactedCount = table.Column<int>(type: "integer", nullable: false),
                    BytesReclaimed = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValueSnapshotRetentionExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ValueSnapshotRetentionPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false),
                    ArchiveAfterDays = table.Column<int>(type: "integer", nullable: false),
                    EntityOverridesJson = table.Column<string>(type: "text", nullable: false),
                    LegalHoldUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfiguredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValueSnapshotRetentionPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ValueSnapshotRollbackPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetEntityName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TargetKeyId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    BasisSnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    AsOfUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BasisStateJson = table.Column<string>(type: "text", nullable: false),
                    BasisStateSha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PlanJson = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExecutedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExecutedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResultJson = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValueSnapshotRollbackPlans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BulkExportEvents_TenantId_OccurredAtUtc",
                table: "BulkExportEvents",
                columns: new[] { "TenantId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BulkExportEvents_TenantId_UserId_OccurredAtUtc",
                table: "BulkExportEvents",
                columns: new[] { "TenantId", "UserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BulkExportEvents_ThresholdExceeded",
                table: "BulkExportEvents",
                column: "ThresholdExceeded");

            migrationBuilder.CreateIndex(
                name: "IX_CrossBorderTransferLogs_Status",
                table: "CrossBorderTransferLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CrossBorderTransferLogs_TenantId_OccurredAtUtc",
                table: "CrossBorderTransferLogs",
                columns: new[] { "TenantId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustodyAccessLogs_TenantId_EvidenceId_OccurredAtUtc",
                table: "CustodyAccessLogs",
                columns: new[] { "TenantId", "EvidenceId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustodyEvents_TenantId_EvidenceId_SequenceNumber",
                table: "CustodyEvents",
                columns: new[] { "TenantId", "EvidenceId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustodyEvents_TransferId",
                table: "CustodyEvents",
                column: "TransferId");

            migrationBuilder.CreateIndex(
                name: "IX_CustodyEvidenceItems_TenantId_CollectedAtUtc",
                table: "CustodyEvidenceItems",
                columns: new[] { "TenantId", "CollectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CustodyEvidenceItems_TenantId_LegalHoldId",
                table: "CustodyEvidenceItems",
                columns: new[] { "TenantId", "LegalHoldId" });

            migrationBuilder.CreateIndex(
                name: "IX_CustodyEvidenceItems_TenantId_SourceKind_SourceId",
                table: "CustodyEvidenceItems",
                columns: new[] { "TenantId", "SourceKind", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceCustodians_TenantId_UserId",
                table: "EvidenceCustodians",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceTransfers_TenantId_EvidenceId_State",
                table: "EvidenceTransfers",
                columns: new[] { "TenantId", "EvidenceId", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldAccessAnomalies_TenantId_DetectedAtUtc",
                table: "FieldAccessAnomalies",
                columns: new[] { "TenantId", "DetectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldAccessAnomalies_TenantId_Status",
                table: "FieldAccessAnomalies",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldAccessAnomalies_UserId",
                table: "FieldAccessAnomalies",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldAccessAudits_EntityId",
                table: "FieldAccessAudits",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldAccessAudits_EntityType_FieldName",
                table: "FieldAccessAudits",
                columns: new[] { "EntityType", "FieldName" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldAccessAudits_TenantId_AccessedAt",
                table: "FieldAccessAudits",
                columns: new[] { "TenantId", "AccessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FieldAccessAudits_UserId",
                table: "FieldAccessAudits",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GdprProcessingActivities_TenantId_ActivityName",
                table: "GdprProcessingActivities",
                columns: new[] { "TenantId", "ActivityName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegalHoldNotifications_TenantId_LegalHoldId_Status",
                table: "LegalHoldNotifications",
                columns: new[] { "TenantId", "LegalHoldId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LegalHolds_TenantId_CaseNumber",
                table: "LegalHolds",
                columns: new[] { "TenantId", "CaseNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegalHolds_TenantId_Status",
                table: "LegalHolds",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MutationValuePayloadChunks_TenantId_ContentSha256",
                table: "MutationValuePayloadChunks",
                columns: new[] { "TenantId", "ContentSha256" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MutationValueSnapshots_AfterStoredSha256",
                table: "MutationValueSnapshots",
                column: "AfterStoredSha256");

            migrationBuilder.CreateIndex(
                name: "IX_MutationValueSnapshots_BeforeStoredSha256",
                table: "MutationValueSnapshots",
                column: "BeforeStoredSha256");

            migrationBuilder.CreateIndex(
                name: "IX_MutationValueSnapshots_CorrelationId",
                table: "MutationValueSnapshots",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_MutationValueSnapshots_TenantId_EntityName_KeyId_OccurredAt~",
                table: "MutationValueSnapshots",
                columns: new[] { "TenantId", "EntityName", "KeyId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MutationValueSnapshots_TenantId_OccurredAtUtc",
                table: "MutationValueSnapshots",
                columns: new[] { "TenantId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SensitiveDataRetentionPolicies_TenantId_Category",
                table: "SensitiveDataRetentionPolicies",
                columns: new[] { "TenantId", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SensitiveFieldClassifications_Category",
                table: "SensitiveFieldClassifications",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SensitiveFieldClassifications_TenantId_EntityType_FieldName",
                table: "SensitiveFieldClassifications",
                columns: new[] { "TenantId", "EntityType", "FieldName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValueSnapshotRetentionExecutions_TenantId_ExecutedAtUtc",
                table: "ValueSnapshotRetentionExecutions",
                columns: new[] { "TenantId", "ExecutedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ValueSnapshotRetentionPolicies_TenantId",
                table: "ValueSnapshotRetentionPolicies",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ValueSnapshotRollbackPlans_TenantId_Status",
                table: "ValueSnapshotRollbackPlans",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ValueSnapshotRollbackPlans_TenantId_TargetEntityName_Target~",
                table: "ValueSnapshotRollbackPlans",
                columns: new[] { "TenantId", "TargetEntityName", "TargetKeyId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BulkExportEvents");

            migrationBuilder.DropTable(
                name: "CrossBorderTransferLogs");

            migrationBuilder.DropTable(
                name: "CustodyAccessLogs");

            migrationBuilder.DropTable(
                name: "CustodyEvents");

            migrationBuilder.DropTable(
                name: "CustodyEvidenceItems");

            migrationBuilder.DropTable(
                name: "EvidenceCustodians");

            migrationBuilder.DropTable(
                name: "EvidenceTransfers");

            migrationBuilder.DropTable(
                name: "FieldAccessAnomalies");

            migrationBuilder.DropTable(
                name: "FieldAccessAudits");

            migrationBuilder.DropTable(
                name: "GdprProcessingActivities");

            migrationBuilder.DropTable(
                name: "LegalHoldNotifications");

            migrationBuilder.DropTable(
                name: "LegalHolds");

            migrationBuilder.DropTable(
                name: "MutationValuePayloadChunks");

            migrationBuilder.DropTable(
                name: "MutationValueSnapshots");

            migrationBuilder.DropTable(
                name: "SensitiveDataRetentionPolicies");

            migrationBuilder.DropTable(
                name: "SensitiveFieldClassifications");

            migrationBuilder.DropTable(
                name: "ValueSnapshotRetentionExecutions");

            migrationBuilder.DropTable(
                name: "ValueSnapshotRetentionPolicies");

            migrationBuilder.DropTable(
                name: "ValueSnapshotRollbackPlans");

            migrationBuilder.CreateIndex(
                name: "ix_entity_change_audit_tenant_time",
                schema: "gameguild.audit",
                table: "entity_change_audit_records",
                columns: new[] { "TenantId", "OccurredAtUtc" });
        }
    }
}
