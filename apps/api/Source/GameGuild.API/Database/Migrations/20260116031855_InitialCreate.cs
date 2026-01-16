using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "gameguild.authentication");

            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.CreateTable(
                name: "AbacPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Effect = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SubjectConditions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ResourceConditions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    EnvironmentConditions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ActionConditions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TargetResources = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TargetActions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AttributeExpression = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ConditionExpression = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TimeConditions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LocationConditions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Obligations = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EffectiveUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Tags = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbacPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccessControlListEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrincipalType = table.Column<int>(type: "integer", nullable: false),
                    PrincipalId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResourceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AccessLevel = table.Column<int>(type: "integer", nullable: false),
                    IsDenied = table.Column<bool>(type: "boolean", nullable: false),
                    GrantedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessControlListEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccessReviewCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ReviewType = table.Column<int>(type: "integer", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    ScopeFilter = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalItems = table.Column<int>(type: "integer", nullable: false),
                    ReviewedItems = table.Column<int>(type: "integer", nullable: false),
                    ApprovedItems = table.Column<int>(type: "integer", nullable: false),
                    RevokedItems = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AutoRevokeOnNoResponse = table.Column<bool>(type: "boolean", nullable: false),
                    ReminderFrequencyDays = table.Column<int>(type: "integer", nullable: false),
                    NotificationTemplate = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessReviewCampaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "authenticationattempt",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsSuccessful = table.Column<bool>(type: "boolean", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AttemptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessingTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DeviceFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsSuspicious = table.Column<bool>(type: "boolean", nullable: false),
                    RiskScore = table.Column<int>(type: "integer", nullable: false),
                    Metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_authenticationattempt", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "blockchaincertificateanchor",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CertificateType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CertificateHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CertificateData = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    TransactionHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    BlockchainNetwork = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BlockNumber = table.Column<long>(type: "bigint", nullable: true),
                    AnchoredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RevocationTransactionHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blockchaincertificateanchor", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ConditionalPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ConditionType = table.Column<int>(type: "integer", nullable: false),
                    PermissionType = table.Column<string>(type: "text", nullable: true),
                    ResourceType = table.Column<string>(type: "text", nullable: true),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    TimeConditions = table.Column<string>(type: "text", nullable: true),
                    EnvironmentConditions = table.Column<string>(type: "text", nullable: true),
                    LocationConditions = table.Column<string>(type: "text", nullable: true),
                    DeviceConditions = table.Column<string>(type: "text", nullable: true),
                    CustomConditions = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConditionalPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "contenttypepermission",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentTypeName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Permissions = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    GrantedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contenttypepermission", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "DataMaskingRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ResourceType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FieldName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MaskingType = table.Column<int>(type: "integer", nullable: false),
                    MaskingPattern = table.Column<string>(type: "text", nullable: true),
                    ShowFirst = table.Column<int>(type: "integer", nullable: true),
                    ShowLast = table.Column<int>(type: "integer", nullable: true),
                    MaskCharacter = table.Column<char>(type: "character(1)", nullable: false),
                    ExemptRoles = table.Column<string>(type: "text", nullable: true),
                    RequiredPermissions = table.Column<string>(type: "text", nullable: true),
                    ExemptUsers = table.Column<string>(type: "text", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataMaskingRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DelegatedAdminScopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdminUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ScopeType = table.Column<int>(type: "integer", nullable: false),
                    AllowedResourceTypes = table.Column<string>(type: "text", nullable: true),
                    AllowedResourceIds = table.Column<string>(type: "text", nullable: true),
                    AllowedUserIds = table.Column<string>(type: "text", nullable: true),
                    AllowedDepartments = table.Column<string>(type: "text", nullable: true),
                    AllowedTeams = table.Column<string>(type: "text", nullable: true),
                    AllowedRoles = table.Column<string>(type: "text", nullable: true),
                    GrantablePermissions = table.Column<string>(type: "text", nullable: true),
                    DeniedPermissions = table.Column<string>(type: "text", nullable: true),
                    CanManageUsers = table.Column<bool>(type: "boolean", nullable: false),
                    CanManagePermissions = table.Column<bool>(type: "boolean", nullable: false),
                    CanManageResources = table.Column<bool>(type: "boolean", nullable: false),
                    CanViewAuditLogs = table.Column<bool>(type: "boolean", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DelegatedAdminScopes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DynamicRole",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParentRoleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Permissions = table.Column<string[]>(type: "text[]", nullable: false),
                    DenyPermissions = table.Column<string[]>(type: "text[]", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    MutuallyExclusiveRoleIds = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    PrerequisiteRoleIds = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    MaxAssignments = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DynamicRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DynamicRole_DynamicRole_ParentRoleId",
                        column: x => x.ParentRoleId,
                        principalTable: "DynamicRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "identityverification",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VerifiedValue = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    InitiatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerificationProvider = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ExternalVerificationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConfidenceScore = table.Column<double>(type: "double precision", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DocumentIds = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identityverification", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "JitElevationRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Permission = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Justification = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewerComments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JitElevationRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "mfaattempt",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    IsSuccessful = table.Column<bool>(type: "boolean", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AttemptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessingTimeMs = table.Column<int>(type: "integer", nullable: false),
                    DeviceFingerprint = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mfaattempt", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "PermissionDelegations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DelegatorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DelegateUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    DelegatedPermissions = table.Column<string[]>(type: "text[]", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CanSubDelegate = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Conditions = table.Column<string>(type: "text", nullable: true),
                    UsageLimit = table.Column<int>(type: "integer", nullable: true),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionDelegations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PermissionTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Permissions = table.Column<string[]>(type: "text[]", nullable: false),
                    IsSystemTemplate = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MinimumTier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PolicyDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequireAuthentication = table.Column<bool>(type: "boolean", nullable: false),
                    AuthenticationSchemesJson = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    RequiredPermissionsJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RequiredRolesJson = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    RequireAccessControlListAccess = table.Column<bool>(type: "boolean", nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MinimumAccessLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsTenantScoped = table.Column<bool>(type: "boolean", nullable: false),
                    PolicyVersion = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RulesJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    UseRuleBasedEvaluation = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "promo_stacking_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    MaxStackableCount = table.Column<int>(type: "integer", nullable: false),
                    AllowExclusiveStacking = table.Column<bool>(type: "boolean", nullable: false),
                    MaxTotalDiscountPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    MaxTotalDiscountAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    AllowedTypesCombinations = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConflictStrategy = table.Column<int>(type: "integer", nullable: false),
                    AllowSameTypeStacking = table.Column<bool>(type: "boolean", nullable: false),
                    MinOrderAmountForStacking = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promo_stacking_rules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "refreshtoken",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedByIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedByIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refreshtoken", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ResourceInvitation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ResourceId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Permissions = table.Column<string[]>(type: "text[]", maxLength: 4000, nullable: false),
                    InvitedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedByUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    InvitedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcceptedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeclinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeclineReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceInvitation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResourceUserPermission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ResourceId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Permissions = table.Column<string[]>(type: "text[]", maxLength: 4000, nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GrantedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedByUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevokedByUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RevocationReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsOwner = table.Column<bool>(type: "boolean", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceUserPermission", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "role",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    permissions = table.Column<string>(type: "jsonb", maxLength: 4000, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "service_accounts",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    client_secret_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scopes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    secret_rotated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    secret_rotation_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_authenticated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_authenticated_from_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    authentication_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    failed_authentication_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_locked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    locked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    allowed_ip_addresses = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "SoDRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RuleType = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ConflictingPermissions = table.Column<string>(type: "text", nullable: false),
                    ConflictingRoles = table.Column<string>(type: "text", nullable: true),
                    ConflictingResources = table.Column<string>(type: "text", nullable: true),
                    AllowedExceptions = table.Column<string>(type: "text", nullable: true),
                    RequireApproval = table.Column<bool>(type: "boolean", nullable: false),
                    ApproverRoles = table.Column<string>(type: "text", nullable: true),
                    MitigationStrategy = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ViolationCount = table.Column<int>(type: "integer", nullable: false),
                    LastViolationDetected = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoDRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    HasPrioritySupport = table.Column<bool>(type: "boolean", nullable: false),
                    HasAdvancedAnalytics = table.Column<bool>(type: "boolean", nullable: false),
                    HasCustomBranding = table.Column<bool>(type: "boolean", nullable: false),
                    Features = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Metadata = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    TrialPeriodDays = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MonthlyPriceInCents = table.Column<long>(type: "bigint", nullable: false),
                    AnnualPriceInCents = table.Column<long>(type: "bigint", nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MaxUsers = table.Column<int>(type: "integer", nullable: true),
                    MaxStorageMb = table.Column<long>(type: "bigint", nullable: true),
                    MaxApiCallsPerMonth = table.Column<long>(type: "bigint", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Permissions = table.Column<string[]>(type: "text[]", nullable: false),
                    DenyPermissions = table.Column<string[]>(type: "text[]", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    GrantedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantPermissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    AdminEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantSecurityVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SecurityVersion = table.Column<long>(type: "bigint", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastChangeReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSecurityVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "trusteddevice",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DeviceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DeviceInfo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    TrustedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AssociatedIpAddresses = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_trusteddevice", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_mfa_configuration",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    totp_secret_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    backup_codes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    enabled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_attempts = table.Column<int>(type: "integer", nullable: false),
                    locked_out_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    preferred_method = table.Column<string>(type: "text", nullable: false),
                    qr_code_setup_data = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_setup_complete = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_mfa_configuration", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Username = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsEmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsSuspended = table.Column<bool>(type: "boolean", nullable: false),
                    TokenVersion = table.Column<int>(type: "integer", nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "usersession",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RefreshToken = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    AccessTokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DeviceFingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DeviceInfo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TerminationReason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TerminatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsTrustedDevice = table.Column<bool>(type: "boolean", nullable: false),
                    TrustedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usersession", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "UserWebAuthnCredentials",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CredentialId = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    PublicKey = table.Column<string>(type: "text", nullable: false),
                    AaGuid = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    SignatureCounter = table.Column<long>(type: "bigint", nullable: false),
                    FriendlyName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CredentialType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "public-key"),
                    AuthenticatorType = table.Column<int>(type: "integer", nullable: false),
                    Transports = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsPasswordless = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    UserVerified = table.Column<bool>(type: "boolean", nullable: false),
                    BackedUp = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RegisteredFromIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    RegisteredUserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserWebAuthnCredentials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccessReviewItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResourceType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PermissionDetails = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Decision = table.Column<int>(type: "integer", nullable: true),
                    DecisionReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastReminderSent = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReminderCount = table.Column<int>(type: "integer", nullable: false),
                    ReviewerNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessReviewItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccessReviewItems_AccessReviewCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "AccessReviewCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DynamicRoleAssignment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    GrantedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DynamicRoleAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DynamicRoleAssignment_DynamicRole_RoleId",
                        column: x => x.RoleId,
                        principalTable: "DynamicRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_role",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_by = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_role", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_role_role_role_id",
                        column: x => x.role_id,
                        principalSchema: "gameguild.authentication",
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoDViolations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ViolationDetails = table.Column<string>(type: "text", nullable: false),
                    ConflictingItems = table.Column<string>(type: "text", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DetectedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ResolutionAction = table.Column<int>(type: "integer", nullable: true),
                    IsException = table.Column<bool>(type: "boolean", nullable: false),
                    ExceptionJustification = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoDViolations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoDViolations_SoDRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "SoDRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FulfilledOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifyingOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastRenewalIdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastPaymentIdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LockedPriceVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastProcessedBillingCycle = table.Column<int>(type: "integer", nullable: false),
                    TrialEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<int>(type: "integer", nullable: true),
                    CancellationNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExternalCustomerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AutoRenew = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentPeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentPeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BillingCycleCount = table.Column<int>(type: "integer", nullable: false),
                    LastPaymentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingCycle = table.Column<int>(type: "integer", nullable: false),
                    Amount_Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Amount_Currency = table.Column<string>(type: "text", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextBillingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantDomains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TopLevelDomain = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Subdomain = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsMainDomain = table.Column<bool>(type: "boolean", nullable: false),
                    IsSecondaryDomain = table.Column<bool>(type: "boolean", nullable: false),
                    UserGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantDomains", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantDomains_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomFields = table.Column<string>(type: "jsonb", maxLength: 10000, nullable: false),
                    Tags = table.Column<string>(type: "jsonb", maxLength: 5000, nullable: false),
                    ExternalReferences = table.Column<string>(type: "jsonb", maxLength: 8000, nullable: false),
                    BusinessInfo = table.Column<string>(type: "jsonb", maxLength: 8000, nullable: false),
                    ContactInfo = table.Column<string>(type: "jsonb", maxLength: 8000, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Size = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantMetadata_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefaultLanguage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DefaultTimezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DefaultCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    AllowUserRegistration = table.Column<bool>(type: "boolean", nullable: false),
                    RequireRegistrationApproval = table.Column<bool>(type: "boolean", nullable: false),
                    RequireTwoFactorAuth = table.Column<bool>(type: "boolean", nullable: false),
                    MaxUsers = table.Column<int>(type: "integer", nullable: true),
                    StorageQuota = table.Column<long>(type: "bigint", nullable: true),
                    EnableAuditLogging = table.Column<bool>(type: "boolean", nullable: false),
                    EnableApiAccess = table.Column<bool>(type: "boolean", nullable: false),
                    BrandingSettings = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    NotificationSettings = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    SecuritySettings = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantSettings_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantStatistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    StatisticDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalMembers = table.Column<int>(type: "integer", nullable: false),
                    ActiveMembers = table.Column<int>(type: "integer", nullable: false),
                    InactiveMembers = table.Column<int>(type: "integer", nullable: false),
                    StorageUsed = table.Column<long>(type: "bigint", nullable: false),
                    ApiCalls = table.Column<int>(type: "integer", nullable: false),
                    NewMembers = table.Column<int>(type: "integer", nullable: false),
                    MembersLeft = table.Column<int>(type: "integer", nullable: false),
                    CustomMetrics = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantStatistics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantStatistics_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsageTracking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UsageAmount = table.Column<long>(type: "bigint", nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Metadata = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageTracking", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsageTracking_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    DiscountTotal = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    PaymentProviderReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExternalPaymentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefundAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    RefundReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    OrderType = table.Column<int>(type: "integer", nullable: false),
                    TargetSubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    FulfilledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_orders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ShortDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsBundle = table.Column<bool>(type: "boolean", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Users_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TenantMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentMemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    Role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LeaveReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantMembers_TenantMembers_ParentMemberId",
                        column: x => x.ParentMemberId,
                        principalTable: "TenantMembers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TenantMembers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TenantMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomFields = table.Column<string>(type: "jsonb", maxLength: 50000, nullable: false),
                    Tags = table.Column<string>(type: "jsonb", maxLength: 10000, nullable: false),
                    ExternalReferences = table.Column<string>(type: "jsonb", maxLength: 25000, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMetadata_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ArchivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ActionUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", maxLength: 10000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GeneralPreferences = table.Column<string>(type: "jsonb", maxLength: 10000, nullable: false),
                    NotificationPreferences = table.Column<string>(type: "jsonb", maxLength: 10000, nullable: false),
                    AccessibilityPreferences = table.Column<string>(type: "jsonb", maxLength: 10000, nullable: false),
                    PrivacyPreferences = table.Column<string>(type: "jsonb", maxLength: 10000, nullable: false),
                    LocalizationPreferences = table.Column<string>(type: "jsonb", maxLength: 10000, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Bio = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Website = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    JobTitle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Company = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BannerUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousStatus = table.Column<int>(type: "integer", nullable: false),
                    NewStatus = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ExternalPaymentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InitiatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    AdditionalContext = table.Column<string>(type: "jsonb", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_audit_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_audit_logs_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pricing_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RuleType = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MinQuantity = table.Column<int>(type: "integer", nullable: true),
                    MaxQuantity = table.Column<int>(type: "integer", nullable: true),
                    DiscountPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    FixedPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    TimeStart = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    TimeEnd = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    DaysOfWeek = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CustomerSegment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pricing_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pricing_rules_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pricing_tiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MinQuantity = table.Column<int>(type: "integer", nullable: false),
                    MaxQuantity = table.Column<int>(type: "integer", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pricing_tiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pricing_tiers_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_bundle_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BundleProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    IncludedProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    BundleDiscountPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_bundle_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_bundle_items_Products_BundleProductId",
                        column: x => x.BundleProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_bundle_items_Products_IncludedProductId",
                        column: x => x.IncludedProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_commission_configs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ReferralCommissionPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    AffiliateCommissionPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    MaxAffiliateDiscount = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    MinimumOrderValue = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    CookieDurationDays = table.Column<int>(type: "integer", nullable: false),
                    CommissionOnRecurring = table.Column<bool>(type: "boolean", nullable: false),
                    MaxRecurringPayments = table.Column<int>(type: "integer", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_commission_configs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_commission_configs_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BasePrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    SalePrice = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    SaleStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SaleEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentVersion = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_pricing", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_pricing_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_subscription_plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    BillingInterval = table.Column<int>(type: "integer", nullable: false),
                    IntervalCount = table.Column<int>(type: "integer", nullable: false),
                    TrialPeriodDays = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_subscription_plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_subscription_plans_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "promo_codes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    MinimumOrderAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    MaxUses = table.Column<int>(type: "integer", nullable: true),
                    MaxUsesPerUser = table.Column<int>(type: "integer", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsExclusive = table.Column<bool>(type: "boolean", nullable: false),
                    StackingPriority = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promo_codes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_promo_codes_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_promo_codes_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcquisitionType = table.Column<int>(type: "integer", nullable: false),
                    AccessStatus = table.Column<int>(type: "integer", nullable: false),
                    PricePaid = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    AccessStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AccessEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GiftedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubscriptionStatus = table.Column<int>(type: "integer", nullable: true),
                    SubscriptionProviderReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CurrentPeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CurrentPeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelAtPeriodEnd = table.Column<bool>(type: "boolean", nullable: false),
                    RevocationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_products_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_products_Users_GiftedByUserId",
                        column: x => x.GiftedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_user_products_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_pricing_versions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductPricingId = table.Column<Guid>(type: "uuid", nullable: false),
                    price_version = table.Column<int>(type: "integer", nullable: false),
                    BasePrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    SalePrice = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ChangeReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_pricing_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_pricing_versions_product_pricing_ProductPricingId",
                        column: x => x.ProductPricingId,
                        principalTable: "product_pricing",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "promo_code_uses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromoCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscountApplied = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promo_code_uses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_promo_code_uses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_promo_code_uses_promo_codes_PromoCodeId",
                        column: x => x.PromoCodeId,
                        principalTable: "promo_codes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_line_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UnitPriceSnapshot = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    BasePriceSnapshot = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    SalePriceSnapshot = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    PromoCodesApplied = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LineTotal = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    PricingTierId = table.Column<Guid>(type: "uuid", nullable: true),
                    PricingTierNameSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsSubscription = table.Column<bool>(type: "boolean", nullable: false),
                    SubscriptionPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    BillingIntervalSnapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    UserProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_line_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_line_items_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_line_items_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_line_items_user_products_UserProductId",
                        column: x => x.UserProductId,
                        principalTable: "user_products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AbacPolicies_IsEnabled",
                table: "AbacPolicies",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_AbacPolicies_Priority",
                table: "AbacPolicies",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_AbacPolicies_TenantId",
                table: "AbacPolicies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessControlListEntries_ResourceType_ResourceId",
                table: "AccessControlListEntries",
                columns: new[] { "ResourceType", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessControlListEntries_TenantId_PrincipalType_PrincipalId",
                table: "AccessControlListEntries",
                columns: new[] { "TenantId", "PrincipalType", "PrincipalId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessControlListEntries_TenantId_PrincipalType_PrincipalId~",
                table: "AccessControlListEntries",
                columns: new[] { "TenantId", "PrincipalType", "PrincipalId", "ResourceType", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessControlListEntries_TenantId_ResourceType_ResourceId",
                table: "AccessControlListEntries",
                columns: new[] { "TenantId", "ResourceType", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessControlListEntries_TenantId_ResourceType_ResourceId_I~",
                table: "AccessControlListEntries",
                columns: new[] { "TenantId", "ResourceType", "ResourceId", "IsDenied" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessControlListEntries_TenantId_ResourceType_ResourceId_P~",
                table: "AccessControlListEntries",
                columns: new[] { "TenantId", "ResourceType", "ResourceId", "PrincipalType", "PrincipalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccessReviewCampaigns_StartDate_EndDate",
                table: "AccessReviewCampaigns",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessReviewCampaigns_Status",
                table: "AccessReviewCampaigns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AccessReviewCampaigns_TenantId",
                table: "AccessReviewCampaigns",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessReviewItems_CampaignId",
                table: "AccessReviewItems",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessReviewItems_Decision",
                table: "AccessReviewItems",
                column: "Decision");

            migrationBuilder.CreateIndex(
                name: "IX_AccessReviewItems_ReviewerId",
                table: "AccessReviewItems",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessReviewItems_SubjectUserId",
                table: "AccessReviewItems",
                column: "SubjectUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConditionalPolicies_IsEnabled",
                table: "ConditionalPolicies",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_ConditionalPolicies_Priority",
                table: "ConditionalPolicies",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_ConditionalPolicies_TenantId",
                table: "ConditionalPolicies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DataMaskingRules_IsEnabled",
                table: "DataMaskingRules",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_DataMaskingRules_ResourceType",
                table: "DataMaskingRules",
                column: "ResourceType");

            migrationBuilder.CreateIndex(
                name: "IX_DataMaskingRules_TenantId",
                table: "DataMaskingRules",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DelegatedAdminScopes_AdminUserId",
                table: "DelegatedAdminScopes",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DelegatedAdminScopes_IsActive",
                table: "DelegatedAdminScopes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DelegatedAdminScopes_StartsAt_ExpiresAt",
                table: "DelegatedAdminScopes",
                columns: new[] { "StartsAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DelegatedAdminScopes_TenantId",
                table: "DelegatedAdminScopes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicRole_IsActive",
                table: "DynamicRole",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicRole_Name",
                table: "DynamicRole",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicRole_ParentRoleId",
                table: "DynamicRole",
                column: "ParentRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicRole_TenantId",
                table: "DynamicRole",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicRole_TenantId_Name",
                table: "DynamicRole",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DynamicRoleAssignment_IsActive",
                table: "DynamicRoleAssignment",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicRoleAssignment_RoleId",
                table: "DynamicRoleAssignment",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicRoleAssignment_StartsAt_ExpiresAt",
                table: "DynamicRoleAssignment",
                columns: new[] { "StartsAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DynamicRoleAssignment_TenantId",
                table: "DynamicRoleAssignment",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicRoleAssignment_UserId",
                table: "DynamicRoleAssignment",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicRoleAssignment_UserId_RoleId_TenantId",
                table: "DynamicRoleAssignment",
                columns: new[] { "UserId", "RoleId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JitElevationRequests_ExpiresAt",
                table: "JitElevationRequests",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_JitElevationRequests_RequesterId",
                table: "JitElevationRequests",
                column: "RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_JitElevationRequests_Status",
                table: "JitElevationRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_JitElevationRequests_TenantId",
                table: "JitElevationRequests",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_order_audit_logs_NewStatus",
                table: "order_audit_logs",
                column: "NewStatus");

            migrationBuilder.CreateIndex(
                name: "IX_order_audit_logs_OccurredAt",
                table: "order_audit_logs",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_order_audit_logs_OrderId",
                table: "order_audit_logs",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_order_audit_logs_TenantId",
                table: "order_audit_logs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_order_line_items_OrderId",
                table: "order_line_items",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_order_line_items_ProductId",
                table: "order_line_items",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_order_line_items_UserProductId",
                table: "order_line_items",
                column: "UserProductId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_CreatedAt",
                table: "orders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_orders_IdempotencyKey",
                table: "orders",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_Status",
                table: "orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_orders_TenantId",
                table: "orders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_UserId",
                table: "orders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionDelegations_DelegateUserId",
                table: "PermissionDelegations",
                column: "DelegateUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionDelegations_DelegatorUserId",
                table: "PermissionDelegations",
                column: "DelegatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionDelegations_ExpiresAt",
                table: "PermissionDelegations",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionDelegations_IsActive",
                table: "PermissionDelegations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionDelegations_TenantId",
                table: "PermissionDelegations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionTemplates_IsSystemTemplate",
                table: "PermissionTemplates",
                column: "IsSystemTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionTemplates_Name",
                table: "PermissionTemplates",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PolicyDefinitions_PolicyName_TenantId",
                table: "PolicyDefinitions",
                columns: new[] { "PolicyName", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pricing_rules_EndDate",
                table: "pricing_rules",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_pricing_rules_IsActive",
                table: "pricing_rules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_pricing_rules_Priority",
                table: "pricing_rules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_pricing_rules_ProductId",
                table: "pricing_rules",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_pricing_rules_RuleType",
                table: "pricing_rules",
                column: "RuleType");

            migrationBuilder.CreateIndex(
                name: "IX_pricing_rules_StartDate",
                table: "pricing_rules",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_pricing_tiers_IsActive",
                table: "pricing_tiers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_pricing_tiers_MinQuantity",
                table: "pricing_tiers",
                column: "MinQuantity");

            migrationBuilder.CreateIndex(
                name: "IX_pricing_tiers_ProductId",
                table: "pricing_tiers",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_product_bundle_items_BundleProductId",
                table: "product_bundle_items",
                column: "BundleProductId");

            migrationBuilder.CreateIndex(
                name: "IX_product_bundle_items_BundleProductId_IncludedProductId",
                table: "product_bundle_items",
                columns: new[] { "BundleProductId", "IncludedProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_bundle_items_IncludedProductId",
                table: "product_bundle_items",
                column: "IncludedProductId");

            migrationBuilder.CreateIndex(
                name: "IX_product_commission_configs_IsActive",
                table: "product_commission_configs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_product_commission_configs_ProductId",
                table: "product_commission_configs",
                column: "ProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_pricing_Currency",
                table: "product_pricing",
                column: "Currency");

            migrationBuilder.CreateIndex(
                name: "IX_product_pricing_IsDefault",
                table: "product_pricing",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_product_pricing_ProductId",
                table: "product_pricing",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_product_pricing_SaleEndDate",
                table: "product_pricing",
                column: "SaleEndDate");

            migrationBuilder.CreateIndex(
                name: "IX_product_pricing_SaleStartDate",
                table: "product_pricing",
                column: "SaleStartDate");

            migrationBuilder.CreateIndex(
                name: "IX_product_pricing_versions_IsActive",
                table: "product_pricing_versions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_product_pricing_versions_ProductPricingId_EffectiveFrom",
                table: "product_pricing_versions",
                columns: new[] { "ProductPricingId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_product_pricing_versions_ProductPricingId_price_version",
                table: "product_pricing_versions",
                columns: new[] { "ProductPricingId", "price_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_subscription_plans_BillingInterval",
                table: "product_subscription_plans",
                column: "BillingInterval");

            migrationBuilder.CreateIndex(
                name: "IX_product_subscription_plans_IsActive",
                table: "product_subscription_plans",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_product_subscription_plans_IsDefault",
                table: "product_subscription_plans",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_product_subscription_plans_Name",
                table: "product_subscription_plans",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_product_subscription_plans_Price",
                table: "product_subscription_plans",
                column: "Price");

            migrationBuilder.CreateIndex(
                name: "IX_product_subscription_plans_ProductId",
                table: "product_subscription_plans",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CreatorId",
                table: "Products",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Type",
                table: "Products",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_promo_code_uses_PromoCodeId",
                table: "promo_code_uses",
                column: "PromoCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_promo_code_uses_UserId",
                table: "promo_code_uses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_promo_codes_Code",
                table: "promo_codes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_promo_codes_CreatedBy",
                table: "promo_codes",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_promo_codes_IsActive",
                table: "promo_codes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_promo_codes_ProductId",
                table: "promo_codes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_promo_codes_Type",
                table: "promo_codes",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_promo_codes_ValidFrom",
                table: "promo_codes",
                column: "ValidFrom");

            migrationBuilder.CreateIndex(
                name: "IX_promo_codes_ValidUntil",
                table: "promo_codes",
                column: "ValidUntil");

            migrationBuilder.CreateIndex(
                name: "IX_promo_stacking_rules_IsActive",
                table: "promo_stacking_rules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_promo_stacking_rules_Name",
                table: "promo_stacking_rules",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_promo_stacking_rules_Priority",
                table: "promo_stacking_rules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceInvitation_ExpiresAt",
                table: "ResourceInvitation",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceInvitation_Status",
                table: "ResourceInvitation",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceInvitation_TenantId_Email",
                table: "ResourceInvitation",
                columns: new[] { "TenantId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceInvitation_TenantId_ResourceType_ResourceId",
                table: "ResourceInvitation",
                columns: new[] { "TenantId", "ResourceType", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceUserPermission_ExpiresAt",
                table: "ResourceUserPermission",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceUserPermission_TenantId_ResourceType_ResourceId",
                table: "ResourceUserPermission",
                columns: new[] { "TenantId", "ResourceType", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceUserPermission_TenantId_UserId",
                table: "ResourceUserPermission",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceUserPermission_TenantId_UserId_ResourceType_Resourc~",
                table: "ResourceUserPermission",
                columns: new[] { "TenantId", "UserId", "ResourceType", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "idx_role_is_active",
                schema: "gameguild.authentication",
                table: "role",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "idx_role_name",
                schema: "gameguild.authentication",
                table: "role",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "idx_role_name_tenant_id",
                schema: "gameguild.authentication",
                table: "role",
                columns: new[] { "name", "tenant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_role_tenant_id",
                schema: "gameguild.authentication",
                table: "role",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "idx_service_accounts_client_id",
                schema: "gameguild.authentication",
                table: "service_accounts",
                column: "client_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_service_accounts_tenant_active",
                schema: "gameguild.authentication",
                table: "service_accounts",
                columns: new[] { "tenant_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "idx_service_accounts_tenant_id",
                schema: "gameguild.authentication",
                table: "service_accounts",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_SoDRules_IsEnabled",
                table: "SoDRules",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_SoDRules_TenantId",
                table: "SoDRules",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SoDViolations_RuleId",
                table: "SoDViolations",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_SoDViolations_Status",
                table: "SoDViolations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SoDViolations_TenantId",
                table: "SoDViolations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SoDViolations_UserId",
                table: "SoDViolations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_ExternalId",
                table: "SubscriptionPlans",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Name",
                table: "SubscriptionPlans",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_CancelledAt",
                table: "Subscriptions",
                column: "CancelledAt");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_ExternalCustomerId",
                table: "Subscriptions",
                column: "ExternalCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_ExternalId",
                table: "Subscriptions",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_LastPaymentAt",
                table: "Subscriptions",
                column: "LastPaymentAt");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_NextBillingDate",
                table: "Subscriptions",
                column: "NextBillingDate");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PlanId",
                table: "Subscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_Status",
                table: "Subscriptions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TenantId_Status",
                table: "Subscriptions",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TrialEndDate",
                table: "Subscriptions",
                column: "TrialEndDate");

            migrationBuilder.CreateIndex(
                name: "IX_TenantDomains_TenantId_IsMainDomain",
                table: "TenantDomains",
                columns: new[] { "TenantId", "IsMainDomain" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantDomains_TopLevelDomain_Subdomain",
                table: "TenantDomains",
                columns: new[] { "TopLevelDomain", "Subdomain" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantMembers_JoinedAt",
                table: "TenantMembers",
                column: "JoinedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenantMembers_ParentMemberId",
                table: "TenantMembers",
                column: "ParentMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantMembers_TenantId_IsActive",
                table: "TenantMembers",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantMembers_UserId_TenantId",
                table: "TenantMembers",
                columns: new[] { "UserId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantMetadata_TenantId",
                table: "TenantMetadata",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantPermissions_ExpiresAt",
                table: "TenantPermissions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenantPermissions_TenantId",
                table: "TenantPermissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantPermissions_TenantId_UserId",
                table: "TenantPermissions",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantPermissions_User_Tenant",
                table: "TenantPermissions",
                columns: new[] { "UserId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantPermissions_UserId",
                table: "TenantPermissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Name",
                table: "Tenants",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantSecurityVersions_TenantId",
                table: "TenantSecurityVersions",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantSettings_TenantId",
                table: "TenantSettings",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantStatistics_StatisticDate",
                table: "TenantStatistics",
                column: "StatisticDate");

            migrationBuilder.CreateIndex(
                name: "IX_TenantStatistics_TenantId",
                table: "TenantStatistics",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsageTracking_ResourceType",
                table: "UsageTracking",
                column: "ResourceType");

            migrationBuilder.CreateIndex(
                name: "IX_UsageTracking_TenantId_Date",
                table: "UsageTracking",
                columns: new[] { "TenantId", "Date" });

            migrationBuilder.CreateIndex(
                name: "ix_user_mfa_configuration_user_id",
                schema: "gameguild.authentication",
                table: "user_mfa_configuration",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_products_AccessEndDate",
                table: "user_products",
                column: "AccessEndDate");

            migrationBuilder.CreateIndex(
                name: "IX_user_products_AccessStatus",
                table: "user_products",
                column: "AccessStatus");

            migrationBuilder.CreateIndex(
                name: "IX_user_products_AcquisitionType",
                table: "user_products",
                column: "AcquisitionType");

            migrationBuilder.CreateIndex(
                name: "IX_user_products_GiftedByUserId",
                table: "user_products",
                column: "GiftedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_products_ProductId",
                table: "user_products",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_user_products_SubscriptionId",
                table: "user_products",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_user_products_UserId",
                table: "user_products",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_products_UserId_ProductId",
                table: "user_products",
                columns: new[] { "UserId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_user_role_assigned_by",
                schema: "gameguild.authentication",
                table: "user_role",
                column: "assigned_by");

            migrationBuilder.CreateIndex(
                name: "idx_user_role_expires_at",
                schema: "gameguild.authentication",
                table: "user_role",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "idx_user_role_role_id",
                schema: "gameguild.authentication",
                table: "user_role",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "idx_user_role_user_id",
                schema: "gameguild.authentication",
                table: "user_role",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_user_role_user_id_role_id",
                schema: "gameguild.authentication",
                table: "user_role",
                columns: new[] { "user_id", "role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserMetadata_UserId",
                table: "UserMetadata",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_CreatedAt",
                table: "UserNotifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_IsRead",
                table: "UserNotifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_Priority",
                table: "UserNotifications",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_Type",
                table: "UserNotifications",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId",
                table: "UserNotifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_UserId",
                table: "UserPreferences",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_UserId",
                table: "UserProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserWebAuthnCredentials_CredentialId",
                schema: "auth",
                table: "UserWebAuthnCredentials",
                column: "CredentialId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserWebAuthnCredentials_UserId",
                schema: "auth",
                table: "UserWebAuthnCredentials",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserWebAuthnCredentials_UserId_IsActive",
                schema: "auth",
                table: "UserWebAuthnCredentials",
                columns: new[] { "UserId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AbacPolicies");

            migrationBuilder.DropTable(
                name: "AccessControlListEntries");

            migrationBuilder.DropTable(
                name: "AccessReviewItems");

            migrationBuilder.DropTable(
                name: "authenticationattempt",
                schema: "gameguild.authentication");

            migrationBuilder.DropTable(
                name: "blockchaincertificateanchor",
                schema: "gameguild.authentication");

            migrationBuilder.DropTable(
                name: "ConditionalPolicies");

            migrationBuilder.DropTable(
                name: "contenttypepermission",
                schema: "gameguild.authentication");

            migrationBuilder.DropTable(
                name: "DataMaskingRules");

            migrationBuilder.DropTable(
                name: "DelegatedAdminScopes");

            migrationBuilder.DropTable(
                name: "DynamicRoleAssignment");

            migrationBuilder.DropTable(
                name: "identityverification",
                schema: "gameguild.authentication");

            migrationBuilder.DropTable(
                name: "JitElevationRequests");

            migrationBuilder.DropTable(
                name: "mfaattempt",
                schema: "gameguild.authentication");

            migrationBuilder.DropTable(
                name: "order_audit_logs");

            migrationBuilder.DropTable(
                name: "order_line_items");

            migrationBuilder.DropTable(
                name: "PermissionDelegations");

            migrationBuilder.DropTable(
                name: "PermissionTemplates");

            migrationBuilder.DropTable(
                name: "PolicyDefinitions");

            migrationBuilder.DropTable(
                name: "pricing_rules");

            migrationBuilder.DropTable(
                name: "pricing_tiers");

            migrationBuilder.DropTable(
                name: "product_bundle_items");

            migrationBuilder.DropTable(
                name: "product_commission_configs");

            migrationBuilder.DropTable(
                name: "product_pricing_versions");

            migrationBuilder.DropTable(
                name: "product_subscription_plans");

            migrationBuilder.DropTable(
                name: "promo_code_uses");

            migrationBuilder.DropTable(
                name: "promo_stacking_rules");

            migrationBuilder.DropTable(
                name: "refreshtoken",
                schema: "gameguild.authentication");

            migrationBuilder.DropTable(
                name: "ResourceInvitation");

            migrationBuilder.DropTable(
                name: "ResourceUserPermission");

            migrationBuilder.DropTable(
                name: "service_accounts",
                schema: "gameguild.authentication");

            migrationBuilder.DropTable(
                name: "SoDViolations");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "TenantDomains");

            migrationBuilder.DropTable(
                name: "TenantMembers");

            migrationBuilder.DropTable(
                name: "TenantMetadata");

            migrationBuilder.DropTable(
                name: "TenantPermissions");

            migrationBuilder.DropTable(
                name: "TenantSecurityVersions");

            migrationBuilder.DropTable(
                name: "TenantSettings");

            migrationBuilder.DropTable(
                name: "TenantStatistics");

            migrationBuilder.DropTable(
                name: "trusteddevice",
                schema: "gameguild.authentication");

            migrationBuilder.DropTable(
                name: "UsageTracking");

            migrationBuilder.DropTable(
                name: "user_mfa_configuration",
                schema: "gameguild.authentication");

            migrationBuilder.DropTable(
                name: "user_role",
                schema: "gameguild.authentication");

            migrationBuilder.DropTable(
                name: "UserMetadata");

            migrationBuilder.DropTable(
                name: "UserNotifications");

            migrationBuilder.DropTable(
                name: "UserPreferences");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "usersession",
                schema: "gameguild.authentication");

            migrationBuilder.DropTable(
                name: "UserWebAuthnCredentials",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "AccessReviewCampaigns");

            migrationBuilder.DropTable(
                name: "DynamicRole");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "user_products");

            migrationBuilder.DropTable(
                name: "product_pricing");

            migrationBuilder.DropTable(
                name: "promo_codes");

            migrationBuilder.DropTable(
                name: "SoDRules");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "role",
                schema: "gameguild.authentication");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
