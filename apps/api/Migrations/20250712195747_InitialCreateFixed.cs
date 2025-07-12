using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateFixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Team",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Team", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "activity_grades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ContentInteractionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    GraderProgramUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Grade = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Feedback = table.Column<string>(type: "TEXT", nullable: true),
                    GradingDetails = table.Column<string>(type: "jsonb", nullable: true),
                    GradedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProgramUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activity_grades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "certificate_blockchain_anchors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CertificateId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BlockchainNetwork = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TransactionHash = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    BlockHash = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    BlockNumber = table.Column<long>(type: "INTEGER", nullable: true),
                    ContractAddress = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TokenId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AnchoredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_certificate_blockchain_anchors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "certificate_tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CertificateId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TagId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RelationshipType = table.Column<int>(type: "INTEGER", nullable: false),
                    TagProficiencyId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_certificate_tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "certificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    ProgramId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CompletionPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MinimumGrade = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    RequiresFeedback = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiresRating = table.Column<bool>(type: "INTEGER", nullable: false),
                    MinimumRating = table.Column<decimal>(type: "decimal(2,1)", nullable: true),
                    ValidityDays = table.Column<int>(type: "INTEGER", nullable: true),
                    VerificationMethod = table.Column<int>(type: "INTEGER", nullable: false),
                    CertificateTemplate = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_certificates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Comment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommentPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PermissionFlags1 = table.Column<ulong>(type: "bigint", nullable: false),
                    PermissionFlags2 = table.Column<ulong>(type: "bigint", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResourceId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentPermissions_Comment_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Comment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "content_interactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProgramUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SubmissionData = table.Column<string>(type: "jsonb", nullable: true),
                    CompletionPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TimeSpentMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    FirstAccessedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastAccessedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ProgramContentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_interactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentContentLicense",
                columns: table => new
                {
                    ContentsId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LicensesId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentContentLicense", x => new { x.ContentsId, x.LicensesId });
                });

            migrationBuilder.CreateTable(
                name: "ContentLicenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentLicenses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentTypePermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TenantPermissionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UserId1 = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PermissionFlags1 = table.Column<ulong>(type: "bigint", nullable: false),
                    PermissionFlags2 = table.Column<ulong>(type: "bigint", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentTypePermissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Credentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Credentials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "financial_transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    FromUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ToUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ExternalTransactionId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    PaymentMethodId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PromoCodeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PlatformFee = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    ProcessorFee = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    NetAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FailureReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PromoCodeId1 = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_transactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Jam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Theme = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Rules = table.Column<string>(type: "TEXT", nullable: true),
                    SubmissionCriteria = table.Column<string>(type: "TEXT", nullable: true),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VotingEndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    MaxParticipants = table.Column<int>(type: "INTEGER", nullable: true),
                    ParticipantCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jam", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JamScore",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SubmissionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CriteriaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    JudgeUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    Feedback = table.Column<string>(type: "TEXT", nullable: true),
                    ProjectJamSubmissionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JamScore", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Languages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Code = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "product_pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    SalePrice = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    SaleStartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SaleEndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_pricing", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "product_programs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProgramId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    ProgramId1 = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_programs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "product_subscription_plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    BillingInterval = table.Column<int>(type: "INTEGER", nullable: false),
                    IntervalCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TrialPeriodDays = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_subscription_plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PermissionFlags1 = table.Column<ulong>(type: "bigint", nullable: false),
                    PermissionFlags2 = table.Column<ulong>(type: "bigint", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResourceId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductPermissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ShortDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    IsBundle = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BundleItems = table.Column<string>(type: "jsonb", nullable: true),
                    ReferralCommissionPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MaxAffiliateDiscount = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    AffiliateCommissionPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "program_contents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProgramId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Body = table.Column<string>(type: "jsonb", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsRequired = table.Column<bool>(type: "INTEGER", nullable: false),
                    GradingMethod = table.Column<int>(type: "INTEGER", nullable: true),
                    MaxPoints = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    EstimatedMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_contents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_program_contents_program_contents_ParentId",
                        column: x => x.ParentId,
                        principalTable: "program_contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "program_feedback_submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProgramId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProgramUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FeedbackData = table.Column<string>(type: "jsonb", nullable: false),
                    OverallRating = table.Column<decimal>(type: "decimal(2,1)", nullable: true),
                    Comments = table.Column<string>(type: "TEXT", nullable: true),
                    WouldRecommend = table.Column<bool>(type: "INTEGER", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_feedback_submissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_program_feedback_submissions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "program_ratings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProgramId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProgramUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Rating = table.Column<decimal>(type: "decimal(2,1)", nullable: false),
                    Review = table.Column<string>(type: "TEXT", nullable: true),
                    ContentQualityRating = table.Column<decimal>(type: "decimal(2,1)", nullable: true),
                    InstructorRating = table.Column<decimal>(type: "decimal(2,1)", nullable: true),
                    DifficultyRating = table.Column<decimal>(type: "decimal(2,1)", nullable: true),
                    ValueRating = table.Column<decimal>(type: "decimal(2,1)", nullable: true),
                    WouldRecommend = table.Column<bool>(type: "INTEGER", nullable: true),
                    ModerationStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    ModeratedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    ModeratedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_ratings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_program_ratings_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "program_users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProgramId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletionPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    FinalGrade = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastAccessedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "program_wishlists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProgramId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_wishlists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProgramPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PermissionFlags1 = table.Column<ulong>(type: "bigint", nullable: false),
                    PermissionFlags2 = table.Column<ulong>(type: "bigint", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResourceId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramPermissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "programs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Thumbnail = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    VideoShowcaseUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    EstimatedHours = table.Column<float>(type: "REAL", nullable: true),
                    EnrollmentStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxEnrollments = table.Column<int>(type: "INTEGER", nullable: true),
                    EnrollmentDeadline = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Difficulty = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_programs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectCategory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectCategory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectCollaborators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Permissions = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectCollaborators", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectFeedbacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    Content = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Categories = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsFeatured = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    HelpfulVotes = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalVotes = table.Column<int>(type: "INTEGER", nullable: false),
                    Platform = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ProjectVersion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectFeedbacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectFollowers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FollowedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    NotificationSettings = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    EmailNotifications = table.Column<bool>(type: "INTEGER", nullable: false),
                    PushNotifications = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectFollowers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectJamSubmissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    JamId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsEligible = table.Column<bool>(type: "INTEGER", nullable: false),
                    SubmissionNotes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    FinalScore = table.Column<decimal>(type: "TEXT", nullable: true),
                    Ranking = table.Column<int>(type: "INTEGER", nullable: true),
                    HasAward = table.Column<bool>(type: "INTEGER", nullable: false),
                    AwardDetails = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectJamSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectJamSubmissions_Jam_JamId",
                        column: x => x.JamId,
                        principalTable: "Jam",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ViewCount = table.Column<int>(type: "INTEGER", nullable: false),
                    DownloadCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FollowerCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMetadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PermissionFlags1 = table.Column<ulong>(type: "bigint", nullable: false),
                    PermissionFlags2 = table.Column<ulong>(type: "bigint", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResourceId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectPermissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectReleases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReleaseVersion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsLatest = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPrerelease = table.Column<bool>(type: "INTEGER", nullable: false),
                    DownloadUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: true),
                    DownloadCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ReleaseNotes = table.Column<string>(type: "TEXT", nullable: true),
                    Checksum = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    SystemRequirements = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    SupportedPlatforms = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ReleaseType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    BuildNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ReleaseMetadata = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectReleases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ShortDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    DevelopmentStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RepositoryUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SocialLinks = table.Column<string>(type: "TEXT", nullable: true),
                    DownloadUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Tags = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedById = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProjectCategoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_ProjectCategory_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ProjectCategory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Projects_ProjectCategory_ProjectCategoryId",
                        column: x => x.ProjectCategoryId,
                        principalTable: "ProjectCategory",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProjectTeams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TeamId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Permissions = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ContributionPercentage = table.Column<decimal>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectTeams_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectTeams_Team_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Team",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectVersion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VersionNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ReleaseNotes = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DownloadCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedById = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectVersion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectVersion_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "promo_code_uses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    PromoCodeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FinancialTransactionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DiscountApplied = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promo_code_uses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_promo_code_uses_financial_transactions_FinancialTransactionId",
                        column: x => x.FinancialTransactionId,
                        principalTable: "financial_transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "promo_codes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    MinimumOrderAmount = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    MaxUses = table.Column<int>(type: "INTEGER", nullable: true),
                    MaxUsesPerUser = table.Column<int>(type: "INTEGER", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promo_codes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_promo_codes_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Token = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsRevoked = table.Column<bool>(type: "INTEGER", nullable: false),
                    RevokedByIp = table.Column<string>(type: "TEXT", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedByIp = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReputationActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ActionType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Points = table.Column<int>(type: "INTEGER", nullable: false),
                    DailyLimit = table.Column<int>(type: "INTEGER", nullable: true),
                    TotalLimit = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    RequiredLevelId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReputationActions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReputationLevels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MinimumScore = table.Column<int>(type: "INTEGER", nullable: false),
                    MaximumScore = table.Column<int>(type: "INTEGER", nullable: true),
                    Color = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReputationLevels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResourceLocalizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ResourceType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LanguageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FieldName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ResourceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CommentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ContentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ContentLicenseId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProjectCategoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProjectCollaboratorId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProjectFeedbackId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProjectFollowerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProjectJamSubmissionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProjectReleaseId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProjectTeamId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ReputationActionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ReputationTierId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UserProfileId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UserReputationHistoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UserReputationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UserTenantReputationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceLocalizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceLocalizations_Comment_CommentId",
                        column: x => x.CommentId,
                        principalTable: "Comment",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResourceLocalizations_ContentLicenses_ContentLicenseId",
                        column: x => x.ContentLicenseId,
                        principalTable: "ContentLicenses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResourceLocalizations_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResourceLocalizations_ProjectCategory_ProjectCategoryId",
                        column: x => x.ProjectCategoryId,
                        principalTable: "ProjectCategory",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResourceLocalizations_ProjectCollaborators_ProjectCollaboratorId",
                        column: x => x.ProjectCollaboratorId,
                        principalTable: "ProjectCollaborators",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResourceLocalizations_ProjectFeedbacks_ProjectFeedbackId",
                        column: x => x.ProjectFeedbackId,
                        principalTable: "ProjectFeedbacks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResourceLocalizations_ProjectFollowers_ProjectFollowerId",
                        column: x => x.ProjectFollowerId,
                        principalTable: "ProjectFollowers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResourceLocalizations_ProjectJamSubmissions_ProjectJamSubmissionId",
                        column: x => x.ProjectJamSubmissionId,
                        principalTable: "ProjectJamSubmissions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResourceLocalizations_ProjectReleases_ProjectReleaseId",
                        column: x => x.ProjectReleaseId,
                        principalTable: "ProjectReleases",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResourceLocalizations_ProjectTeams_ProjectTeamId",
                        column: x => x.ProjectTeamId,
                        principalTable: "ProjectTeams",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResourceLocalizations_ReputationActions_ReputationActionId",
                        column: x => x.ReputationActionId,
                        principalTable: "ReputationActions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResourceLocalizations_ReputationLevels_ReputationTierId",
                        column: x => x.ReputationTierId,
                        principalTable: "ReputationLevels",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ResourceMetadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ResourceType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    AdditionalData = table.Column<string>(type: "jsonb", nullable: true),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SeoMetadata = table.Column<string>(type: "jsonb", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceMetadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tenants_ResourceMetadata_MetadataId",
                        column: x => x.MetadataId,
                        principalTable: "ResourceMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Tenants_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tag_proficiencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    ProficiencyLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: true),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tag_proficiencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tag_proficiencies_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: true),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tags_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TeamMember",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TeamId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    InvitedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMember", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMember_Team_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Team",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamMember_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TenantUserGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentGroupId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantUserGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantUserGroups_TenantUserGroups_ParentGroupId",
                        column: x => x.ParentGroupId,
                        principalTable: "TenantUserGroups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TenantUserGroups_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestingFeedbackForms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TestingRequestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FormSchema = table.Column<string>(type: "TEXT", nullable: false),
                    IsForOnline = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsForSessions = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestingFeedbackForms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestingFeedbackForms_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TestingLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    MaxTestersCapacity = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxProjectsCapacity = table.Column<int>(type: "INTEGER", nullable: false),
                    EquipmentAvailable = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestingLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestingLocations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    GivenName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FamilyName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProfiles_ResourceMetadata_MetadataId",
                        column: x => x.MetadataId,
                        principalTable: "ResourceMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserProfiles_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,8)", nullable: false, defaultValue: 0m),
                    AvailableBalance = table.Column<decimal>(type: "decimal(18,8)", nullable: false, defaultValue: 0m),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "tag_relationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SourceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(3,2)", nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tag_relationships", x => x.Id);
                    table.CheckConstraint("CK_TagRelationships_NoSelfReference", "\"SourceId\" != \"TargetId\"");
                    table.ForeignKey(
                        name: "FK_tag_relationships_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_tag_relationships_tags_SourceId",
                        column: x => x.SourceId,
                        principalTable: "tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tag_relationships_tags_TargetId",
                        column: x => x.TargetId,
                        principalTable: "tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantDomains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TopLevelDomain = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Subdomain = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsMainDomain = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSecondaryDomain = table.Column<bool>(type: "INTEGER", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserGroupId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantDomains", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantDomains_TenantUserGroups_UserGroupId",
                        column: x => x.UserGroupId,
                        principalTable: "TenantUserGroups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TenantDomains_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId1 = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PermissionFlags1 = table.Column<ulong>(type: "bigint", nullable: false),
                    PermissionFlags2 = table.Column<ulong>(type: "bigint", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantPermissions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TenantPermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TenantPermissions_Users_UserId1",
                        column: x => x.UserId1,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TenantUserGroupMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsAutoAssigned = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantUserGroupMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantUserGroupMemberships_TenantUserGroups_UserGroupId",
                        column: x => x.UserGroupId,
                        principalTable: "TenantUserGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TenantUserGroupMemberships_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TenantUserGroupMemberships_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestingRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProjectVersionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    InstructionsType = table.Column<int>(type: "INTEGER", nullable: false),
                    InstructionsContent = table.Column<string>(type: "TEXT", nullable: true),
                    InstructionsUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    InstructionsFileId = table.Column<Guid>(type: "TEXT", nullable: true),
                    MaxTesters = table.Column<int>(type: "INTEGER", nullable: true),
                    CurrentTesterCount = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedById = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestingRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestingRequests_ProjectVersion_ProjectVersionId",
                        column: x => x.ProjectVersionId,
                        principalTable: "ProjectVersion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestingRequests_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TestingRequests_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_certificates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CertificateId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProgramId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProgramUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    VerificationCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FinalGrade = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RevocationReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_certificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_certificates_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_user_certificates_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_user_certificates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_certificates_certificates_CertificateId",
                        column: x => x.CertificateId,
                        principalTable: "certificates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_certificates_program_users_ProgramUserId",
                        column: x => x.ProgramUserId,
                        principalTable: "program_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_user_certificates_programs_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "user_financial_methods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    LastFour = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    ExpiryMonth = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                    ExpiryYear = table.Column<string>(type: "TEXT", maxLength: 4, nullable: true),
                    Brand = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_financial_methods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_financial_methods_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_user_financial_methods_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_kyc_verifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Provider = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ExternalVerificationId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    VerificationLevel = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DocumentTypes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DocumentCountry = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    ProviderData = table.Column<string>(type: "jsonb", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_kyc_verifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_kyc_verifications_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_user_kyc_verifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubscriptionPlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ExternalSubscriptionId = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    CurrentPeriodStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CurrentPeriodEnd = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CanceledAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndsAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TrialEndsAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastPaymentAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NextBillingAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ProductSubscriptionPlanId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_subscriptions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_user_subscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_subscriptions_product_subscription_plans_ProductSubscriptionPlanId",
                        column: x => x.ProductSubscriptionPlanId,
                        principalTable: "product_subscription_plans",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_user_subscriptions_product_subscription_plans_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalTable: "product_subscription_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserReputations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentLevelId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastLevelCalculation = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PositiveChanges = table.Column<int>(type: "INTEGER", nullable: false),
                    NegativeChanges = table.Column<int>(type: "INTEGER", nullable: false),
                    ReputationTierId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReputations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserReputations_ReputationLevels_CurrentLevelId",
                        column: x => x.CurrentLevelId,
                        principalTable: "ReputationLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserReputations_ReputationLevels_ReputationTierId",
                        column: x => x.ReputationTierId,
                        principalTable: "ReputationLevels",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserReputations_ResourceMetadata_MetadataId",
                        column: x => x.MetadataId,
                        principalTable: "ResourceMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserReputations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserReputations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTenantReputations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantPermissionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentLevelId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastLevelCalculation = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PositiveChanges = table.Column<int>(type: "INTEGER", nullable: false),
                    NegativeChanges = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTenantReputations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTenantReputations_ReputationLevels_CurrentLevelId",
                        column: x => x.CurrentLevelId,
                        principalTable: "ReputationLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserTenantReputations_ResourceMetadata_MetadataId",
                        column: x => x.MetadataId,
                        principalTable: "ResourceMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserTenantReputations_TenantPermissions_TenantPermissionId",
                        column: x => x.TenantPermissionId,
                        principalTable: "TenantPermissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserTenantReputations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TestingParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TestingRequestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InstructionsAcknowledged = table.Column<bool>(type: "INTEGER", nullable: false),
                    InstructionsAcknowledgedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestingParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestingParticipants_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TestingParticipants_TestingRequests_TestingRequestId",
                        column: x => x.TestingRequestId,
                        principalTable: "TestingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestingParticipants_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestingSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TestingRequestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LocationId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    SessionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MaxTesters = table.Column<int>(type: "INTEGER", nullable: false),
                    RegisteredTesterCount = table.Column<int>(type: "INTEGER", nullable: false),
                    RegisteredProjectMemberCount = table.Column<int>(type: "INTEGER", nullable: false),
                    RegisteredProjectCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ManagerUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ManagerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedById = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestingSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestingSessions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TestingSessions_TestingLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "TestingLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestingSessions_TestingRequests_TestingRequestId",
                        column: x => x.TestingRequestId,
                        principalTable: "TestingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestingSessions_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestingSessions_Users_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AcquisitionType = table.Column<int>(type: "INTEGER", nullable: false),
                    AccessStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    PricePaid = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    AccessStartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AccessEndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    GiftedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProductId1 = table.Column<Guid>(type: "TEXT", nullable: true),
                    UserSubscriptionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_products_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_products_Products_ProductId1",
                        column: x => x.ProductId1,
                        principalTable: "Products",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_user_products_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_user_products_Users_GiftedByUserId",
                        column: x => x.GiftedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_user_products_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_products_user_subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "user_subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_user_products_user_subscriptions_UserSubscriptionId",
                        column: x => x.UserSubscriptionId,
                        principalTable: "user_subscriptions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserReputationHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "INTEGER", nullable: false),
                    MetadataId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TenantPermissionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ReputationActionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PointsChange = table.Column<int>(type: "INTEGER", nullable: false),
                    PreviousScore = table.Column<int>(type: "INTEGER", nullable: false),
                    NewScore = table.Column<int>(type: "INTEGER", nullable: false),
                    PreviousLevelId = table.Column<Guid>(type: "TEXT", nullable: true),
                    NewLevelId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    TriggeredByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RelatedResourceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserReputationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UserTenantReputationId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReputationHistory", x => x.Id);
                    table.CheckConstraint("CK_UserReputationHistory_UserOrUserTenant", "(\"UserId\" IS NOT NULL AND \"UserTenantId\" IS NULL) OR (\"UserId\" IS NULL AND \"UserTenantId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_UserReputationHistory_ReputationActions_ReputationActionId",
                        column: x => x.ReputationActionId,
                        principalTable: "ReputationActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserReputationHistory_ReputationLevels_NewLevelId",
                        column: x => x.NewLevelId,
                        principalTable: "ReputationLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserReputationHistory_ReputationLevels_PreviousLevelId",
                        column: x => x.PreviousLevelId,
                        principalTable: "ReputationLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserReputationHistory_ResourceMetadata_MetadataId",
                        column: x => x.MetadataId,
                        principalTable: "ResourceMetadata",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserReputationHistory_TenantPermissions_TenantPermissionId",
                        column: x => x.TenantPermissionId,
                        principalTable: "TenantPermissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserReputationHistory_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserReputationHistory_UserReputations_UserReputationId",
                        column: x => x.UserReputationId,
                        principalTable: "UserReputations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserReputationHistory_UserTenantReputations_UserTenantReputationId",
                        column: x => x.UserTenantReputationId,
                        principalTable: "UserTenantReputations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserReputationHistory_Users_TriggeredByUserId",
                        column: x => x.TriggeredByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserReputationHistory_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SessionRegistrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RegistrationType = table.Column<int>(type: "INTEGER", nullable: false),
                    ProjectRole = table.Column<int>(type: "INTEGER", nullable: true),
                    RegistrationNotes = table.Column<string>(type: "TEXT", nullable: true),
                    AttendanceStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    AttendedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionRegistrations_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SessionRegistrations_TestingSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "TestingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessionRegistrations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionWaitlists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RegistrationType = table.Column<int>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false),
                    RegistrationNotes = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionWaitlists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionWaitlists_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SessionWaitlists_TestingSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "TestingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SessionWaitlists_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestingFeedback",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TestingRequestId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FeedbackFormId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    TestingContext = table.Column<int>(type: "INTEGER", nullable: false),
                    FeedbackData = table.Column<string>(type: "TEXT", nullable: false),
                    AdditionalNotes = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestingFeedback", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestingFeedback_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TestingFeedback_TestingFeedbackForms_FeedbackFormId",
                        column: x => x.FeedbackFormId,
                        principalTable: "TestingFeedbackForms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestingFeedback_TestingRequests_TestingRequestId",
                        column: x => x.TestingRequestId,
                        principalTable: "TestingRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestingFeedback_TestingSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "TestingSessions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TestingFeedback_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_activity_grades_ContentInteractionId",
                table: "activity_grades",
                column: "ContentInteractionId");

            migrationBuilder.CreateIndex(
                name: "IX_activity_grades_CreatedAt",
                table: "activity_grades",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_activity_grades_DeletedAt",
                table: "activity_grades",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_activity_grades_GraderProgramUserId",
                table: "activity_grades",
                column: "GraderProgramUserId");

            migrationBuilder.CreateIndex(
                name: "IX_activity_grades_ProgramUserId",
                table: "activity_grades",
                column: "ProgramUserId");

            migrationBuilder.CreateIndex(
                name: "IX_activity_grades_TenantId",
                table: "activity_grades",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_certificate_blockchain_anchors_AnchoredAt",
                table: "certificate_blockchain_anchors",
                column: "AnchoredAt");

            migrationBuilder.CreateIndex(
                name: "IX_certificate_blockchain_anchors_BlockchainNetwork",
                table: "certificate_blockchain_anchors",
                column: "BlockchainNetwork");

            migrationBuilder.CreateIndex(
                name: "IX_certificate_blockchain_anchors_CertificateId",
                table: "certificate_blockchain_anchors",
                column: "CertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_certificate_blockchain_anchors_CreatedAt",
                table: "certificate_blockchain_anchors",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_certificate_blockchain_anchors_DeletedAt",
                table: "certificate_blockchain_anchors",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_certificate_blockchain_anchors_Status",
                table: "certificate_blockchain_anchors",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_certificate_blockchain_anchors_TenantId",
                table: "certificate_blockchain_anchors",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_certificate_blockchain_anchors_TransactionHash",
                table: "certificate_blockchain_anchors",
                column: "TransactionHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_certificate_tags_CertificateId",
                table: "certificate_tags",
                column: "CertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_certificate_tags_CertificateId_TagId",
                table: "certificate_tags",
                columns: new[] { "CertificateId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_certificate_tags_CreatedAt",
                table: "certificate_tags",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_certificate_tags_DeletedAt",
                table: "certificate_tags",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_certificate_tags_RelationshipType",
                table: "certificate_tags",
                column: "RelationshipType");

            migrationBuilder.CreateIndex(
                name: "IX_certificate_tags_TagId",
                table: "certificate_tags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_certificate_tags_TagProficiencyId",
                table: "certificate_tags",
                column: "TagProficiencyId");

            migrationBuilder.CreateIndex(
                name: "IX_certificate_tags_TenantId",
                table: "certificate_tags",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_certificates_CompletionPercentage",
                table: "certificates",
                column: "CompletionPercentage");

            migrationBuilder.CreateIndex(
                name: "IX_certificates_CreatedAt",
                table: "certificates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_certificates_DeletedAt",
                table: "certificates",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_certificates_ProductId",
                table: "certificates",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_certificates_ProgramId",
                table: "certificates",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_certificates_TenantId",
                table: "certificates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_certificates_Type",
                table: "certificates",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_CreatedAt",
                table: "Comment",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_DeletedAt",
                table: "Comment",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_MetadataId",
                table: "Comment",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_TenantId",
                table: "Comment",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentPermissions_CreatedAt",
                table: "CommentPermissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CommentPermissions_DeletedAt",
                table: "CommentPermissions",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CommentPermissions_Expiration",
                table: "CommentPermissions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_CommentPermissions_Resource_User",
                table: "CommentPermissions",
                columns: new[] { "ResourceId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_CommentPermissions_TenantId",
                table: "CommentPermissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentPermissions_User_Tenant_Resource",
                table: "CommentPermissions",
                columns: new[] { "UserId", "TenantId", "ResourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_content_interactions_ContentId",
                table: "content_interactions",
                column: "ContentId");

            migrationBuilder.CreateIndex(
                name: "IX_content_interactions_CreatedAt",
                table: "content_interactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_content_interactions_DeletedAt",
                table: "content_interactions",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_content_interactions_ProgramContentId",
                table: "content_interactions",
                column: "ProgramContentId");

            migrationBuilder.CreateIndex(
                name: "IX_content_interactions_ProgramUserId",
                table: "content_interactions",
                column: "ProgramUserId");

            migrationBuilder.CreateIndex(
                name: "IX_content_interactions_TenantId",
                table: "content_interactions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentContentLicense_LicensesId",
                table: "ContentContentLicense",
                column: "LicensesId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentLicenses_CreatedAt",
                table: "ContentLicenses",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContentLicenses_DeletedAt",
                table: "ContentLicenses",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContentLicenses_MetadataId",
                table: "ContentLicenses",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentLicenses_TenantId",
                table: "ContentLicenses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypePermissions_ContentType_Tenant",
                table: "ContentTypePermissions",
                columns: new[] { "ContentType", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypePermissions_ContentType_User_Tenant",
                table: "ContentTypePermissions",
                columns: new[] { "ContentType", "UserId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypePermissions_CreatedAt",
                table: "ContentTypePermissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypePermissions_DeletedAt",
                table: "ContentTypePermissions",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypePermissions_ExpiresAt",
                table: "ContentTypePermissions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypePermissions_TenantId",
                table: "ContentTypePermissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypePermissions_TenantPermissionId",
                table: "ContentTypePermissions",
                column: "TenantPermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypePermissions_UserId",
                table: "ContentTypePermissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentTypePermissions_UserId1",
                table: "ContentTypePermissions",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_Credentials_CreatedAt",
                table: "Credentials",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Credentials_DeletedAt",
                table: "Credentials",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Credentials_TenantId",
                table: "Credentials",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Credentials_UserId_Type",
                table: "Credentials",
                columns: new[] { "UserId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_financial_transactions_Amount",
                table: "financial_transactions",
                column: "Amount");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transactions_CreatedAt",
                table: "financial_transactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transactions_DeletedAt",
                table: "financial_transactions",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transactions_ExternalTransactionId",
                table: "financial_transactions",
                column: "ExternalTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transactions_FromUserId",
                table: "financial_transactions",
                column: "FromUserId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transactions_PaymentMethodId",
                table: "financial_transactions",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transactions_ProcessedAt",
                table: "financial_transactions",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transactions_PromoCodeId",
                table: "financial_transactions",
                column: "PromoCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transactions_PromoCodeId1",
                table: "financial_transactions",
                column: "PromoCodeId1");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transactions_Status",
                table: "financial_transactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transactions_TenantId",
                table: "financial_transactions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transactions_ToUserId",
                table: "financial_transactions",
                column: "ToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_financial_transactions_Type",
                table: "financial_transactions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Jam_CreatedAt",
                table: "Jam",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Jam_DeletedAt",
                table: "Jam",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Jam_TenantId",
                table: "Jam",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_JamScore_CreatedAt",
                table: "JamScore",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_JamScore_DeletedAt",
                table: "JamScore",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_JamScore_ProjectJamSubmissionId",
                table: "JamScore",
                column: "ProjectJamSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_JamScore_TenantId",
                table: "JamScore",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Languages_Code",
                table: "Languages",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Languages_CreatedAt",
                table: "Languages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Languages_DeletedAt",
                table: "Languages",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Languages_Name",
                table: "Languages",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Languages_TenantId",
                table: "Languages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_product_pricing_CreatedAt",
                table: "product_pricing",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_product_pricing_Currency",
                table: "product_pricing",
                column: "Currency");

            migrationBuilder.CreateIndex(
                name: "IX_product_pricing_DeletedAt",
                table: "product_pricing",
                column: "DeletedAt");

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
                name: "IX_product_pricing_TenantId",
                table: "product_pricing",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_product_programs_CreatedAt",
                table: "product_programs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_product_programs_DeletedAt",
                table: "product_programs",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_product_programs_ProductId_ProgramId",
                table: "product_programs",
                columns: new[] { "ProductId", "ProgramId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_product_programs_ProductId_SortOrder",
                table: "product_programs",
                columns: new[] { "ProductId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_product_programs_ProgramId",
                table: "product_programs",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_product_programs_ProgramId1",
                table: "product_programs",
                column: "ProgramId1");

            migrationBuilder.CreateIndex(
                name: "IX_product_programs_TenantId",
                table: "product_programs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_product_subscription_plans_CreatedAt",
                table: "product_subscription_plans",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_product_subscription_plans_DeletedAt",
                table: "product_subscription_plans",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_product_subscription_plans_TenantId",
                table: "product_subscription_plans",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSubscriptionPlans_BillingInterval",
                table: "product_subscription_plans",
                column: "BillingInterval");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSubscriptionPlans_IsActive",
                table: "product_subscription_plans",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSubscriptionPlans_IsDefault",
                table: "product_subscription_plans",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSubscriptionPlans_Name",
                table: "product_subscription_plans",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSubscriptionPlans_Price",
                table: "product_subscription_plans",
                column: "Price");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSubscriptionPlans_ProductId",
                table: "product_subscription_plans",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPermissions_CreatedAt",
                table: "ProductPermissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPermissions_DeletedAt",
                table: "ProductPermissions",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPermissions_Expiration",
                table: "ProductPermissions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPermissions_Resource_User",
                table: "ProductPermissions",
                columns: new[] { "ResourceId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductPermissions_TenantId",
                table: "ProductPermissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductPermissions_User_Tenant_Resource",
                table: "ProductPermissions",
                columns: new[] { "UserId", "TenantId", "ResourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_CreatedAt",
                table: "Products",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CreatorId",
                table: "Products",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_DeletedAt",
                table: "Products",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Products_MetadataId",
                table: "Products",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Status",
                table: "Products",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId",
                table: "Products",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Visibility",
                table: "Products",
                column: "Visibility");

            migrationBuilder.CreateIndex(
                name: "IX_program_contents_CreatedAt",
                table: "program_contents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_program_contents_DeletedAt",
                table: "program_contents",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_program_contents_IsRequired",
                table: "program_contents",
                column: "IsRequired");

            migrationBuilder.CreateIndex(
                name: "IX_program_contents_ParentId",
                table: "program_contents",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_program_contents_ParentId_SortOrder",
                table: "program_contents",
                columns: new[] { "ParentId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_program_contents_ProgramId",
                table: "program_contents",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_program_contents_ProgramId_SortOrder",
                table: "program_contents",
                columns: new[] { "ProgramId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_program_contents_SortOrder",
                table: "program_contents",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_program_contents_TenantId",
                table: "program_contents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_program_contents_Type",
                table: "program_contents",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_program_contents_Visibility",
                table: "program_contents",
                column: "Visibility");

            migrationBuilder.CreateIndex(
                name: "IX_program_feedback_submissions_CreatedAt",
                table: "program_feedback_submissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_program_feedback_submissions_DeletedAt",
                table: "program_feedback_submissions",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_program_feedback_submissions_OverallRating",
                table: "program_feedback_submissions",
                column: "OverallRating");

            migrationBuilder.CreateIndex(
                name: "IX_program_feedback_submissions_ProductId",
                table: "program_feedback_submissions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_program_feedback_submissions_ProgramId",
                table: "program_feedback_submissions",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_program_feedback_submissions_ProgramUserId",
                table: "program_feedback_submissions",
                column: "ProgramUserId");

            migrationBuilder.CreateIndex(
                name: "IX_program_feedback_submissions_SubmittedAt",
                table: "program_feedback_submissions",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_program_feedback_submissions_TenantId",
                table: "program_feedback_submissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_program_feedback_submissions_UserId",
                table: "program_feedback_submissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_program_feedback_submissions_UserId_ProgramId",
                table: "program_feedback_submissions",
                columns: new[] { "UserId", "ProgramId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_program_ratings_CreatedAt",
                table: "program_ratings",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_program_ratings_DeletedAt",
                table: "program_ratings",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_program_ratings_ModeratedBy",
                table: "program_ratings",
                column: "ModeratedBy");

            migrationBuilder.CreateIndex(
                name: "IX_program_ratings_ModerationStatus",
                table: "program_ratings",
                column: "ModerationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_program_ratings_ProductId",
                table: "program_ratings",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_program_ratings_ProgramId",
                table: "program_ratings",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_program_ratings_ProgramUserId",
                table: "program_ratings",
                column: "ProgramUserId");

            migrationBuilder.CreateIndex(
                name: "IX_program_ratings_Rating",
                table: "program_ratings",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_program_ratings_SubmittedAt",
                table: "program_ratings",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_program_ratings_TenantId",
                table: "program_ratings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_program_ratings_UserId",
                table: "program_ratings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_program_ratings_UserId_ProgramId",
                table: "program_ratings",
                columns: new[] { "UserId", "ProgramId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_program_users_CompletionPercentage",
                table: "program_users",
                column: "CompletionPercentage");

            migrationBuilder.CreateIndex(
                name: "IX_program_users_CreatedAt",
                table: "program_users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_program_users_DeletedAt",
                table: "program_users",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_program_users_IsActive",
                table: "program_users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_program_users_JoinedAt",
                table: "program_users",
                column: "JoinedAt");

            migrationBuilder.CreateIndex(
                name: "IX_program_users_ProgramId",
                table: "program_users",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_program_users_TenantId",
                table: "program_users",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_program_users_UserId",
                table: "program_users",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_program_users_UserId_ProgramId",
                table: "program_users",
                columns: new[] { "UserId", "ProgramId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_program_wishlists_AddedAt",
                table: "program_wishlists",
                column: "AddedAt");

            migrationBuilder.CreateIndex(
                name: "IX_program_wishlists_CreatedAt",
                table: "program_wishlists",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_program_wishlists_DeletedAt",
                table: "program_wishlists",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_program_wishlists_ProgramId",
                table: "program_wishlists",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_program_wishlists_TenantId",
                table: "program_wishlists",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_program_wishlists_UserId",
                table: "program_wishlists",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_program_wishlists_UserId_ProgramId",
                table: "program_wishlists",
                columns: new[] { "UserId", "ProgramId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgramPermissions_CreatedAt",
                table: "ProgramPermissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramPermissions_DeletedAt",
                table: "ProgramPermissions",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramPermissions_Expiration",
                table: "ProgramPermissions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramPermissions_Resource_User",
                table: "ProgramPermissions",
                columns: new[] { "ResourceId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProgramPermissions_TenantId",
                table: "ProgramPermissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramPermissions_User_Tenant_Resource",
                table: "ProgramPermissions",
                columns: new[] { "UserId", "TenantId", "ResourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_programs_Category",
                table: "programs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_programs_CreatedAt",
                table: "programs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_programs_DeletedAt",
                table: "programs",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_programs_Difficulty",
                table: "programs",
                column: "Difficulty");

            migrationBuilder.CreateIndex(
                name: "IX_programs_MetadataId",
                table: "programs",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_programs_Slug",
                table: "programs",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_programs_Status",
                table: "programs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_programs_TenantId",
                table: "programs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_programs_Visibility",
                table: "programs",
                column: "Visibility");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCategory_CreatedAt",
                table: "ProjectCategory",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCategory_DeletedAt",
                table: "ProjectCategory",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCategory_MetadataId",
                table: "ProjectCategory",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCategory_TenantId",
                table: "ProjectCategory",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCollaborators_CreatedAt",
                table: "ProjectCollaborators",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCollaborators_DeletedAt",
                table: "ProjectCollaborators",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCollaborators_MetadataId",
                table: "ProjectCollaborators",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCollaborators_Project_User",
                table: "ProjectCollaborators",
                columns: new[] { "ProjectId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCollaborators_TenantId",
                table: "ProjectCollaborators",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectCollaborators_User",
                table: "ProjectCollaborators",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeedbacks_CreatedAt",
                table: "ProjectFeedbacks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeedbacks_Date",
                table: "ProjectFeedbacks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeedbacks_DeletedAt",
                table: "ProjectFeedbacks",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeedbacks_MetadataId",
                table: "ProjectFeedbacks",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeedbacks_Project_Rating",
                table: "ProjectFeedbacks",
                columns: new[] { "ProjectId", "Rating" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeedbacks_Project_User",
                table: "ProjectFeedbacks",
                columns: new[] { "ProjectId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeedbacks_TenantId",
                table: "ProjectFeedbacks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFeedbacks_User",
                table: "ProjectFeedbacks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFollowers_CreatedAt",
                table: "ProjectFollowers",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFollowers_Date",
                table: "ProjectFollowers",
                column: "FollowedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFollowers_DeletedAt",
                table: "ProjectFollowers",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFollowers_MetadataId",
                table: "ProjectFollowers",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFollowers_Project_User",
                table: "ProjectFollowers",
                columns: new[] { "ProjectId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFollowers_TenantId",
                table: "ProjectFollowers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFollowers_User",
                table: "ProjectFollowers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectJamSubmissions_CreatedAt",
                table: "ProjectJamSubmissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectJamSubmissions_Date",
                table: "ProjectJamSubmissions",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectJamSubmissions_DeletedAt",
                table: "ProjectJamSubmissions",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectJamSubmissions_Jam",
                table: "ProjectJamSubmissions",
                column: "JamId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectJamSubmissions_MetadataId",
                table: "ProjectJamSubmissions",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectJamSubmissions_Project_Jam",
                table: "ProjectJamSubmissions",
                columns: new[] { "ProjectId", "JamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectJamSubmissions_Score",
                table: "ProjectJamSubmissions",
                column: "FinalScore");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectJamSubmissions_TenantId",
                table: "ProjectJamSubmissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMetadata_ProjectId",
                table: "ProjectMetadata",
                column: "ProjectId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPermissions_CreatedAt",
                table: "ProjectPermissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPermissions_DeletedAt",
                table: "ProjectPermissions",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPermissions_Expiration",
                table: "ProjectPermissions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPermissions_Resource_User",
                table: "ProjectPermissions",
                columns: new[] { "ResourceId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPermissions_TenantId",
                table: "ProjectPermissions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPermissions_User_Tenant_Resource",
                table: "ProjectPermissions",
                columns: new[] { "UserId", "TenantId", "ResourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReleases_CreatedAt",
                table: "ProjectReleases",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReleases_DeletedAt",
                table: "ProjectReleases",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReleases_Latest",
                table: "ProjectReleases",
                column: "IsLatest");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReleases_MetadataId",
                table: "ProjectReleases",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReleases_Project_Date",
                table: "ProjectReleases",
                columns: new[] { "ProjectId", "ReleasedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReleases_Project_Version",
                table: "ProjectReleases",
                columns: new[] { "ProjectId", "ReleaseVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectReleases_TenantId",
                table: "ProjectReleases",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CategoryId",
                table: "Projects",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CategoryId_Status",
                table: "Projects",
                columns: new[] { "CategoryId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CreatedAt",
                table: "Projects",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CreatedById",
                table: "Projects",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_DeletedAt",
                table: "Projects",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_MetadataId",
                table: "Projects",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectCategoryId",
                table: "Projects",
                column: "ProjectCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Status",
                table: "Projects",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Status_Visibility",
                table: "Projects",
                columns: new[] { "Status", "Visibility" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_TenantId",
                table: "Projects",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Title",
                table: "Projects",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_UpdatedAt",
                table: "Projects",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Visibility",
                table: "Projects",
                column: "Visibility");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeams_CreatedAt",
                table: "ProjectTeams",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeams_Date",
                table: "ProjectTeams",
                column: "AssignedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeams_DeletedAt",
                table: "ProjectTeams",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeams_MetadataId",
                table: "ProjectTeams",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeams_Project_Team",
                table: "ProjectTeams",
                columns: new[] { "ProjectId", "TeamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeams_Team",
                table: "ProjectTeams",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeams_TenantId",
                table: "ProjectTeams",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectVersion_CreatedAt",
                table: "ProjectVersion",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectVersion_CreatedById",
                table: "ProjectVersion",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectVersion_DeletedAt",
                table: "ProjectVersion",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectVersion_ProjectId",
                table: "ProjectVersion",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectVersion_TenantId",
                table: "ProjectVersion",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_promo_code_uses_CreatedAt",
                table: "promo_code_uses",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_promo_code_uses_DeletedAt",
                table: "promo_code_uses",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_promo_code_uses_FinancialTransactionId",
                table: "promo_code_uses",
                column: "FinancialTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_promo_code_uses_PromoCodeId",
                table: "promo_code_uses",
                column: "PromoCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_promo_code_uses_TenantId",
                table: "promo_code_uses",
                column: "TenantId");

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
                name: "IX_promo_codes_CreatedAt",
                table: "promo_codes",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_promo_codes_CreatedBy",
                table: "promo_codes",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_promo_codes_DeletedAt",
                table: "promo_codes",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_promo_codes_IsActive",
                table: "promo_codes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_promo_codes_ProductId",
                table: "promo_codes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_promo_codes_TenantId",
                table: "promo_codes",
                column: "TenantId");

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
                name: "IX_RefreshTokens_CreatedAt",
                table: "RefreshTokens",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_DeletedAt",
                table: "RefreshTokens",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TenantId",
                table: "RefreshTokens",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationActions_ActionType",
                table: "ReputationActions",
                column: "ActionType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReputationActions_ActionType_TenantId",
                table: "ReputationActions",
                columns: new[] { "ActionType", "TenantId" },
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationActions_CreatedAt",
                table: "ReputationActions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationActions_DeletedAt",
                table: "ReputationActions",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationActions_IsActive",
                table: "ReputationActions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationActions_MetadataId",
                table: "ReputationActions",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationActions_Points",
                table: "ReputationActions",
                column: "Points");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationActions_RequiredLevelId",
                table: "ReputationActions",
                column: "RequiredLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationActions_TenantId",
                table: "ReputationActions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationLevels_CreatedAt",
                table: "ReputationLevels",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationLevels_DeletedAt",
                table: "ReputationLevels",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationLevels_MetadataId",
                table: "ReputationLevels",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationLevels_MinimumScore",
                table: "ReputationLevels",
                column: "MinimumScore");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationLevels_SortOrder",
                table: "ReputationLevels",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ReputationLevels_TenantId",
                table: "ReputationLevels",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLocalizations_CommentId",
                table: "ResourceLocalizations",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLocalizations_ContentId",
                table: "ResourceLocalizations",
                column: "ContentId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLocalizations_ContentLicenseId",
                table: "ResourceLocalizations",
                column: "ContentLicenseId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLocalizations_CreatedAt",
                table: "ResourceLocalizations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLocalizations_DeletedAt",
                table: "ResourceLocalizations",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLocalizations_LanguageId",
                table: "ResourceLocalizations",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLocalizations_ProjectCategoryId",
                table: "ResourceLocalizations",
                column: "ProjectCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLocalizations_ProjectCollaboratorId",
                table: "ResourceLocalizations",
                column: "ProjectCollaboratorId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLocalizations_ProjectFeedbackId",
                table: "ResourceLocalizations",
                column: "ProjectFeedbackId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLocalizations_ProjectFollowerId",
                table: "ResourceLocalizations",
                column: "ProjectFollowerId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLocalizations_ProjectJamSubmissionId",
                table: "ResourceLocalizations",
                column: "ProjectJamSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLocalizations_ProjectReleaseId",
                table: "ResourceLocalizations",
                column: "ProjectReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLocalizations_ProjectTeamId",
                table: "ResourceLocalizations",
                column: "ProjectTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLocalizations_ReputationActionId",
                table: "ResourceLocalizations",
                column: "ReputationActionId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLocalizations_ReputationTierId",
                table: "ResourceLocalizations",
                column: "ReputationTierId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLocalizations_TenantId",
                table: "ResourceLocalizations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLocalizations_UserProfileId",
                table: "ResourceLocalizations",
                column: "UserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLocalizations_UserReputationHistoryId",
                table: "ResourceLocalizations",
                column: "UserReputationHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLocalizations_UserReputationId",
                table: "ResourceLocalizations",
                column: "UserReputationId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLocalizations_UserTenantReputationId",
                table: "ResourceLocalizations",
                column: "UserTenantReputationId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceMetadata_CreatedAt",
                table: "ResourceMetadata",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceMetadata_DeletedAt",
                table: "ResourceMetadata",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceMetadata_ResourceType",
                table: "ResourceMetadata",
                column: "ResourceType");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceMetadata_TenantId",
                table: "ResourceMetadata",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRegistrations_CreatedAt",
                table: "SessionRegistrations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRegistrations_DeletedAt",
                table: "SessionRegistrations",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRegistrations_SessionId",
                table: "SessionRegistrations",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRegistrations_TenantId",
                table: "SessionRegistrations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionRegistrations_UserId",
                table: "SessionRegistrations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionWaitlists_CreatedAt",
                table: "SessionWaitlists",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SessionWaitlists_DeletedAt",
                table: "SessionWaitlists",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SessionWaitlists_SessionId",
                table: "SessionWaitlists",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionWaitlists_TenantId",
                table: "SessionWaitlists",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionWaitlists_UserId",
                table: "SessionWaitlists",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tag_proficiencies_CreatedAt",
                table: "tag_proficiencies",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_tag_proficiencies_DeletedAt",
                table: "tag_proficiencies",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_tag_proficiencies_IsActive",
                table: "tag_proficiencies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_tag_proficiencies_Name",
                table: "tag_proficiencies",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_tag_proficiencies_ProficiencyLevel",
                table: "tag_proficiencies",
                column: "ProficiencyLevel");

            migrationBuilder.CreateIndex(
                name: "IX_tag_proficiencies_TenantId",
                table: "tag_proficiencies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_tag_proficiencies_Type",
                table: "tag_proficiencies",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_tag_relationships_CreatedAt",
                table: "tag_relationships",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_tag_relationships_DeletedAt",
                table: "tag_relationships",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_tag_relationships_SourceId",
                table: "tag_relationships",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_tag_relationships_SourceId_TargetId",
                table: "tag_relationships",
                columns: new[] { "SourceId", "TargetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tag_relationships_TargetId",
                table: "tag_relationships",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_tag_relationships_TenantId",
                table: "tag_relationships",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_tag_relationships_Type",
                table: "tag_relationships",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_tags_CreatedAt",
                table: "tags",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_tags_DeletedAt",
                table: "tags",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_tags_IsActive",
                table: "tags",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_tags_Name",
                table: "tags",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_tags_Name_TenantId",
                table: "tags",
                columns: new[] { "Name", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tags_TenantId",
                table: "tags",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_tags_Type",
                table: "tags",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMember_CreatedAt",
                table: "TeamMember",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMember_DeletedAt",
                table: "TeamMember",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMember_TeamId",
                table: "TeamMember",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMember_TenantId",
                table: "TeamMember",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantDomains_CreatedAt",
                table: "TenantDomains",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenantDomains_DeletedAt",
                table: "TenantDomains",
                column: "DeletedAt");

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
                name: "IX_TenantDomains_UserGroupId",
                table: "TenantDomains",
                column: "UserGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantPermissions_CreatedAt",
                table: "TenantPermissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenantPermissions_DeletedAt",
                table: "TenantPermissions",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenantPermissions_ExpiresAt",
                table: "TenantPermissions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenantPermissions_TenantId",
                table: "TenantPermissions",
                column: "TenantId");

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
                name: "IX_TenantPermissions_UserId1",
                table: "TenantPermissions",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_CreatedAt",
                table: "Tenants",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_DeletedAt",
                table: "Tenants",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_MetadataId",
                table: "Tenants",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Name",
                table: "Tenants",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_TenantId",
                table: "Tenants",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUserGroupMemberships_CreatedAt",
                table: "TenantUserGroupMemberships",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUserGroupMemberships_DeletedAt",
                table: "TenantUserGroupMemberships",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUserGroupMemberships_TenantId",
                table: "TenantUserGroupMemberships",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUserGroupMemberships_UserGroupId",
                table: "TenantUserGroupMemberships",
                column: "UserGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUserGroupMemberships_UserId_UserGroupId",
                table: "TenantUserGroupMemberships",
                columns: new[] { "UserId", "UserGroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantUserGroups_CreatedAt",
                table: "TenantUserGroups",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUserGroups_DeletedAt",
                table: "TenantUserGroups",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUserGroups_ParentGroupId",
                table: "TenantUserGroups",
                column: "ParentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUserGroups_TenantId_Name",
                table: "TenantUserGroups",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestingFeedback_CreatedAt",
                table: "TestingFeedback",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestingFeedback_DeletedAt",
                table: "TestingFeedback",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestingFeedback_FeedbackFormId",
                table: "TestingFeedback",
                column: "FeedbackFormId");

            migrationBuilder.CreateIndex(
                name: "IX_TestingFeedback_SessionId",
                table: "TestingFeedback",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TestingFeedback_TenantId",
                table: "TestingFeedback",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TestingFeedback_TestingRequestId",
                table: "TestingFeedback",
                column: "TestingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TestingFeedback_UserId",
                table: "TestingFeedback",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TestingFeedbackForms_CreatedAt",
                table: "TestingFeedbackForms",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestingFeedbackForms_DeletedAt",
                table: "TestingFeedbackForms",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestingFeedbackForms_TenantId",
                table: "TestingFeedbackForms",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TestingLocations_CreatedAt",
                table: "TestingLocations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestingLocations_DeletedAt",
                table: "TestingLocations",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestingLocations_TenantId",
                table: "TestingLocations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TestingParticipants_CreatedAt",
                table: "TestingParticipants",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestingParticipants_DeletedAt",
                table: "TestingParticipants",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestingParticipants_TenantId",
                table: "TestingParticipants",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TestingParticipants_TestingRequestId",
                table: "TestingParticipants",
                column: "TestingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TestingParticipants_UserId",
                table: "TestingParticipants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TestingRequests_CreatedAt",
                table: "TestingRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestingRequests_CreatedById",
                table: "TestingRequests",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TestingRequests_DeletedAt",
                table: "TestingRequests",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestingRequests_ProjectVersionId",
                table: "TestingRequests",
                column: "ProjectVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_TestingRequests_TenantId",
                table: "TestingRequests",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TestingSessions_CreatedAt",
                table: "TestingSessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestingSessions_CreatedById",
                table: "TestingSessions",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TestingSessions_DeletedAt",
                table: "TestingSessions",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TestingSessions_LocationId",
                table: "TestingSessions",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TestingSessions_ManagerId",
                table: "TestingSessions",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_TestingSessions_TenantId",
                table: "TestingSessions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TestingSessions_TestingRequestId",
                table: "TestingSessions",
                column: "TestingRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_user_certificates_CertificateId",
                table: "user_certificates",
                column: "CertificateId");

            migrationBuilder.CreateIndex(
                name: "IX_user_certificates_CreatedAt",
                table: "user_certificates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_user_certificates_DeletedAt",
                table: "user_certificates",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_user_certificates_IssuedAt",
                table: "user_certificates",
                column: "IssuedAt");

            migrationBuilder.CreateIndex(
                name: "IX_user_certificates_ProductId",
                table: "user_certificates",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_user_certificates_ProgramId",
                table: "user_certificates",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_user_certificates_ProgramUserId",
                table: "user_certificates",
                column: "ProgramUserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_certificates_Status",
                table: "user_certificates",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_user_certificates_TenantId",
                table: "user_certificates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_user_certificates_UserId",
                table: "user_certificates",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_certificates_VerificationCode",
                table: "user_certificates",
                column: "VerificationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_financial_methods_CreatedAt",
                table: "user_financial_methods",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_user_financial_methods_DeletedAt",
                table: "user_financial_methods",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_user_financial_methods_ExternalId",
                table: "user_financial_methods",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_user_financial_methods_IsDefault",
                table: "user_financial_methods",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_user_financial_methods_Status",
                table: "user_financial_methods",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_user_financial_methods_TenantId",
                table: "user_financial_methods",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_user_financial_methods_Type",
                table: "user_financial_methods",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_user_financial_methods_UserId",
                table: "user_financial_methods",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_kyc_verifications_CreatedAt",
                table: "user_kyc_verifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_user_kyc_verifications_DeletedAt",
                table: "user_kyc_verifications",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_user_kyc_verifications_ExternalVerificationId",
                table: "user_kyc_verifications",
                column: "ExternalVerificationId");

            migrationBuilder.CreateIndex(
                name: "IX_user_kyc_verifications_Provider",
                table: "user_kyc_verifications",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_user_kyc_verifications_Status",
                table: "user_kyc_verifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_user_kyc_verifications_SubmittedAt",
                table: "user_kyc_verifications",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_user_kyc_verifications_TenantId",
                table: "user_kyc_verifications",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_user_kyc_verifications_UserId",
                table: "user_kyc_verifications",
                column: "UserId");

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
                name: "IX_user_products_CreatedAt",
                table: "user_products",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_user_products_DeletedAt",
                table: "user_products",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_user_products_GiftedByUserId",
                table: "user_products",
                column: "GiftedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_products_ProductId",
                table: "user_products",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_user_products_ProductId1",
                table: "user_products",
                column: "ProductId1");

            migrationBuilder.CreateIndex(
                name: "IX_user_products_SubscriptionId",
                table: "user_products",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_user_products_TenantId",
                table: "user_products",
                column: "TenantId");

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
                name: "IX_user_products_UserSubscriptionId",
                table: "user_products",
                column: "UserSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_user_subscriptions_CreatedAt",
                table: "user_subscriptions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_user_subscriptions_CurrentPeriodEnd",
                table: "user_subscriptions",
                column: "CurrentPeriodEnd");

            migrationBuilder.CreateIndex(
                name: "IX_user_subscriptions_CurrentPeriodStart",
                table: "user_subscriptions",
                column: "CurrentPeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_user_subscriptions_DeletedAt",
                table: "user_subscriptions",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_user_subscriptions_ExternalSubscriptionId",
                table: "user_subscriptions",
                column: "ExternalSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_user_subscriptions_NextBillingAt",
                table: "user_subscriptions",
                column: "NextBillingAt");

            migrationBuilder.CreateIndex(
                name: "IX_user_subscriptions_ProductSubscriptionPlanId",
                table: "user_subscriptions",
                column: "ProductSubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_user_subscriptions_Status",
                table: "user_subscriptions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_user_subscriptions_SubscriptionPlanId",
                table: "user_subscriptions",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_user_subscriptions_TenantId",
                table: "user_subscriptions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_user_subscriptions_UserId",
                table: "user_subscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_CreatedAt",
                table: "UserProfiles",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_DeletedAt",
                table: "UserProfiles",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_MetadataId",
                table: "UserProfiles",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_TenantId",
                table: "UserProfiles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReputationHistory_CreatedAt",
                table: "UserReputationHistory",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserReputationHistory_DeletedAt",
                table: "UserReputationHistory",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserReputationHistory_MetadataId",
                table: "UserReputationHistory",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReputationHistory_NewLevelId",
                table: "UserReputationHistory",
                column: "NewLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReputationHistory_OccurredAt",
                table: "UserReputationHistory",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserReputationHistory_PointsChange",
                table: "UserReputationHistory",
                column: "PointsChange");

            migrationBuilder.CreateIndex(
                name: "IX_UserReputationHistory_PreviousLevelId",
                table: "UserReputationHistory",
                column: "PreviousLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReputationHistory_RelatedResourceId",
                table: "UserReputationHistory",
                column: "RelatedResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReputationHistory_ReputationActionId",
                table: "UserReputationHistory",
                column: "ReputationActionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReputationHistory_TenantId",
                table: "UserReputationHistory",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReputationHistory_TenantPermissionId_OccurredAt",
                table: "UserReputationHistory",
                columns: new[] { "TenantPermissionId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserReputationHistory_TriggeredByUserId",
                table: "UserReputationHistory",
                column: "TriggeredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReputationHistory_UserId_OccurredAt",
                table: "UserReputationHistory",
                columns: new[] { "UserId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserReputationHistory_UserReputationId",
                table: "UserReputationHistory",
                column: "UserReputationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReputationHistory_UserTenantReputationId",
                table: "UserReputationHistory",
                column: "UserTenantReputationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReputations_CreatedAt",
                table: "UserReputations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserReputations_CurrentLevelId",
                table: "UserReputations",
                column: "CurrentLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReputations_DeletedAt",
                table: "UserReputations",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserReputations_MetadataId",
                table: "UserReputations",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReputations_ReputationTierId",
                table: "UserReputations",
                column: "ReputationTierId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReputations_Score",
                table: "UserReputations",
                column: "Score");

            migrationBuilder.CreateIndex(
                name: "IX_UserReputations_TenantId",
                table: "UserReputations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReputations_UserId",
                table: "UserReputations",
                column: "UserId",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                table: "Users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DeletedAt",
                table: "Users",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId",
                table: "Users",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UpdatedAt",
                table: "Users",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserTenantReputations_CreatedAt",
                table: "UserTenantReputations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserTenantReputations_CurrentLevelId",
                table: "UserTenantReputations",
                column: "CurrentLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTenantReputations_DeletedAt",
                table: "UserTenantReputations",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserTenantReputations_MetadataId",
                table: "UserTenantReputations",
                column: "MetadataId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTenantReputations_Score",
                table: "UserTenantReputations",
                column: "Score");

            migrationBuilder.CreateIndex(
                name: "IX_UserTenantReputations_TenantId",
                table: "UserTenantReputations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTenantReputations_TenantPermissionId",
                table: "UserTenantReputations",
                column: "TenantPermissionId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_activity_grades_Tenants_TenantId",
                table: "activity_grades",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_activity_grades_content_interactions_ContentInteractionId",
                table: "activity_grades",
                column: "ContentInteractionId",
                principalTable: "content_interactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_activity_grades_program_users_GraderProgramUserId",
                table: "activity_grades",
                column: "GraderProgramUserId",
                principalTable: "program_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_activity_grades_program_users_ProgramUserId",
                table: "activity_grades",
                column: "ProgramUserId",
                principalTable: "program_users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_certificate_blockchain_anchors_Tenants_TenantId",
                table: "certificate_blockchain_anchors",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_certificate_blockchain_anchors_user_certificates_CertificateId",
                table: "certificate_blockchain_anchors",
                column: "CertificateId",
                principalTable: "user_certificates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_certificate_tags_Tenants_TenantId",
                table: "certificate_tags",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_certificate_tags_certificates_CertificateId",
                table: "certificate_tags",
                column: "CertificateId",
                principalTable: "certificates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_certificate_tags_tag_proficiencies_TagId",
                table: "certificate_tags",
                column: "TagId",
                principalTable: "tag_proficiencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_certificate_tags_tag_proficiencies_TagProficiencyId",
                table: "certificate_tags",
                column: "TagProficiencyId",
                principalTable: "tag_proficiencies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_certificates_Products_ProductId",
                table: "certificates",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_certificates_Tenants_TenantId",
                table: "certificates",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_certificates_programs_ProgramId",
                table: "certificates",
                column: "ProgramId",
                principalTable: "programs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_ResourceMetadata_MetadataId",
                table: "Comment",
                column: "MetadataId",
                principalTable: "ResourceMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_Tenants_TenantId",
                table: "Comment",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CommentPermissions_Tenants_TenantId",
                table: "CommentPermissions",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CommentPermissions_Users_UserId",
                table: "CommentPermissions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_content_interactions_Tenants_TenantId",
                table: "content_interactions",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_content_interactions_program_contents_ContentId",
                table: "content_interactions",
                column: "ContentId",
                principalTable: "program_contents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_content_interactions_program_contents_ProgramContentId",
                table: "content_interactions",
                column: "ProgramContentId",
                principalTable: "program_contents",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_content_interactions_program_users_ProgramUserId",
                table: "content_interactions",
                column: "ProgramUserId",
                principalTable: "program_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ContentContentLicense_ContentLicenses_LicensesId",
                table: "ContentContentLicense",
                column: "LicensesId",
                principalTable: "ContentLicenses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ContentLicenses_ResourceMetadata_MetadataId",
                table: "ContentLicenses",
                column: "MetadataId",
                principalTable: "ResourceMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ContentLicenses_Tenants_TenantId",
                table: "ContentLicenses",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ContentTypePermissions_TenantPermissions_TenantPermissionId",
                table: "ContentTypePermissions",
                column: "TenantPermissionId",
                principalTable: "TenantPermissions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ContentTypePermissions_Tenants_TenantId",
                table: "ContentTypePermissions",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ContentTypePermissions_Users_UserId",
                table: "ContentTypePermissions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ContentTypePermissions_Users_UserId1",
                table: "ContentTypePermissions",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Credentials_Tenants_TenantId",
                table: "Credentials",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Credentials_Users_UserId",
                table: "Credentials",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_financial_transactions_Tenants_TenantId",
                table: "financial_transactions",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_financial_transactions_Users_FromUserId",
                table: "financial_transactions",
                column: "FromUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_financial_transactions_Users_ToUserId",
                table: "financial_transactions",
                column: "ToUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_financial_transactions_promo_codes_PromoCodeId",
                table: "financial_transactions",
                column: "PromoCodeId",
                principalTable: "promo_codes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_financial_transactions_promo_codes_PromoCodeId1",
                table: "financial_transactions",
                column: "PromoCodeId1",
                principalTable: "promo_codes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_financial_transactions_user_financial_methods_PaymentMethodId",
                table: "financial_transactions",
                column: "PaymentMethodId",
                principalTable: "user_financial_methods",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Jam_Tenants_TenantId",
                table: "Jam",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JamScore_ProjectJamSubmissions_ProjectJamSubmissionId",
                table: "JamScore",
                column: "ProjectJamSubmissionId",
                principalTable: "ProjectJamSubmissions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JamScore_Tenants_TenantId",
                table: "JamScore",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Languages_Tenants_TenantId",
                table: "Languages",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_product_pricing_Products_ProductId",
                table: "product_pricing",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_product_pricing_Tenants_TenantId",
                table: "product_pricing",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_product_programs_Products_ProductId",
                table: "product_programs",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_product_programs_Tenants_TenantId",
                table: "product_programs",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_product_programs_programs_ProgramId",
                table: "product_programs",
                column: "ProgramId",
                principalTable: "programs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_product_programs_programs_ProgramId1",
                table: "product_programs",
                column: "ProgramId1",
                principalTable: "programs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_product_subscription_plans_Products_ProductId",
                table: "product_subscription_plans",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_product_subscription_plans_Tenants_TenantId",
                table: "product_subscription_plans",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPermissions_Products_ResourceId",
                table: "ProductPermissions",
                column: "ResourceId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPermissions_Tenants_TenantId",
                table: "ProductPermissions",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductPermissions_Users_UserId",
                table: "ProductPermissions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_ResourceMetadata_MetadataId",
                table: "Products",
                column: "MetadataId",
                principalTable: "ResourceMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Tenants_TenantId",
                table: "Products",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Users_CreatorId",
                table: "Products",
                column: "CreatorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_program_contents_Tenants_TenantId",
                table: "program_contents",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_program_contents_programs_ProgramId",
                table: "program_contents",
                column: "ProgramId",
                principalTable: "programs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_program_feedback_submissions_Tenants_TenantId",
                table: "program_feedback_submissions",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_program_feedback_submissions_Users_UserId",
                table: "program_feedback_submissions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_program_feedback_submissions_program_users_ProgramUserId",
                table: "program_feedback_submissions",
                column: "ProgramUserId",
                principalTable: "program_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_program_feedback_submissions_programs_ProgramId",
                table: "program_feedback_submissions",
                column: "ProgramId",
                principalTable: "programs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_program_ratings_Tenants_TenantId",
                table: "program_ratings",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_program_ratings_Users_ModeratedBy",
                table: "program_ratings",
                column: "ModeratedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_program_ratings_Users_UserId",
                table: "program_ratings",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_program_ratings_program_users_ProgramUserId",
                table: "program_ratings",
                column: "ProgramUserId",
                principalTable: "program_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_program_ratings_programs_ProgramId",
                table: "program_ratings",
                column: "ProgramId",
                principalTable: "programs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_program_users_Tenants_TenantId",
                table: "program_users",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_program_users_Users_UserId",
                table: "program_users",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_program_users_programs_ProgramId",
                table: "program_users",
                column: "ProgramId",
                principalTable: "programs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_program_wishlists_Tenants_TenantId",
                table: "program_wishlists",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_program_wishlists_Users_UserId",
                table: "program_wishlists",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_program_wishlists_programs_ProgramId",
                table: "program_wishlists",
                column: "ProgramId",
                principalTable: "programs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramPermissions_Tenants_TenantId",
                table: "ProgramPermissions",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramPermissions_Users_UserId",
                table: "ProgramPermissions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProgramPermissions_programs_ResourceId",
                table: "ProgramPermissions",
                column: "ResourceId",
                principalTable: "programs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_programs_ResourceMetadata_MetadataId",
                table: "programs",
                column: "MetadataId",
                principalTable: "ResourceMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_programs_Tenants_TenantId",
                table: "programs",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectCategory_ResourceMetadata_MetadataId",
                table: "ProjectCategory",
                column: "MetadataId",
                principalTable: "ResourceMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectCategory_Tenants_TenantId",
                table: "ProjectCategory",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectCollaborators_Projects_ProjectId",
                table: "ProjectCollaborators",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectCollaborators_ResourceMetadata_MetadataId",
                table: "ProjectCollaborators",
                column: "MetadataId",
                principalTable: "ResourceMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectCollaborators_Tenants_TenantId",
                table: "ProjectCollaborators",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectCollaborators_Users_UserId",
                table: "ProjectCollaborators",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectFeedbacks_Projects_ProjectId",
                table: "ProjectFeedbacks",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectFeedbacks_ResourceMetadata_MetadataId",
                table: "ProjectFeedbacks",
                column: "MetadataId",
                principalTable: "ResourceMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectFeedbacks_Tenants_TenantId",
                table: "ProjectFeedbacks",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectFeedbacks_Users_UserId",
                table: "ProjectFeedbacks",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectFollowers_Projects_ProjectId",
                table: "ProjectFollowers",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectFollowers_ResourceMetadata_MetadataId",
                table: "ProjectFollowers",
                column: "MetadataId",
                principalTable: "ResourceMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectFollowers_Tenants_TenantId",
                table: "ProjectFollowers",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectFollowers_Users_UserId",
                table: "ProjectFollowers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectJamSubmissions_Projects_ProjectId",
                table: "ProjectJamSubmissions",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectJamSubmissions_ResourceMetadata_MetadataId",
                table: "ProjectJamSubmissions",
                column: "MetadataId",
                principalTable: "ResourceMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectJamSubmissions_Tenants_TenantId",
                table: "ProjectJamSubmissions",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectMetadata_Projects_ProjectId",
                table: "ProjectMetadata",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectPermissions_Projects_ResourceId",
                table: "ProjectPermissions",
                column: "ResourceId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectPermissions_Tenants_TenantId",
                table: "ProjectPermissions",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectPermissions_Users_UserId",
                table: "ProjectPermissions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectReleases_Projects_ProjectId",
                table: "ProjectReleases",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectReleases_ResourceMetadata_MetadataId",
                table: "ProjectReleases",
                column: "MetadataId",
                principalTable: "ResourceMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectReleases_Tenants_TenantId",
                table: "ProjectReleases",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_ResourceMetadata_MetadataId",
                table: "Projects",
                column: "MetadataId",
                principalTable: "ResourceMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Tenants_TenantId",
                table: "Projects",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Users_CreatedById",
                table: "Projects",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTeams_ResourceMetadata_MetadataId",
                table: "ProjectTeams",
                column: "MetadataId",
                principalTable: "ResourceMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTeams_Tenants_TenantId",
                table: "ProjectTeams",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectVersion_Tenants_TenantId",
                table: "ProjectVersion",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectVersion_Users_CreatedById",
                table: "ProjectVersion",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_promo_code_uses_Tenants_TenantId",
                table: "promo_code_uses",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_promo_code_uses_Users_UserId",
                table: "promo_code_uses",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_promo_code_uses_promo_codes_PromoCodeId",
                table: "promo_code_uses",
                column: "PromoCodeId",
                principalTable: "promo_codes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_promo_codes_Tenants_TenantId",
                table: "promo_codes",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_promo_codes_Users_CreatedBy",
                table: "promo_codes",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_Tenants_TenantId",
                table: "RefreshTokens",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ReputationActions_ReputationLevels_RequiredLevelId",
                table: "ReputationActions",
                column: "RequiredLevelId",
                principalTable: "ReputationLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ReputationActions_ResourceMetadata_MetadataId",
                table: "ReputationActions",
                column: "MetadataId",
                principalTable: "ResourceMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ReputationActions_Tenants_TenantId",
                table: "ReputationActions",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ReputationLevels_ResourceMetadata_MetadataId",
                table: "ReputationLevels",
                column: "MetadataId",
                principalTable: "ResourceMetadata",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ReputationLevels_Tenants_TenantId",
                table: "ReputationLevels",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceLocalizations_Tenants_TenantId",
                table: "ResourceLocalizations",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceLocalizations_UserProfiles_UserProfileId",
                table: "ResourceLocalizations",
                column: "UserProfileId",
                principalTable: "UserProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceLocalizations_UserReputationHistory_UserReputationHistoryId",
                table: "ResourceLocalizations",
                column: "UserReputationHistoryId",
                principalTable: "UserReputationHistory",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceLocalizations_UserReputations_UserReputationId",
                table: "ResourceLocalizations",
                column: "UserReputationId",
                principalTable: "UserReputations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceLocalizations_UserTenantReputations_UserTenantReputationId",
                table: "ResourceLocalizations",
                column: "UserTenantReputationId",
                principalTable: "UserTenantReputations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceMetadata_Tenants_TenantId",
                table: "ResourceMetadata",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ResourceMetadata_Tenants_TenantId",
                table: "ResourceMetadata");

            migrationBuilder.DropTable(
                name: "activity_grades");

            migrationBuilder.DropTable(
                name: "certificate_blockchain_anchors");

            migrationBuilder.DropTable(
                name: "certificate_tags");

            migrationBuilder.DropTable(
                name: "CommentPermissions");

            migrationBuilder.DropTable(
                name: "ContentContentLicense");

            migrationBuilder.DropTable(
                name: "ContentTypePermissions");

            migrationBuilder.DropTable(
                name: "Credentials");

            migrationBuilder.DropTable(
                name: "JamScore");

            migrationBuilder.DropTable(
                name: "product_pricing");

            migrationBuilder.DropTable(
                name: "product_programs");

            migrationBuilder.DropTable(
                name: "ProductPermissions");

            migrationBuilder.DropTable(
                name: "program_feedback_submissions");

            migrationBuilder.DropTable(
                name: "program_ratings");

            migrationBuilder.DropTable(
                name: "program_wishlists");

            migrationBuilder.DropTable(
                name: "ProgramPermissions");

            migrationBuilder.DropTable(
                name: "ProjectMetadata");

            migrationBuilder.DropTable(
                name: "ProjectPermissions");

            migrationBuilder.DropTable(
                name: "promo_code_uses");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "ResourceLocalizations");

            migrationBuilder.DropTable(
                name: "SessionRegistrations");

            migrationBuilder.DropTable(
                name: "SessionWaitlists");

            migrationBuilder.DropTable(
                name: "tag_relationships");

            migrationBuilder.DropTable(
                name: "TeamMember");

            migrationBuilder.DropTable(
                name: "TenantDomains");

            migrationBuilder.DropTable(
                name: "TenantUserGroupMemberships");

            migrationBuilder.DropTable(
                name: "TestingFeedback");

            migrationBuilder.DropTable(
                name: "TestingParticipants");

            migrationBuilder.DropTable(
                name: "user_kyc_verifications");

            migrationBuilder.DropTable(
                name: "user_products");

            migrationBuilder.DropTable(
                name: "content_interactions");

            migrationBuilder.DropTable(
                name: "user_certificates");

            migrationBuilder.DropTable(
                name: "tag_proficiencies");

            migrationBuilder.DropTable(
                name: "financial_transactions");

            migrationBuilder.DropTable(
                name: "Comment");

            migrationBuilder.DropTable(
                name: "ContentLicenses");

            migrationBuilder.DropTable(
                name: "Languages");

            migrationBuilder.DropTable(
                name: "ProjectCollaborators");

            migrationBuilder.DropTable(
                name: "ProjectFeedbacks");

            migrationBuilder.DropTable(
                name: "ProjectFollowers");

            migrationBuilder.DropTable(
                name: "ProjectJamSubmissions");

            migrationBuilder.DropTable(
                name: "ProjectReleases");

            migrationBuilder.DropTable(
                name: "ProjectTeams");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "UserReputationHistory");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "TenantUserGroups");

            migrationBuilder.DropTable(
                name: "TestingFeedbackForms");

            migrationBuilder.DropTable(
                name: "TestingSessions");

            migrationBuilder.DropTable(
                name: "user_subscriptions");

            migrationBuilder.DropTable(
                name: "program_contents");

            migrationBuilder.DropTable(
                name: "certificates");

            migrationBuilder.DropTable(
                name: "program_users");

            migrationBuilder.DropTable(
                name: "promo_codes");

            migrationBuilder.DropTable(
                name: "user_financial_methods");

            migrationBuilder.DropTable(
                name: "Jam");

            migrationBuilder.DropTable(
                name: "Team");

            migrationBuilder.DropTable(
                name: "ReputationActions");

            migrationBuilder.DropTable(
                name: "UserReputations");

            migrationBuilder.DropTable(
                name: "UserTenantReputations");

            migrationBuilder.DropTable(
                name: "TestingLocations");

            migrationBuilder.DropTable(
                name: "TestingRequests");

            migrationBuilder.DropTable(
                name: "product_subscription_plans");

            migrationBuilder.DropTable(
                name: "programs");

            migrationBuilder.DropTable(
                name: "ReputationLevels");

            migrationBuilder.DropTable(
                name: "TenantPermissions");

            migrationBuilder.DropTable(
                name: "ProjectVersion");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "ProjectCategory");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "ResourceMetadata");
        }
    }
}
