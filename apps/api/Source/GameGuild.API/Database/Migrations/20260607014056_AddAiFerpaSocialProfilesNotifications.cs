using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddAiFerpaSocialProfilesNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IntegrationSettingsJson",
                table: "TenantSettings",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ai_conversation_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestKind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Model = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RequestText = table.Column<string>(type: "text", nullable: false),
                    SystemPrompt = table.Column<string>(type: "text", nullable: true),
                    ResponseText = table.Column<string>(type: "text", nullable: true),
                    Outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OutcomeCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    OutcomeReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    FinishReason = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    InputTokens = table.Column<int>(type: "integer", nullable: true),
                    OutputTokens = table.Column<int>(type: "integer", nullable: true),
                    TotalTokens = table.Column<int>(type: "integer", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_conversation_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ai_prompt_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SystemPrompt = table.Column<string>(type: "text", nullable: true),
                    Prompt = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystemTemplate = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ai_prompt_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "billing_webhook_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExternalEventId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    Headers = table.Column<string>(type: "text", nullable: true),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    IsFailed = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessingAttempts = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_billing_webhook_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ferpa_directory_information_policies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AllowedFieldsJson = table.Column<string>(type: "jsonb", nullable: false),
                    OptOutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AnnualNoticeSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NoticeUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ferpa_directory_information_policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ferpa_disclosure_consents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuardianUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Recipient = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Scope = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ferpa_disclosure_consents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ferpa_disclosure_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisclosedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Recipient = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Basis = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RecordIdsJson = table.Column<string>(type: "jsonb", nullable: false),
                    DisclosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ferpa_disclosure_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ferpa_education_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordKind = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ExternalRecordId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ProtectionLevel = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsDirectoryInformation = table.Column<bool>(type: "boolean", nullable: false),
                    RetentionUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ferpa_education_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ferpa_inspection_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessingNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ferpa_inspection_requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VoidReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Metadata = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExternalPaymentId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ExternalTransactionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ExternalCustomerId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PaymentMethodId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ErrorCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CancelledByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    MaxRetries = table.Column<int>(type: "integer", nullable: false),
                    NextRetryAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefundedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RefundId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    RefundReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "social_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Handle = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Bio = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BannerUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Headline = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Location = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    TimeZone = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SocialLinksJson = table.Column<string>(type: "jsonb", nullable: false),
                    Visibility = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AvailabilityStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ShowActivity = table.Column<bool>(type: "boolean", nullable: false),
                    ShowPortfolio = table.Column<bool>(type: "boolean", nullable: false),
                    ShowSkills = table.Column<bool>(type: "boolean", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletenessScore = table.Column<int>(type: "integer", nullable: false),
                    FollowerCount = table.Column<int>(type: "integer", nullable: false),
                    FollowingCount = table.Column<int>(type: "integer", nullable: false),
                    PostCount = table.Column<int>(type: "integer", nullable: false),
                    ProjectCount = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_wallets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsLocked = table.Column<bool>(type: "boolean", nullable: false),
                    LockReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LastTransactionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DailyLimit = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    MonthlyLimit = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_wallets", x => x.Id);
                    table.CheckConstraint("CK_UserWallet_Balance_NonNegative", "\"Balance\" >= 0");
                    table.CheckConstraint("CK_UserWallet_UserId_NotEmpty", "\"UserId\" <> '00000000-0000-0000-0000-000000000000'::uuid");
                });

            migrationBuilder.CreateTable(
                name: "social_profile_portfolio_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_profile_portfolio_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_social_profile_portfolio_items_social_profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "social_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "social_profile_skills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Proficiency = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_profile_skills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_social_profile_skills_social_profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "social_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wallet_transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ReferenceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallet_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_wallet_transactions_user_wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "user_wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ai_conversation_logs_Outcome",
                table: "ai_conversation_logs",
                column: "Outcome");

            migrationBuilder.CreateIndex(
                name: "IX_ai_conversation_logs_Provider",
                table: "ai_conversation_logs",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_ai_conversation_logs_TenantId_OccurredAt",
                table: "ai_conversation_logs",
                columns: new[] { "TenantId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ai_conversation_logs_UserId",
                table: "ai_conversation_logs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ai_prompt_templates_Category",
                table: "ai_prompt_templates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ai_prompt_templates_IsActive",
                table: "ai_prompt_templates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ai_prompt_templates_TenantId_Key",
                table: "ai_prompt_templates",
                columns: new[] { "TenantId", "Key" });

            migrationBuilder.CreateIndex(
                name: "ix_billing_webhook_events_created_at",
                table: "billing_webhook_events",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "ix_billing_webhook_events_event_type",
                table: "billing_webhook_events",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "ix_billing_webhook_events_external_id_provider",
                table: "billing_webhook_events",
                columns: new[] { "ExternalEventId", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_billing_webhook_events_is_failed",
                table: "billing_webhook_events",
                column: "IsFailed");

            migrationBuilder.CreateIndex(
                name: "ix_billing_webhook_events_is_processed",
                table: "billing_webhook_events",
                column: "IsProcessed");

            migrationBuilder.CreateIndex(
                name: "ix_billing_webhook_events_subscription_id",
                table: "billing_webhook_events",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "ix_billing_webhook_events_tenant_id",
                table: "billing_webhook_events",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ferpa_directory_information_policies_TenantId",
                table: "ferpa_directory_information_policies",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ferpa_disclosure_consents_StudentUserId",
                table: "ferpa_disclosure_consents",
                column: "StudentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ferpa_disclosure_consents_StudentUserId_Recipient_Scope",
                table: "ferpa_disclosure_consents",
                columns: new[] { "StudentUserId", "Recipient", "Scope" });

            migrationBuilder.CreateIndex(
                name: "IX_ferpa_disclosure_logs_DisclosedAt",
                table: "ferpa_disclosure_logs",
                column: "DisclosedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ferpa_disclosure_logs_StudentUserId",
                table: "ferpa_disclosure_logs",
                column: "StudentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ferpa_education_records_ExternalRecordId",
                table: "ferpa_education_records",
                column: "ExternalRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ferpa_education_records_RecordKind",
                table: "ferpa_education_records",
                column: "RecordKind");

            migrationBuilder.CreateIndex(
                name: "IX_ferpa_education_records_StudentUserId",
                table: "ferpa_education_records",
                column: "StudentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ferpa_education_records_TenantId",
                table: "ferpa_education_records",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ferpa_inspection_requests_Deadline",
                table: "ferpa_inspection_requests",
                column: "Deadline");

            migrationBuilder.CreateIndex(
                name: "IX_ferpa_inspection_requests_Status",
                table: "ferpa_inspection_requests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ferpa_inspection_requests_StudentUserId",
                table: "ferpa_inspection_requests",
                column: "StudentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_DueDate",
                table: "invoices",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_ExternalId",
                table: "invoices",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_InvoiceNumber",
                table: "invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_IssuedAt",
                table: "invoices",
                column: "IssuedAt");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_PaymentId_Unique",
                table: "invoices",
                column: "PaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_SubscriptionId",
                table: "invoices",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_TenantId_Status",
                table: "invoices",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_payments_ExternalPaymentId",
                table: "payments",
                column: "ExternalPaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_IdempotencyKey",
                table: "payments",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_SubscriptionId",
                table: "payments",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_TenantId_Status",
                table: "payments",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_social_profile_portfolio_items_ProfileId",
                table: "social_profile_portfolio_items",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_social_profile_portfolio_items_ProjectId",
                table: "social_profile_portfolio_items",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_social_profile_skills_ProfileId",
                table: "social_profile_skills",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_social_profile_skills_ProfileId_Name",
                table: "social_profile_skills",
                columns: new[] { "ProfileId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_social_profiles_Handle",
                table: "social_profiles",
                column: "Handle",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_social_profiles_UserId",
                table: "social_profiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_social_profiles_Visibility",
                table: "social_profiles",
                column: "Visibility");

            migrationBuilder.CreateIndex(
                name: "IX_user_wallets_Currency",
                table: "user_wallets",
                column: "Currency");

            migrationBuilder.CreateIndex(
                name: "IX_user_wallets_IsActive",
                table: "user_wallets",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_user_wallets_UserId",
                table: "user_wallets",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wallet_transactions_CreatedAt",
                table: "wallet_transactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_transactions_ReferenceId",
                table: "wallet_transactions",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_transactions_Status",
                table: "wallet_transactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_transactions_Type",
                table: "wallet_transactions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_transactions_WalletId",
                table: "wallet_transactions",
                column: "WalletId");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_conversation_logs");

            migrationBuilder.DropTable(
                name: "ai_prompt_templates");

            migrationBuilder.DropTable(
                name: "billing_webhook_events");

            migrationBuilder.DropTable(
                name: "ferpa_directory_information_policies");

            migrationBuilder.DropTable(
                name: "ferpa_disclosure_consents");

            migrationBuilder.DropTable(
                name: "ferpa_disclosure_logs");

            migrationBuilder.DropTable(
                name: "ferpa_education_records");

            migrationBuilder.DropTable(
                name: "ferpa_inspection_requests");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "social_profile_portfolio_items");

            migrationBuilder.DropTable(
                name: "social_profile_skills");

            migrationBuilder.DropTable(
                name: "wallet_transactions");

            migrationBuilder.DropTable(
                name: "social_profiles");

            migrationBuilder.DropTable(
                name: "user_wallets");

            migrationBuilder.DropColumn(
                name: "IntegrationSettingsJson",
                table: "TenantSettings");
        }
    }
}
