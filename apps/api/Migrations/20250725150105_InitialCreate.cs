using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.Migrations {
  /// <inheritdoc />
  public partial class InitialCreate : Migration {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) {
      migrationBuilder.CreateTable(
          name: "achievements",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
            Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
            IconUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
            Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
            Points = table.Column<int>(type: "integer", nullable: false),
            IsActive = table.Column<bool>(type: "boolean", nullable: false),
            IsSecret = table.Column<bool>(type: "boolean", nullable: false),
            IsRepeatable = table.Column<bool>(type: "boolean", nullable: false),
            Conditions = table.Column<string>(type: "jsonb", nullable: true),
            DisplayOrder = table.Column<int>(type: "integer", nullable: false),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
            IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_achievements", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "DiscountCodes",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
            Type = table.Column<int>(type: "integer", nullable: false),
            Value = table.Column<decimal>(type: "numeric", nullable: false),
            MinimumAmount = table.Column<decimal>(type: "numeric", nullable: true),
            MaximumDiscount = table.Column<decimal>(type: "numeric", nullable: true),
            ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            ValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            MaxUses = table.Column<int>(type: "integer", nullable: true),
            CurrentUses = table.Column<int>(type: "integer", nullable: false),
            IsActive = table.Column<bool>(type: "boolean", nullable: false),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
          },
          constraints: table => {
            table.PrimaryKey("PK_DiscountCodes", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "Payments",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            ProductId = table.Column<Guid>(type: "uuid", nullable: true),
            Amount = table.Column<decimal>(type: "numeric", nullable: false),
            Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
            Status = table.Column<int>(type: "integer", nullable: false),
            Method = table.Column<int>(type: "integer", nullable: false),
            Gateway = table.Column<int>(type: "integer", nullable: false),
            ProviderTransactionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
            PaymentIntentId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
            DiscountCodeId = table.Column<Guid>(type: "uuid", nullable: true),
            DiscountAmount = table.Column<decimal>(type: "numeric", nullable: false),
            FinalAmount = table.Column<decimal>(type: "numeric", nullable: false),
            ProcessingFee = table.Column<decimal>(type: "numeric", nullable: false),
            NetAmount = table.Column<decimal>(type: "numeric", nullable: false),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            Metadata = table.Column<string>(type: "text", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_Payments", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "Team",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Name = table.Column<string>(type: "text", nullable: false),
            Description = table.Column<string>(type: "text", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_Team", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "achievement_levels",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            AchievementId = table.Column<Guid>(type: "uuid", nullable: false),
            Level = table.Column<int>(type: "integer", nullable: false),
            Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
            Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
            RequiredProgress = table.Column<int>(type: "integer", nullable: false),
            Points = table.Column<int>(type: "integer", nullable: false),
            IconUrl = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
            Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
            IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_achievement_levels", x => x.Id);
            table.ForeignKey(
                      name: "FK_achievement_levels_achievements_AchievementId",
                      column: x => x.AchievementId,
                      principalTable: "achievements",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "achievement_prerequisites",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            AchievementId = table.Column<Guid>(type: "uuid", nullable: false),
            PrerequisiteAchievementId = table.Column<Guid>(type: "uuid", nullable: false),
            RequiresCompletion = table.Column<bool>(type: "boolean", nullable: false),
            MinimumLevel = table.Column<int>(type: "integer", nullable: true),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
            IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_achievement_prerequisites", x => x.Id);
            table.ForeignKey(
                      name: "FK_achievement_prerequisites_achievements_AchievementId",
                      column: x => x.AchievementId,
                      principalTable: "achievements",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
            table.ForeignKey(
                      name: "FK_achievement_prerequisites_achievements_PrerequisiteAchievem~",
                      column: x => x.PrerequisiteAchievementId,
                      principalTable: "achievements",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
          });

      migrationBuilder.CreateTable(
          name: "PaymentRefunds",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
            ExternalRefundId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            RefundAmount = table.Column<decimal>(type: "numeric", nullable: false),
            Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
            Status = table.Column<int>(type: "integer", nullable: false),
            ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
          },
          constraints: table => {
            table.PrimaryKey("PK_PaymentRefunds", x => x.Id);
            table.ForeignKey(
                      name: "FK_PaymentRefunds_Payments_PaymentId",
                      column: x => x.PaymentId,
                      principalTable: "Payments",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "achievement_progress",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            AchievementId = table.Column<Guid>(type: "uuid", nullable: false),
            CurrentProgress = table.Column<int>(type: "integer", nullable: false),
            TargetProgress = table.Column<int>(type: "integer", nullable: false),
            LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
            Context = table.Column<string>(type: "jsonb", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
            IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_achievement_progress", x => x.Id);
            table.ForeignKey(
                      name: "FK_achievement_progress_achievements_AchievementId",
                      column: x => x.AchievementId,
                      principalTable: "achievements",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "activity_grades",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            ContentInteractionId = table.Column<Guid>(type: "uuid", nullable: false),
            GraderProgramUserId = table.Column<Guid>(type: "uuid", nullable: false),
            Grade = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
            Feedback = table.Column<string>(type: "text", nullable: true),
            GradingDetails = table.Column<string>(type: "jsonb", nullable: true),
            GradedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            ProgramUserId = table.Column<Guid>(type: "uuid", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_activity_grades", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "certificate_blockchain_anchors",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            CertificateId = table.Column<Guid>(type: "uuid", nullable: false),
            BlockchainNetwork = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            TransactionHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
            BlockHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
            BlockNumber = table.Column<long>(type: "bigint", nullable: true),
            ContractAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            TokenId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
            Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
            AnchoredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_certificate_blockchain_anchors", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "certificate_tags",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            CertificateId = table.Column<Guid>(type: "uuid", nullable: false),
            TagId = table.Column<Guid>(type: "uuid", nullable: false),
            RelationshipType = table.Column<int>(type: "integer", nullable: false),
            TagProficiencyId = table.Column<Guid>(type: "uuid", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_certificate_tags", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "certificates",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Description = table.Column<string>(type: "text", nullable: false),
            Type = table.Column<int>(type: "integer", nullable: false),
            ProgramId = table.Column<Guid>(type: "uuid", nullable: true),
            ProductId = table.Column<Guid>(type: "uuid", nullable: true),
            CompletionPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
            MinimumGrade = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
            RequiresFeedback = table.Column<bool>(type: "boolean", nullable: false),
            RequiresRating = table.Column<bool>(type: "boolean", nullable: false),
            MinimumRating = table.Column<decimal>(type: "numeric(2,1)", nullable: true),
            ValidityDays = table.Column<int>(type: "integer", nullable: true),
            VerificationMethod = table.Column<int>(type: "integer", nullable: false),
            CertificateTemplate = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            IsActive = table.Column<bool>(type: "boolean", nullable: false),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_certificates", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "Comment",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            Visibility = table.Column<int>(type: "integer", nullable: false),
            MetadataId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            Content = table.Column<string>(type: "text", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_Comment", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "CommentPermissions",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            PermissionFlags1 = table.Column<long>(type: "bigint", nullable: false),
            PermissionFlags2 = table.Column<long>(type: "bigint", nullable: false),
            UserId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            ResourceId = table.Column<Guid>(type: "uuid", nullable: false)
          },
          constraints: table => {
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
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            ProgramUserId = table.Column<Guid>(type: "uuid", nullable: false),
            ContentId = table.Column<Guid>(type: "uuid", nullable: false),
            Status = table.Column<int>(type: "integer", nullable: false),
            SubmissionData = table.Column<string>(type: "jsonb", nullable: true),
            CompletionPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
            TimeSpentMinutes = table.Column<int>(type: "integer", nullable: true),
            FirstAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            ProgramContentId = table.Column<Guid>(type: "uuid", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_content_interactions", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "content_progress",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            ContentId = table.Column<Guid>(type: "uuid", nullable: false),
            ProgramEnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
            CompletionStatus = table.Column<int>(type: "integer", nullable: false),
            ProgressPercentage = table.Column<decimal>(type: "numeric", nullable: false),
            FirstAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TimeSpentSeconds = table.Column<int>(type: "integer", nullable: false),
            Score = table.Column<decimal>(type: "numeric", nullable: true),
            MaxScore = table.Column<decimal>(type: "numeric", nullable: true),
            Attempts = table.Column<int>(type: "integer", nullable: false),
            ProgressData = table.Column<string>(type: "jsonb", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_content_progress", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "content_reports",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            ReporterId = table.Column<Guid>(type: "uuid", nullable: false),
            ReportType = table.Column<int>(type: "integer", nullable: false),
            Subject = table.Column<int>(type: "integer", nullable: false),
            ContentId = table.Column<Guid>(type: "uuid", nullable: true),
            ReportedUserId = table.Column<Guid>(type: "uuid", nullable: true),
            PeerReviewId = table.Column<Guid>(type: "uuid", nullable: true),
            Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            Description = table.Column<string>(type: "text", nullable: false),
            Evidence = table.Column<string>(type: "jsonb", nullable: true),
            Status = table.Column<int>(type: "integer", nullable: false),
            Priority = table.Column<int>(type: "integer", nullable: false),
            ModeratorId = table.Column<Guid>(type: "uuid", nullable: true),
            AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            ResolutionNotes = table.Column<string>(type: "text", nullable: true),
            ActionTaken = table.Column<string>(type: "text", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_content_reports", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "ContentContentLicense",
          columns: table => new {
            ContentsId = table.Column<Guid>(type: "uuid", nullable: false),
            LicensesId = table.Column<Guid>(type: "uuid", nullable: false)
          },
          constraints: table => {
            table.PrimaryKey("PK_ContentContentLicense", x => new { x.ContentsId, x.LicensesId });
          });

      migrationBuilder.CreateTable(
          name: "ContentLicenses",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            Visibility = table.Column<int>(type: "integer", nullable: false),
            MetadataId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_ContentLicenses", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "ContentTypePermissions",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            TenantPermissionId = table.Column<Guid>(type: "uuid", nullable: true),
            UserId1 = table.Column<Guid>(type: "uuid", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            PermissionFlags1 = table.Column<long>(type: "bigint", nullable: false),
            PermissionFlags2 = table.Column<long>(type: "bigint", nullable: false),
            UserId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_ContentTypePermissions", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "Credentials",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
            Value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
            Metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            IsActive = table.Column<bool>(type: "boolean", nullable: false),
            LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_Credentials", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "financial_transactions",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            FromUserId = table.Column<Guid>(type: "uuid", nullable: true),
            ToUserId = table.Column<Guid>(type: "uuid", nullable: true),
            Type = table.Column<int>(type: "integer", nullable: false),
            Amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
            Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
            Status = table.Column<int>(type: "integer", nullable: false),
            ExternalTransactionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
            PaymentMethodId = table.Column<Guid>(type: "uuid", nullable: true),
            PromoCodeId = table.Column<Guid>(type: "uuid", nullable: true),
            PlatformFee = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
            ProcessorFee = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
            NetAmount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
            Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            Metadata = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
            ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            Error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            PromoCodeId1 = table.Column<Guid>(type: "uuid", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_financial_transactions", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "Jam",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Theme = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            Description = table.Column<string>(type: "text", nullable: true),
            Rules = table.Column<string>(type: "text", nullable: true),
            SubmissionCriteria = table.Column<string>(type: "text", nullable: true),
            StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            VotingEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            MaxParticipants = table.Column<int>(type: "integer", nullable: true),
            ParticipantCount = table.Column<int>(type: "integer", nullable: false),
            Status = table.Column<int>(type: "integer", nullable: false),
            CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_Jam", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "JamScore",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
            CriteriaId = table.Column<Guid>(type: "uuid", nullable: false),
            JudgeUserId = table.Column<Guid>(type: "uuid", nullable: false),
            Score = table.Column<int>(type: "integer", nullable: false),
            Feedback = table.Column<string>(type: "text", nullable: true),
            ProjectJamSubmissionId = table.Column<Guid>(type: "uuid", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_JamScore", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "Languages",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
            Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
            IsActive = table.Column<bool>(type: "boolean", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_Languages", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "peer_reviews",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            ContentInteractionId = table.Column<Guid>(type: "uuid", nullable: false),
            ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
            RevieweeId = table.Column<Guid>(type: "uuid", nullable: false),
            Status = table.Column<int>(type: "integer", nullable: false),
            AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            Grade = table.Column<decimal>(type: "numeric", nullable: true),
            Feedback = table.Column<string>(type: "text", nullable: true),
            ReviewData = table.Column<string>(type: "jsonb", nullable: true),
            IsAccepted = table.Column<bool>(type: "boolean", nullable: true),
            AcceptanceReason = table.Column<string>(type: "text", nullable: true),
            ResponseAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            ReviewQuality = table.Column<int>(type: "integer", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_peer_reviews", x => x.Id);
            table.ForeignKey(
                      name: "FK_peer_reviews_content_interactions_ContentInteractionId",
                      column: x => x.ContentInteractionId,
                      principalTable: "content_interactions",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "post_comments",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            PostId = table.Column<Guid>(type: "uuid", nullable: false),
            AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
            Content = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
            ParentCommentId = table.Column<Guid>(type: "uuid", nullable: true),
            LikesCount = table.Column<int>(type: "integer", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_post_comments", x => x.Id);
            table.ForeignKey(
                      name: "FK_post_comments_post_comments_ParentCommentId",
                      column: x => x.ParentCommentId,
                      principalTable: "post_comments",
                      principalColumn: "Id");
          });

      migrationBuilder.CreateTable(
          name: "post_content_references",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            PostId = table.Column<Guid>(type: "uuid", nullable: false),
            ReferencedResourceId = table.Column<Guid>(type: "uuid", nullable: false),
            ReferenceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
            Context = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_post_content_references", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "post_followers",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            PostId = table.Column<Guid>(type: "uuid", nullable: false),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            NotifyOnComments = table.Column<bool>(type: "boolean", nullable: false),
            NotifyOnLikes = table.Column<bool>(type: "boolean", nullable: false),
            NotifyOnShares = table.Column<bool>(type: "boolean", nullable: false),
            NotifyOnUpdates = table.Column<bool>(type: "boolean", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_post_followers", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "post_likes",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            PostId = table.Column<Guid>(type: "uuid", nullable: false),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            ReactionType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_post_likes", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "post_statistics",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            PostId = table.Column<Guid>(type: "uuid", nullable: false),
            ViewsCount = table.Column<int>(type: "integer", nullable: false),
            UniqueViewersCount = table.Column<int>(type: "integer", nullable: false),
            ExternalSharesCount = table.Column<int>(type: "integer", nullable: false),
            AverageEngagementTime = table.Column<double>(type: "double precision", nullable: false),
            EngagementScore = table.Column<double>(type: "double precision", nullable: false),
            TrendingScore = table.Column<double>(type: "double precision", nullable: false),
            LastCalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_post_statistics", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "post_tag_assignments",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            PostId = table.Column<Guid>(type: "uuid", nullable: false),
            TagId = table.Column<Guid>(type: "uuid", nullable: false),
            Order = table.Column<int>(type: "integer", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_post_tag_assignments", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "post_tags",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
            DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
            Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
            UsageCount = table.Column<int>(type: "integer", nullable: false),
            IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_post_tags", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "post_views",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            PostId = table.Column<Guid>(type: "uuid", nullable: false),
            UserId = table.Column<Guid>(type: "uuid", nullable: true),
            ViewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
            UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            Referrer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            DurationSeconds = table.Column<int>(type: "integer", nullable: false),
            IsEngaged = table.Column<bool>(type: "boolean", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_post_views", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "posts",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            Visibility = table.Column<int>(type: "integer", nullable: false),
            MetadataId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            Slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Status = table.Column<int>(type: "integer", nullable: false),
            PostType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
            AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
            IsSystemGenerated = table.Column<bool>(type: "boolean", nullable: false),
            IsPinned = table.Column<bool>(type: "boolean", nullable: false),
            LikesCount = table.Column<int>(type: "integer", nullable: false),
            CommentsCount = table.Column<int>(type: "integer", nullable: false),
            SharesCount = table.Column<int>(type: "integer", nullable: false),
            RichContent = table.Column<string>(type: "jsonb", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_posts", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "product_pricing",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            ProductId = table.Column<Guid>(type: "uuid", nullable: false),
            Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            BasePrice = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
            SalePrice = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
            Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
            SaleStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            SaleEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            IsDefault = table.Column<bool>(type: "boolean", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_product_pricing", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "product_programs",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            ProductId = table.Column<Guid>(type: "uuid", nullable: false),
            ProgramId = table.Column<Guid>(type: "uuid", nullable: false),
            SortOrder = table.Column<int>(type: "integer", nullable: false),
            ProgramId1 = table.Column<Guid>(type: "uuid", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_product_programs", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "product_subscription_plans",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
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
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_product_subscription_plans", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "ProductPermissions",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            PermissionFlags1 = table.Column<long>(type: "bigint", nullable: false),
            PermissionFlags2 = table.Column<long>(type: "bigint", nullable: false),
            UserId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            ResourceId = table.Column<Guid>(type: "uuid", nullable: false)
          },
          constraints: table => {
            table.PrimaryKey("PK_ProductPermissions", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "Products",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            Visibility = table.Column<int>(type: "integer", nullable: false),
            MetadataId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            Slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Status = table.Column<int>(type: "integer", nullable: false),
            Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
            ShortDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            Type = table.Column<int>(type: "integer", nullable: false),
            IsBundle = table.Column<bool>(type: "boolean", nullable: false),
            CreatorId = table.Column<Guid>(type: "uuid", nullable: false),
            BundleItems = table.Column<string>(type: "jsonb", nullable: true),
            ReferralCommissionPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
            MaxAffiliateDiscount = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
            AffiliateCommissionPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_Products", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "program_contents",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            ProgramId = table.Column<Guid>(type: "uuid", nullable: false),
            ParentId = table.Column<Guid>(type: "uuid", nullable: true),
            Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Description = table.Column<string>(type: "text", nullable: false),
            Type = table.Column<int>(type: "integer", nullable: false),
            Body = table.Column<string>(type: "jsonb", nullable: false),
            SortOrder = table.Column<int>(type: "integer", nullable: false),
            IsRequired = table.Column<bool>(type: "boolean", nullable: false),
            GradingMethod = table.Column<int>(type: "integer", nullable: true),
            MaxPoints = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
            EstimatedMinutes = table.Column<int>(type: "integer", nullable: true),
            Visibility = table.Column<int>(type: "integer", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_program_contents", x => x.Id);
            table.ForeignKey(
                      name: "FK_program_contents_program_contents_ParentId",
                      column: x => x.ParentId,
                      principalTable: "program_contents",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
          });

      migrationBuilder.CreateTable(
          name: "program_enrollments",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            ProgramId = table.Column<Guid>(type: "uuid", nullable: false),
            EnrollmentStatus = table.Column<int>(type: "integer", nullable: false),
            EnrollmentSource = table.Column<int>(type: "integer", nullable: false),
            EnrolledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            CompletionStatus = table.Column<int>(type: "integer", nullable: false),
            ProgressPercentage = table.Column<decimal>(type: "numeric", nullable: false),
            CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            FinalGrade = table.Column<decimal>(type: "numeric", nullable: true),
            CertificateIssued = table.Column<bool>(type: "boolean", nullable: false),
            CertificateIssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            CertificateId = table.Column<Guid>(type: "uuid", nullable: true),
            Metadata = table.Column<string>(type: "jsonb", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_program_enrollments", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "program_feedback_submissions",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            ProgramId = table.Column<Guid>(type: "uuid", nullable: false),
            ProductId = table.Column<Guid>(type: "uuid", nullable: true),
            ProgramUserId = table.Column<Guid>(type: "uuid", nullable: false),
            FeedbackData = table.Column<string>(type: "jsonb", nullable: false),
            OverallRating = table.Column<decimal>(type: "numeric(2,1)", nullable: true),
            Comments = table.Column<string>(type: "text", nullable: true),
            WouldRecommend = table.Column<bool>(type: "boolean", nullable: true),
            SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_program_feedback_submissions", x => x.Id);
            table.ForeignKey(
                      name: "FK_program_feedback_submissions_Products_ProductId",
                      column: x => x.ProductId,
                      principalTable: "Products",
                      principalColumn: "Id");
          });

      migrationBuilder.CreateTable(
          name: "program_ratings",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            ProgramId = table.Column<Guid>(type: "uuid", nullable: false),
            UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
            Rating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
            Review = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            IsVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
            IsFeatured = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
            HelpfulVotes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
            UnhelpfulVotes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
            ProgramUserId = table.Column<Guid>(type: "uuid", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_program_ratings", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "program_users",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            ProgramId = table.Column<Guid>(type: "uuid", nullable: false),
            IsActive = table.Column<bool>(type: "boolean", nullable: false),
            JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            CompletionPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
            FinalGrade = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
            StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_program_users", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "program_wishlists",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            ProgramId = table.Column<Guid>(type: "uuid", nullable: false),
            AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            Notes = table.Column<string>(type: "text", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_program_wishlists", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "ProgramPermissions",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            PermissionFlags1 = table.Column<long>(type: "bigint", nullable: false),
            PermissionFlags2 = table.Column<long>(type: "bigint", nullable: false),
            UserId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            ResourceId = table.Column<Guid>(type: "uuid", nullable: false)
          },
          constraints: table => {
            table.PrimaryKey("PK_ProgramPermissions", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "programs",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            Visibility = table.Column<int>(type: "integer", nullable: false),
            MetadataId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            Slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Status = table.Column<int>(type: "integer", nullable: false),
            Thumbnail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            VideoShowcaseUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            EstimatedHours = table.Column<float>(type: "real", nullable: true),
            EnrollmentStatus = table.Column<int>(type: "integer", nullable: false),
            MaxEnrollments = table.Column<int>(type: "integer", nullable: true),
            EnrollmentDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            Category = table.Column<int>(type: "integer", nullable: false),
            Difficulty = table.Column<int>(type: "integer", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_programs", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "ProjectCategory",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            Visibility = table.Column<int>(type: "integer", nullable: false),
            MetadataId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_ProjectCategory", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "ProjectCollaborators",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            Visibility = table.Column<int>(type: "integer", nullable: false),
            MetadataId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            Role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            Permissions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
            IsActive = table.Column<bool>(type: "boolean", nullable: false),
            JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            LeftAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_ProjectCollaborators", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "ProjectFeedbacks",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            Visibility = table.Column<int>(type: "integer", nullable: false),
            MetadataId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            Rating = table.Column<int>(type: "integer", nullable: false),
            Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            Categories = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
            IsVerified = table.Column<bool>(type: "boolean", nullable: false),
            Status = table.Column<int>(type: "integer", nullable: false),
            HelpfulVotes = table.Column<int>(type: "integer", nullable: false),
            TotalVotes = table.Column<int>(type: "integer", nullable: false),
            Platform = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
            ProjectVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_ProjectFeedbacks", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "ProjectFollowers",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            Visibility = table.Column<int>(type: "integer", nullable: false),
            MetadataId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            FollowedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            NotificationSettings = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
            EmailNotifications = table.Column<bool>(type: "boolean", nullable: false),
            PushNotifications = table.Column<bool>(type: "boolean", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_ProjectFollowers", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "ProjectJamSubmissions",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            Visibility = table.Column<int>(type: "integer", nullable: false),
            MetadataId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
            JamId = table.Column<Guid>(type: "uuid", nullable: false),
            SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            IsEligible = table.Column<bool>(type: "boolean", nullable: false),
            SubmissionNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            FinalScore = table.Column<decimal>(type: "numeric", nullable: true),
            Ranking = table.Column<int>(type: "integer", nullable: true),
            HasAward = table.Column<bool>(type: "boolean", nullable: false),
            AwardDetails = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
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
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
            ViewCount = table.Column<int>(type: "integer", nullable: false),
            DownloadCount = table.Column<int>(type: "integer", nullable: false),
            FollowerCount = table.Column<int>(type: "integer", nullable: false)
          },
          constraints: table => {
            table.PrimaryKey("PK_ProjectMetadata", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "ProjectPermissions",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            PermissionFlags1 = table.Column<long>(type: "bigint", nullable: false),
            PermissionFlags2 = table.Column<long>(type: "bigint", nullable: false),
            UserId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            ResourceId = table.Column<Guid>(type: "uuid", nullable: false)
          },
          constraints: table => {
            table.PrimaryKey("PK_ProjectPermissions", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "ProjectReleases",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            Visibility = table.Column<int>(type: "integer", nullable: false),
            MetadataId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
            ReleaseVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
            ReleasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            IsLatest = table.Column<bool>(type: "boolean", nullable: false),
            IsPrerelease = table.Column<bool>(type: "boolean", nullable: false),
            DownloadUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            FileSize = table.Column<long>(type: "bigint", nullable: true),
            DownloadCount = table.Column<int>(type: "integer", nullable: false),
            ReleaseNotes = table.Column<string>(type: "text", nullable: true),
            Checksum = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
            SystemRequirements = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
            SupportedPlatforms = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            ReleaseType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
            Status = table.Column<int>(type: "integer", nullable: false),
            BuildNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
            ReleaseMetadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_ProjectReleases", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "Projects",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            Visibility = table.Column<int>(type: "integer", nullable: false),
            MetadataId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            Slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Status = table.Column<int>(type: "integer", nullable: false),
            ShortDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            Type = table.Column<int>(type: "integer", nullable: false),
            DevelopmentStatus = table.Column<int>(type: "integer", nullable: false),
            CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
            WebsiteUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            RepositoryUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            SocialLinks = table.Column<string>(type: "text", nullable: true),
            DownloadUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            Tags = table.Column<string>(type: "text", nullable: true),
            CreatedById = table.Column<Guid>(type: "uuid", nullable: true),
            ProjectCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
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
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            Visibility = table.Column<int>(type: "integer", nullable: false),
            MetadataId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
            TeamId = table.Column<Guid>(type: "uuid", nullable: false),
            Role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            IsActive = table.Column<bool>(type: "boolean", nullable: false),
            Permissions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
            Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
            ContributionPercentage = table.Column<decimal>(type: "numeric", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
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
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
            VersionNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
            ReleaseNotes = table.Column<string>(type: "text", nullable: true),
            Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
            DownloadCount = table.Column<int>(type: "integer", nullable: false),
            CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
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
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            PromoCodeId = table.Column<Guid>(type: "uuid", nullable: false),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            FinancialTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
            DiscountApplied = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_promo_code_uses", x => x.Id);
            table.ForeignKey(
                      name: "FK_promo_code_uses_financial_transactions_FinancialTransaction~",
                      column: x => x.FinancialTransactionId,
                      principalTable: "financial_transactions",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "promo_codes",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
            Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Description = table.Column<string>(type: "text", nullable: true),
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
            ProductId = table.Column<Guid>(type: "uuid", nullable: true),
            CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
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
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            Token = table.Column<string>(type: "text", nullable: false),
            ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
            RevokedByIp = table.Column<string>(type: "text", nullable: true),
            RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            ReplacedByToken = table.Column<string>(type: "text", nullable: true),
            CreatedByIp = table.Column<string>(type: "text", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_RefreshTokens", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "ReputationActions",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            Visibility = table.Column<int>(type: "integer", nullable: false),
            MetadataId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            ActionType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
            Points = table.Column<int>(type: "integer", nullable: false),
            DailyLimit = table.Column<int>(type: "integer", nullable: true),
            TotalLimit = table.Column<int>(type: "integer", nullable: true),
            IsActive = table.Column<bool>(type: "boolean", nullable: false),
            RequiredLevelId = table.Column<Guid>(type: "uuid", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_ReputationActions", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "ReputationLevels",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            Visibility = table.Column<int>(type: "integer", nullable: false),
            MetadataId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
            MinimumScore = table.Column<int>(type: "integer", nullable: false),
            MaximumScore = table.Column<int>(type: "integer", nullable: true),
            Color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
            Icon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
            SortOrder = table.Column<int>(type: "integer", nullable: false),
            IsActive = table.Column<bool>(type: "boolean", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_ReputationLevels", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "ResourceLocalizations",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            ResourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
            FieldName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            ResourceId = table.Column<Guid>(type: "uuid", nullable: false),
            Content = table.Column<string>(type: "text", nullable: false),
            IsDefault = table.Column<bool>(type: "boolean", nullable: false),
            Status = table.Column<int>(type: "integer", nullable: false),
            CommentId = table.Column<Guid>(type: "uuid", nullable: true),
            ContentId = table.Column<Guid>(type: "uuid", nullable: true),
            ContentLicenseId = table.Column<Guid>(type: "uuid", nullable: true),
            ProjectCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
            ProjectCollaboratorId = table.Column<Guid>(type: "uuid", nullable: true),
            ProjectFeedbackId = table.Column<Guid>(type: "uuid", nullable: true),
            ProjectFollowerId = table.Column<Guid>(type: "uuid", nullable: true),
            ProjectJamSubmissionId = table.Column<Guid>(type: "uuid", nullable: true),
            ProjectReleaseId = table.Column<Guid>(type: "uuid", nullable: true),
            ProjectTeamId = table.Column<Guid>(type: "uuid", nullable: true),
            ReputationActionId = table.Column<Guid>(type: "uuid", nullable: true),
            ReputationTierId = table.Column<Guid>(type: "uuid", nullable: true),
            UserProfileId = table.Column<Guid>(type: "uuid", nullable: true),
            UserReputationHistoryId = table.Column<Guid>(type: "uuid", nullable: true),
            UserReputationId = table.Column<Guid>(type: "uuid", nullable: true),
            UserTenantReputationId = table.Column<Guid>(type: "uuid", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
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
                      name: "FK_ResourceLocalizations_ProjectCollaborators_ProjectCollabora~",
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
                      name: "FK_ResourceLocalizations_ProjectJamSubmissions_ProjectJamSubmi~",
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
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            ResourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            AdditionalData = table.Column<string>(type: "jsonb", nullable: true),
            Tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            SeoMetadata = table.Column<string>(type: "jsonb", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_ResourceMetadata", x => x.Id);
          });

      migrationBuilder.CreateTable(
          name: "Tenants",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            Visibility = table.Column<int>(type: "integer", nullable: false),
            MetadataId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            IsActive = table.Column<bool>(type: "boolean", nullable: false),
            Slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
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
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            Type = table.Column<int>(type: "integer", nullable: false),
            ProficiencyLevel = table.Column<int>(type: "integer", nullable: false),
            Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
            Icon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
            IsActive = table.Column<bool>(type: "boolean", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_tag_proficiencies", x => x.Id);
            table.ForeignKey(
                      name: "FK_tag_proficiencies_Tenants_TenantId",
                      column: x => x.TenantId,
                      principalTable: "Tenants",
                      principalColumn: "Id");
          });

      migrationBuilder.CreateTable(
          name: "tags",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            Type = table.Column<int>(type: "integer", nullable: false),
            Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
            Icon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
            IsActive = table.Column<bool>(type: "boolean", nullable: false),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
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
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            TeamId = table.Column<Guid>(type: "uuid", nullable: false),
            UserId = table.Column<string>(type: "text", nullable: false),
            Role = table.Column<int>(type: "integer", nullable: false),
            InvitedBy = table.Column<string>(type: "text", nullable: true),
            Status = table.Column<int>(type: "integer", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
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
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: false),
            ParentGroupId = table.Column<Guid>(type: "uuid", nullable: true),
            IsActive = table.Column<bool>(type: "boolean", nullable: false),
            IsDefault = table.Column<bool>(type: "boolean", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
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
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            TestingRequestId = table.Column<Guid>(type: "uuid", nullable: false),
            FormSchema = table.Column<string>(type: "text", nullable: false),
            IsForOnline = table.Column<bool>(type: "boolean", nullable: false),
            IsForSessions = table.Column<bool>(type: "boolean", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_TestingFeedbackForms", x => x.Id);
            table.ForeignKey(
                      name: "FK_TestingFeedbackForms_Tenants_TenantId",
                      column: x => x.TenantId,
                      principalTable: "Tenants",
                      principalColumn: "Id");
          });

      migrationBuilder.CreateTable(
          name: "TestingLocations",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Description = table.Column<string>(type: "text", nullable: true),
            Address = table.Column<string>(type: "text", nullable: true),
            MaxTestersCapacity = table.Column<int>(type: "integer", nullable: false),
            MaxProjectsCapacity = table.Column<int>(type: "integer", nullable: false),
            EquipmentAvailable = table.Column<string>(type: "text", nullable: true),
            Status = table.Column<int>(type: "integer", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_TestingLocations", x => x.Id);
            table.ForeignKey(
                      name: "FK_TestingLocations_Tenants_TenantId",
                      column: x => x.TenantId,
                      principalTable: "Tenants",
                      principalColumn: "Id");
          });

      migrationBuilder.CreateTable(
          name: "UserProfiles",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            Visibility = table.Column<int>(type: "integer", nullable: false),
            MetadataId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            GivenName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
            FamilyName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
            DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
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
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            IsActive = table.Column<bool>(type: "boolean", nullable: false),
            Balance = table.Column<decimal>(type: "numeric(18,8)", nullable: false, defaultValue: 0m),
            AvailableBalance = table.Column<decimal>(type: "numeric(18,8)", nullable: false, defaultValue: 0m),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_Users", x => x.Id);
            table.ForeignKey(
                      name: "FK_Users_Tenants_TenantId",
                      column: x => x.TenantId,
                      principalTable: "Tenants",
                      principalColumn: "Id");
          });

      migrationBuilder.CreateTable(
          name: "tag_relationships",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            SourceId = table.Column<Guid>(type: "uuid", nullable: false),
            TargetId = table.Column<Guid>(type: "uuid", nullable: false),
            Type = table.Column<int>(type: "integer", nullable: false),
            Weight = table.Column<decimal>(type: "numeric(3,2)", nullable: true),
            Metadata = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
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
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            TopLevelDomain = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Subdomain = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
            IsMainDomain = table.Column<bool>(type: "boolean", nullable: false),
            IsSecondaryDomain = table.Column<bool>(type: "boolean", nullable: false),
            TenantId = table.Column<Guid>(type: "uuid", nullable: false),
            UserGroupId = table.Column<Guid>(type: "uuid", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
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
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            UserId1 = table.Column<Guid>(type: "uuid", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            PermissionFlags1 = table.Column<long>(type: "bigint", nullable: false),
            PermissionFlags2 = table.Column<long>(type: "bigint", nullable: false),
            UserId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
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
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            UserGroupId = table.Column<Guid>(type: "uuid", nullable: false),
            JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            IsAutoAssigned = table.Column<bool>(type: "boolean", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
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
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            ProjectVersionId = table.Column<Guid>(type: "uuid", nullable: false),
            Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Description = table.Column<string>(type: "text", nullable: true),
            DownloadUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
            InstructionsType = table.Column<int>(type: "integer", nullable: false),
            InstructionsContent = table.Column<string>(type: "text", nullable: true),
            InstructionsUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            InstructionsFileId = table.Column<Guid>(type: "uuid", nullable: true),
            FeedbackFormContent = table.Column<string>(type: "text", nullable: true),
            MaxTesters = table.Column<int>(type: "integer", nullable: true),
            CurrentTesterCount = table.Column<int>(type: "integer", nullable: false),
            StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            Status = table.Column<int>(type: "integer", nullable: false),
            CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
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
          name: "user_achievements",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            AchievementId = table.Column<Guid>(type: "uuid", nullable: false),
            EarnedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            Level = table.Column<int>(type: "integer", nullable: true),
            Progress = table.Column<int>(type: "integer", nullable: false),
            MaxProgress = table.Column<int>(type: "integer", nullable: false),
            IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
            IsNotified = table.Column<bool>(type: "boolean", nullable: false),
            Context = table.Column<string>(type: "jsonb", nullable: true),
            PointsEarned = table.Column<int>(type: "integer", nullable: false),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            EarnCount = table.Column<int>(type: "integer", nullable: false),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
            IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_user_achievements", x => x.Id);
            table.ForeignKey(
                      name: "FK_user_achievements_Users_UserId",
                      column: x => x.UserId,
                      principalTable: "Users",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
            table.ForeignKey(
                      name: "FK_user_achievements_achievements_AchievementId",
                      column: x => x.AchievementId,
                      principalTable: "achievements",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "user_certificates",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            CertificateId = table.Column<Guid>(type: "uuid", nullable: false),
            ProgramId = table.Column<Guid>(type: "uuid", nullable: true),
            ProductId = table.Column<Guid>(type: "uuid", nullable: true),
            ProgramUserId = table.Column<Guid>(type: "uuid", nullable: true),
            VerificationCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            FinalGrade = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
            Metadata = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
            Status = table.Column<int>(type: "integer", nullable: false),
            IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            RevocationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
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
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            Type = table.Column<int>(type: "integer", nullable: false),
            Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            ExternalId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
            LastFour = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
            ExpiryMonth = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
            ExpiryYear = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
            Brand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
            Status = table.Column<int>(type: "integer", nullable: false),
            IsDefault = table.Column<bool>(type: "boolean", nullable: false),
            IsActive = table.Column<bool>(type: "boolean", nullable: false),
            DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
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
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            Provider = table.Column<int>(type: "integer", nullable: false),
            Status = table.Column<int>(type: "integer", nullable: false),
            ExternalVerificationId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
            VerificationLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
            DocumentTypes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            DocumentCountry = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
            SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
            ProviderData = table.Column<string>(type: "jsonb", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
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
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            SubscriptionPlanId = table.Column<Guid>(type: "uuid", nullable: false),
            Status = table.Column<int>(type: "integer", nullable: false),
            ExternalSubscriptionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
            CurrentPeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            CurrentPeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            CanceledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            EndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TrialEndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            LastPaymentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            NextBillingAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            ProductSubscriptionPlanId = table.Column<Guid>(type: "uuid", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
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
                      name: "FK_user_subscriptions_product_subscription_plans_ProductSubscr~",
                      column: x => x.ProductSubscriptionPlanId,
                      principalTable: "product_subscription_plans",
                      principalColumn: "Id");
            table.ForeignKey(
                      name: "FK_user_subscriptions_product_subscription_plans_SubscriptionP~",
                      column: x => x.SubscriptionPlanId,
                      principalTable: "product_subscription_plans",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
          });

      migrationBuilder.CreateTable(
          name: "UserReputations",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            Visibility = table.Column<int>(type: "integer", nullable: false),
            MetadataId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            Score = table.Column<int>(type: "integer", nullable: false),
            CurrentLevelId = table.Column<Guid>(type: "uuid", nullable: true),
            LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            LastLevelCalculation = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            PositiveChanges = table.Column<int>(type: "integer", nullable: false),
            NegativeChanges = table.Column<int>(type: "integer", nullable: false),
            ReputationTierId = table.Column<Guid>(type: "uuid", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
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
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            Visibility = table.Column<int>(type: "integer", nullable: false),
            MetadataId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantPermissionId = table.Column<Guid>(type: "uuid", nullable: false),
            Score = table.Column<int>(type: "integer", nullable: false),
            CurrentLevelId = table.Column<Guid>(type: "uuid", nullable: true),
            LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            LastLevelCalculation = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            PositiveChanges = table.Column<int>(type: "integer", nullable: false),
            NegativeChanges = table.Column<int>(type: "integer", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
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
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            TestingRequestId = table.Column<Guid>(type: "uuid", nullable: false),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            InstructionsAcknowledged = table.Column<bool>(type: "boolean", nullable: false),
            InstructionsAcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
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
          name: "TestingRequestPermissions",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            PermissionFlags1 = table.Column<long>(type: "bigint", nullable: false),
            PermissionFlags2 = table.Column<long>(type: "bigint", nullable: false),
            UserId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            ResourceId = table.Column<Guid>(type: "uuid", nullable: false)
          },
          constraints: table => {
            table.PrimaryKey("PK_TestingRequestPermissions", x => x.Id);
            table.ForeignKey(
                      name: "FK_TestingRequestPermissions_Tenants_TenantId",
                      column: x => x.TenantId,
                      principalTable: "Tenants",
                      principalColumn: "Id");
            table.ForeignKey(
                      name: "FK_TestingRequestPermissions_TestingRequests_ResourceId",
                      column: x => x.ResourceId,
                      principalTable: "TestingRequests",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
            table.ForeignKey(
                      name: "FK_TestingRequestPermissions_Users_UserId",
                      column: x => x.UserId,
                      principalTable: "Users",
                      principalColumn: "Id");
          });

      migrationBuilder.CreateTable(
          name: "TestingSessions",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            TestingRequestId = table.Column<Guid>(type: "uuid", nullable: false),
            LocationId = table.Column<Guid>(type: "uuid", nullable: false),
            SessionName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            SessionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            MaxTesters = table.Column<int>(type: "integer", nullable: false),
            RegisteredTesterCount = table.Column<int>(type: "integer", nullable: false),
            RegisteredProjectMemberCount = table.Column<int>(type: "integer", nullable: false),
            RegisteredProjectCount = table.Column<int>(type: "integer", nullable: false),
            Status = table.Column<int>(type: "integer", nullable: false),
            ManagerId = table.Column<Guid>(type: "uuid", nullable: false),
            ManagerUserId = table.Column<Guid>(type: "uuid", nullable: false),
            CreatedById = table.Column<Guid>(type: "uuid", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
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
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
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
            ProductId1 = table.Column<Guid>(type: "uuid", nullable: true),
            UserSubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
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
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
            Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            Visibility = table.Column<int>(type: "integer", nullable: false),
            MetadataId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            UserId = table.Column<Guid>(type: "uuid", nullable: true),
            UserReputationId = table.Column<Guid>(type: "uuid", nullable: true),
            UserTenantReputationId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantPermissionId = table.Column<Guid>(type: "uuid", nullable: true),
            ReputationActionId = table.Column<Guid>(type: "uuid", nullable: true),
            PointsChange = table.Column<int>(type: "integer", nullable: false),
            PreviousScore = table.Column<int>(type: "integer", nullable: false),
            NewScore = table.Column<int>(type: "integer", nullable: false),
            PreviousLevelId = table.Column<Guid>(type: "uuid", nullable: true),
            NewLevelId = table.Column<Guid>(type: "uuid", nullable: true),
            Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
            TriggeredByUserId = table.Column<Guid>(type: "uuid", nullable: true),
            RelatedResourceId = table.Column<Guid>(type: "uuid", nullable: true),
            OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
          },
          constraints: table => {
            table.PrimaryKey("PK_UserReputationHistory", x => x.Id);
            table.CheckConstraint("CK_UserReputationHistory_UserOrUserTenant", "(\"UserReputationId\" IS NOT NULL AND \"UserTenantReputationId\" IS NULL) OR (\"UserReputationId\" IS NULL AND \"UserTenantReputationId\" IS NOT NULL)");
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
                      principalColumn: "Id",
                      onDelete: ReferentialAction.SetNull);
            table.ForeignKey(
                      name: "FK_UserReputationHistory_UserTenantReputations_UserTenantReput~",
                      column: x => x.UserTenantReputationId,
                      principalTable: "UserTenantReputations",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.SetNull);
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
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            SessionId = table.Column<Guid>(type: "uuid", nullable: false),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            RegistrationType = table.Column<int>(type: "integer", nullable: false),
            ProjectRole = table.Column<int>(type: "integer", nullable: true),
            RegistrationNotes = table.Column<string>(type: "text", nullable: true),
            AttendanceStatus = table.Column<int>(type: "integer", nullable: false),
            AttendedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
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
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            SessionId = table.Column<Guid>(type: "uuid", nullable: false),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            RegistrationType = table.Column<int>(type: "integer", nullable: false),
            Position = table.Column<int>(type: "integer", nullable: false),
            RegistrationNotes = table.Column<string>(type: "text", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
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
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            TestingRequestId = table.Column<Guid>(type: "uuid", nullable: false),
            FeedbackFormId = table.Column<Guid>(type: "uuid", nullable: false),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            SessionId = table.Column<Guid>(type: "uuid", nullable: true),
            TestingContext = table.Column<int>(type: "integer", nullable: false),
            FeedbackData = table.Column<string>(type: "text", nullable: false),
            OverallRating = table.Column<int>(type: "integer", nullable: true),
            WouldRecommend = table.Column<bool>(type: "boolean", nullable: true),
            AdditionalNotes = table.Column<string>(type: "text", nullable: true),
            IsReported = table.Column<bool>(type: "boolean", nullable: false),
            QualityRating = table.Column<int>(type: "integer", nullable: true),
            ReportReason = table.Column<string>(type: "text", nullable: true),
            ReportedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
            ReportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true)
          },
          constraints: table => {
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
                      name: "FK_TestingFeedback_Users_ReportedByUserId",
                      column: x => x.ReportedByUserId,
                      principalTable: "Users",
                      principalColumn: "Id");
            table.ForeignKey(
                      name: "FK_TestingFeedback_Users_UserId",
                      column: x => x.UserId,
                      principalTable: "Users",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "TestingSessionPermissions",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            PermissionFlags1 = table.Column<long>(type: "bigint", nullable: false),
            PermissionFlags2 = table.Column<long>(type: "bigint", nullable: false),
            UserId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            ResourceId = table.Column<Guid>(type: "uuid", nullable: false)
          },
          constraints: table => {
            table.PrimaryKey("PK_TestingSessionPermissions", x => x.Id);
            table.ForeignKey(
                      name: "FK_TestingSessionPermissions_Tenants_TenantId",
                      column: x => x.TenantId,
                      principalTable: "Tenants",
                      principalColumn: "Id");
            table.ForeignKey(
                      name: "FK_TestingSessionPermissions_TestingSessions_ResourceId",
                      column: x => x.ResourceId,
                      principalTable: "TestingSessions",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
            table.ForeignKey(
                      name: "FK_TestingSessionPermissions_Users_UserId",
                      column: x => x.UserId,
                      principalTable: "Users",
                      principalColumn: "Id");
          });

      migrationBuilder.CreateTable(
          name: "SessionRegistrationPermissions",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            PermissionFlags1 = table.Column<long>(type: "bigint", nullable: false),
            PermissionFlags2 = table.Column<long>(type: "bigint", nullable: false),
            UserId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            ResourceId = table.Column<Guid>(type: "uuid", nullable: false)
          },
          constraints: table => {
            table.PrimaryKey("PK_SessionRegistrationPermissions", x => x.Id);
            table.ForeignKey(
                      name: "FK_SessionRegistrationPermissions_SessionRegistrations_Resourc~",
                      column: x => x.ResourceId,
                      principalTable: "SessionRegistrations",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
            table.ForeignKey(
                      name: "FK_SessionRegistrationPermissions_Tenants_TenantId",
                      column: x => x.TenantId,
                      principalTable: "Tenants",
                      principalColumn: "Id");
            table.ForeignKey(
                      name: "FK_SessionRegistrationPermissions_Users_UserId",
                      column: x => x.UserId,
                      principalTable: "Users",
                      principalColumn: "Id");
          });

      migrationBuilder.CreateTable(
          name: "TestingFeedbackPermissions",
          columns: table => new {
            Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
            Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
            DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            PermissionFlags1 = table.Column<long>(type: "bigint", nullable: false),
            PermissionFlags2 = table.Column<long>(type: "bigint", nullable: false),
            UserId = table.Column<Guid>(type: "uuid", nullable: true),
            TenantId = table.Column<Guid>(type: "uuid", nullable: true),
            ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            ResourceId = table.Column<Guid>(type: "uuid", nullable: false)
          },
          constraints: table => {
            table.PrimaryKey("PK_TestingFeedbackPermissions", x => x.Id);
            table.ForeignKey(
                      name: "FK_TestingFeedbackPermissions_Tenants_TenantId",
                      column: x => x.TenantId,
                      principalTable: "Tenants",
                      principalColumn: "Id");
            table.ForeignKey(
                      name: "FK_TestingFeedbackPermissions_TestingFeedback_ResourceId",
                      column: x => x.ResourceId,
                      principalTable: "TestingFeedback",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
            table.ForeignKey(
                      name: "FK_TestingFeedbackPermissions_Users_UserId",
                      column: x => x.UserId,
                      principalTable: "Users",
                      principalColumn: "Id");
          });

      migrationBuilder.CreateIndex(
          name: "IX_achievement_levels_AchievementId",
          table: "achievement_levels",
          column: "AchievementId");

      migrationBuilder.CreateIndex(
          name: "IX_achievement_levels_Level",
          table: "achievement_levels",
          column: "Level");

      migrationBuilder.CreateIndex(
          name: "IX_achievement_prerequisites_AchievementId",
          table: "achievement_prerequisites",
          column: "AchievementId");

      migrationBuilder.CreateIndex(
          name: "IX_achievement_prerequisites_PrerequisiteAchievementId",
          table: "achievement_prerequisites",
          column: "PrerequisiteAchievementId");

      migrationBuilder.CreateIndex(
          name: "IX_achievement_progress_AchievementId",
          table: "achievement_progress",
          column: "AchievementId");

      migrationBuilder.CreateIndex(
          name: "IX_achievement_progress_TenantId",
          table: "achievement_progress",
          column: "TenantId");

      migrationBuilder.CreateIndex(
          name: "IX_achievement_progress_UserId",
          table: "achievement_progress",
          column: "UserId");

      migrationBuilder.CreateIndex(
          name: "IX_achievement_progress_UserId_AchievementId",
          table: "achievement_progress",
          columns: new[] { "UserId", "AchievementId" },
          unique: true);

      migrationBuilder.CreateIndex(
          name: "IX_achievements_Category",
          table: "achievements",
          column: "Category");

      migrationBuilder.CreateIndex(
          name: "IX_achievements_IsActive",
          table: "achievements",
          column: "IsActive");

      migrationBuilder.CreateIndex(
          name: "IX_achievements_TenantId",
          table: "achievements",
          column: "TenantId");

      migrationBuilder.CreateIndex(
          name: "IX_achievements_Type",
          table: "achievements",
          column: "Type");

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
          name: "IX_content_progress_CompletedAt",
          table: "content_progress",
          column: "CompletedAt");

      migrationBuilder.CreateIndex(
          name: "IX_content_progress_CompletionStatus",
          table: "content_progress",
          column: "CompletionStatus");

      migrationBuilder.CreateIndex(
          name: "IX_content_progress_ContentId",
          table: "content_progress",
          column: "ContentId");

      migrationBuilder.CreateIndex(
          name: "IX_content_progress_CreatedAt",
          table: "content_progress",
          column: "CreatedAt");

      migrationBuilder.CreateIndex(
          name: "IX_content_progress_DeletedAt",
          table: "content_progress",
          column: "DeletedAt");

      migrationBuilder.CreateIndex(
          name: "IX_content_progress_ProgramEnrollmentId",
          table: "content_progress",
          column: "ProgramEnrollmentId");

      migrationBuilder.CreateIndex(
          name: "IX_content_progress_TenantId",
          table: "content_progress",
          column: "TenantId");

      migrationBuilder.CreateIndex(
          name: "IX_content_progress_UserId",
          table: "content_progress",
          column: "UserId");

      migrationBuilder.CreateIndex(
          name: "IX_content_progress_UserId_ContentId",
          table: "content_progress",
          columns: new[] { "UserId", "ContentId" },
          unique: true);

      migrationBuilder.CreateIndex(
          name: "IX_content_reports_CreatedAt",
          table: "content_reports",
          column: "CreatedAt");

      migrationBuilder.CreateIndex(
          name: "IX_content_reports_DeletedAt",
          table: "content_reports",
          column: "DeletedAt");

      migrationBuilder.CreateIndex(
          name: "IX_content_reports_ModeratorId",
          table: "content_reports",
          column: "ModeratorId");

      migrationBuilder.CreateIndex(
          name: "IX_content_reports_PeerReviewId",
          table: "content_reports",
          column: "PeerReviewId");

      migrationBuilder.CreateIndex(
          name: "IX_content_reports_ReportedUserId",
          table: "content_reports",
          column: "ReportedUserId");

      migrationBuilder.CreateIndex(
          name: "IX_content_reports_ReporterId",
          table: "content_reports",
          column: "ReporterId");

      migrationBuilder.CreateIndex(
          name: "IX_content_reports_ReportType",
          table: "content_reports",
          column: "ReportType");

      migrationBuilder.CreateIndex(
          name: "IX_content_reports_Status",
          table: "content_reports",
          column: "Status");

      migrationBuilder.CreateIndex(
          name: "IX_content_reports_TenantId",
          table: "content_reports",
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
          name: "IX_PaymentRefunds_PaymentId",
          table: "PaymentRefunds",
          column: "PaymentId");

      migrationBuilder.CreateIndex(
          name: "IX_peer_reviews_AssignedAt",
          table: "peer_reviews",
          column: "AssignedAt");

      migrationBuilder.CreateIndex(
          name: "IX_peer_reviews_ContentInteractionId",
          table: "peer_reviews",
          column: "ContentInteractionId");

      migrationBuilder.CreateIndex(
          name: "IX_peer_reviews_CreatedAt",
          table: "peer_reviews",
          column: "CreatedAt");

      migrationBuilder.CreateIndex(
          name: "IX_peer_reviews_DeletedAt",
          table: "peer_reviews",
          column: "DeletedAt");

      migrationBuilder.CreateIndex(
          name: "IX_peer_reviews_RevieweeId",
          table: "peer_reviews",
          column: "RevieweeId");

      migrationBuilder.CreateIndex(
          name: "IX_peer_reviews_ReviewerId",
          table: "peer_reviews",
          column: "ReviewerId");

      migrationBuilder.CreateIndex(
          name: "IX_peer_reviews_Status",
          table: "peer_reviews",
          column: "Status");

      migrationBuilder.CreateIndex(
          name: "IX_peer_reviews_TenantId",
          table: "peer_reviews",
          column: "TenantId");

      migrationBuilder.CreateIndex(
          name: "IX_post_comments_AuthorId",
          table: "post_comments",
          column: "AuthorId");

      migrationBuilder.CreateIndex(
          name: "IX_post_comments_CreatedAt",
          table: "post_comments",
          column: "CreatedAt");

      migrationBuilder.CreateIndex(
          name: "IX_post_comments_DeletedAt",
          table: "post_comments",
          column: "DeletedAt");

      migrationBuilder.CreateIndex(
          name: "IX_post_comments_ParentCommentId",
          table: "post_comments",
          column: "ParentCommentId");

      migrationBuilder.CreateIndex(
          name: "IX_post_comments_PostId",
          table: "post_comments",
          column: "PostId");

      migrationBuilder.CreateIndex(
          name: "IX_post_comments_TenantId",
          table: "post_comments",
          column: "TenantId");

      migrationBuilder.CreateIndex(
          name: "IX_post_content_references_CreatedAt",
          table: "post_content_references",
          column: "CreatedAt");

      migrationBuilder.CreateIndex(
          name: "IX_post_content_references_DeletedAt",
          table: "post_content_references",
          column: "DeletedAt");

      migrationBuilder.CreateIndex(
          name: "IX_post_content_references_PostId",
          table: "post_content_references",
          column: "PostId");

      migrationBuilder.CreateIndex(
          name: "IX_post_content_references_ReferencedResourceId",
          table: "post_content_references",
          column: "ReferencedResourceId");

      migrationBuilder.CreateIndex(
          name: "IX_post_content_references_TenantId",
          table: "post_content_references",
          column: "TenantId");

      migrationBuilder.CreateIndex(
          name: "IX_post_followers_CreatedAt",
          table: "post_followers",
          column: "CreatedAt");

      migrationBuilder.CreateIndex(
          name: "IX_post_followers_DeletedAt",
          table: "post_followers",
          column: "DeletedAt");

      migrationBuilder.CreateIndex(
          name: "IX_post_followers_PostId",
          table: "post_followers",
          column: "PostId");

      migrationBuilder.CreateIndex(
          name: "IX_post_followers_TenantId",
          table: "post_followers",
          column: "TenantId");

      migrationBuilder.CreateIndex(
          name: "IX_post_followers_UserId",
          table: "post_followers",
          column: "UserId");

      migrationBuilder.CreateIndex(
          name: "IX_post_likes_CreatedAt",
          table: "post_likes",
          column: "CreatedAt");

      migrationBuilder.CreateIndex(
          name: "IX_post_likes_DeletedAt",
          table: "post_likes",
          column: "DeletedAt");

      migrationBuilder.CreateIndex(
          name: "IX_post_likes_PostId",
          table: "post_likes",
          column: "PostId");

      migrationBuilder.CreateIndex(
          name: "IX_post_likes_PostId_UserId",
          table: "post_likes",
          columns: new[] { "PostId", "UserId" },
          unique: true);

      migrationBuilder.CreateIndex(
          name: "IX_post_likes_TenantId",
          table: "post_likes",
          column: "TenantId");

      migrationBuilder.CreateIndex(
          name: "IX_post_likes_UserId",
          table: "post_likes",
          column: "UserId");

      migrationBuilder.CreateIndex(
          name: "IX_post_statistics_CreatedAt",
          table: "post_statistics",
          column: "CreatedAt");

      migrationBuilder.CreateIndex(
          name: "IX_post_statistics_DeletedAt",
          table: "post_statistics",
          column: "DeletedAt");

      migrationBuilder.CreateIndex(
          name: "IX_post_statistics_PostId",
          table: "post_statistics",
          column: "PostId",
          unique: true);

      migrationBuilder.CreateIndex(
          name: "IX_post_statistics_TenantId",
          table: "post_statistics",
          column: "TenantId");

      migrationBuilder.CreateIndex(
          name: "IX_post_tag_assignments_CreatedAt",
          table: "post_tag_assignments",
          column: "CreatedAt");

      migrationBuilder.CreateIndex(
          name: "IX_post_tag_assignments_DeletedAt",
          table: "post_tag_assignments",
          column: "DeletedAt");

      migrationBuilder.CreateIndex(
          name: "IX_post_tag_assignments_PostId",
          table: "post_tag_assignments",
          column: "PostId");

      migrationBuilder.CreateIndex(
          name: "IX_post_tag_assignments_TagId",
          table: "post_tag_assignments",
          column: "TagId");

      migrationBuilder.CreateIndex(
          name: "IX_post_tag_assignments_TenantId",
          table: "post_tag_assignments",
          column: "TenantId");

      migrationBuilder.CreateIndex(
          name: "IX_post_tags_Category",
          table: "post_tags",
          column: "Category");

      migrationBuilder.CreateIndex(
          name: "IX_post_tags_CreatedAt",
          table: "post_tags",
          column: "CreatedAt");

      migrationBuilder.CreateIndex(
          name: "IX_post_tags_DeletedAt",
          table: "post_tags",
          column: "DeletedAt");

      migrationBuilder.CreateIndex(
          name: "IX_post_tags_Name",
          table: "post_tags",
          column: "Name");

      migrationBuilder.CreateIndex(
          name: "IX_post_tags_TenantId",
          table: "post_tags",
          column: "TenantId");

      migrationBuilder.CreateIndex(
          name: "IX_post_tags_UsageCount",
          table: "post_tags",
          column: "UsageCount");

      migrationBuilder.CreateIndex(
          name: "IX_post_views_CreatedAt",
          table: "post_views",
          column: "CreatedAt");

      migrationBuilder.CreateIndex(
          name: "IX_post_views_DeletedAt",
          table: "post_views",
          column: "DeletedAt");

      migrationBuilder.CreateIndex(
          name: "IX_post_views_IpAddress",
          table: "post_views",
          column: "IpAddress");

      migrationBuilder.CreateIndex(
          name: "IX_post_views_PostId",
          table: "post_views",
          column: "PostId");

      migrationBuilder.CreateIndex(
          name: "IX_post_views_TenantId",
          table: "post_views",
          column: "TenantId");

      migrationBuilder.CreateIndex(
          name: "IX_post_views_UserId",
          table: "post_views",
          column: "UserId");

      migrationBuilder.CreateIndex(
          name: "IX_post_views_ViewedAt",
          table: "post_views",
          column: "ViewedAt");

      migrationBuilder.CreateIndex(
          name: "IX_posts_AuthorId",
          table: "posts",
          column: "AuthorId");

      migrationBuilder.CreateIndex(
          name: "IX_posts_CreatedAt",
          table: "posts",
          column: "CreatedAt");

      migrationBuilder.CreateIndex(
          name: "IX_posts_DeletedAt",
          table: "posts",
          column: "DeletedAt");

      migrationBuilder.CreateIndex(
          name: "IX_posts_IsPinned",
          table: "posts",
          column: "IsPinned");

      migrationBuilder.CreateIndex(
          name: "IX_posts_IsSystemGenerated",
          table: "posts",
          column: "IsSystemGenerated");

      migrationBuilder.CreateIndex(
          name: "IX_posts_MetadataId",
          table: "posts",
          column: "MetadataId");

      migrationBuilder.CreateIndex(
          name: "IX_posts_PostType",
          table: "posts",
          column: "PostType");

      migrationBuilder.CreateIndex(
          name: "IX_posts_TenantId",
          table: "posts",
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
          name: "IX_program_enrollments_CompletionStatus",
          table: "program_enrollments",
          column: "CompletionStatus");

      migrationBuilder.CreateIndex(
          name: "IX_program_enrollments_CreatedAt",
          table: "program_enrollments",
          column: "CreatedAt");

      migrationBuilder.CreateIndex(
          name: "IX_program_enrollments_DeletedAt",
          table: "program_enrollments",
          column: "DeletedAt");

      migrationBuilder.CreateIndex(
          name: "IX_program_enrollments_EnrollmentStatus",
          table: "program_enrollments",
          column: "EnrollmentStatus");

      migrationBuilder.CreateIndex(
          name: "IX_program_enrollments_ProgramId",
          table: "program_enrollments",
          column: "ProgramId");

      migrationBuilder.CreateIndex(
          name: "IX_program_enrollments_TenantId",
          table: "program_enrollments",
          column: "TenantId");

      migrationBuilder.CreateIndex(
          name: "IX_program_enrollments_UserId",
          table: "program_enrollments",
          column: "UserId");

      migrationBuilder.CreateIndex(
          name: "IX_program_enrollments_UserId_ProgramId",
          table: "program_enrollments",
          columns: new[] { "UserId", "ProgramId" },
          unique: true);

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
          name: "IX_program_ratings_DeletedAt",
          table: "program_ratings",
          column: "DeletedAt");

      migrationBuilder.CreateIndex(
          name: "IX_program_ratings_ProgramId",
          table: "program_ratings",
          column: "ProgramId");

      migrationBuilder.CreateIndex(
          name: "IX_program_ratings_ProgramUserId",
          table: "program_ratings",
          column: "ProgramUserId");

      migrationBuilder.CreateIndex(
          name: "IX_program_ratings_TenantId",
          table: "program_ratings",
          column: "TenantId");

      migrationBuilder.CreateIndex(
          name: "IX_program_ratings_UserId",
          table: "program_ratings",
          column: "UserId");

      migrationBuilder.CreateIndex(
          name: "IX_ProgramRatings_CreatedAt",
          table: "program_ratings",
          column: "CreatedAt");

      migrationBuilder.CreateIndex(
          name: "IX_ProgramRatings_IsFeatured",
          table: "program_ratings",
          column: "IsFeatured");

      migrationBuilder.CreateIndex(
          name: "IX_ProgramRatings_IsVerified",
          table: "program_ratings",
          column: "IsVerified");

      migrationBuilder.CreateIndex(
          name: "IX_ProgramRatings_ProgramId_UserId_Unique",
          table: "program_ratings",
          columns: new[] { "ProgramId", "UserId" },
          unique: true);

      migrationBuilder.CreateIndex(
          name: "IX_ProgramRatings_Rating",
          table: "program_ratings",
          column: "Rating");

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
          name: "IX_SessionRegistrationPermissions_CreatedAt",
          table: "SessionRegistrationPermissions",
          column: "CreatedAt");

      migrationBuilder.CreateIndex(
          name: "IX_SessionRegistrationPermissions_DeletedAt",
          table: "SessionRegistrationPermissions",
          column: "DeletedAt");

      migrationBuilder.CreateIndex(
          name: "IX_SessionRegistrationPermissions_ResourceId",
          table: "SessionRegistrationPermissions",
          column: "ResourceId");

      migrationBuilder.CreateIndex(
          name: "IX_SessionRegistrationPermissions_TenantId",
          table: "SessionRegistrationPermissions",
          column: "TenantId");

      migrationBuilder.CreateIndex(
          name: "IX_SessionRegistrationPermissions_User_Tenant_Resource",
          table: "SessionRegistrationPermissions",
          columns: new[] { "UserId", "TenantId", "ResourceId" });

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
          name: "IX_TestingFeedback_ReportedByUserId",
          table: "TestingFeedback",
          column: "ReportedByUserId");

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
          name: "IX_TestingFeedbackPermissions_CreatedAt",
          table: "TestingFeedbackPermissions",
          column: "CreatedAt");

      migrationBuilder.CreateIndex(
          name: "IX_TestingFeedbackPermissions_DeletedAt",
          table: "TestingFeedbackPermissions",
          column: "DeletedAt");

      migrationBuilder.CreateIndex(
          name: "IX_TestingFeedbackPermissions_ResourceId",
          table: "TestingFeedbackPermissions",
          column: "ResourceId");

      migrationBuilder.CreateIndex(
          name: "IX_TestingFeedbackPermissions_TenantId",
          table: "TestingFeedbackPermissions",
          column: "TenantId");

      migrationBuilder.CreateIndex(
          name: "IX_TestingFeedbackPermissions_User_Tenant_Resource",
          table: "TestingFeedbackPermissions",
          columns: new[] { "UserId", "TenantId", "ResourceId" });

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
          name: "IX_TestingRequestPermissions_CreatedAt",
          table: "TestingRequestPermissions",
          column: "CreatedAt");

      migrationBuilder.CreateIndex(
          name: "IX_TestingRequestPermissions_DeletedAt",
          table: "TestingRequestPermissions",
          column: "DeletedAt");

      migrationBuilder.CreateIndex(
          name: "IX_TestingRequestPermissions_ResourceId",
          table: "TestingRequestPermissions",
          column: "ResourceId");

      migrationBuilder.CreateIndex(
          name: "IX_TestingRequestPermissions_TenantId",
          table: "TestingRequestPermissions",
          column: "TenantId");

      migrationBuilder.CreateIndex(
          name: "IX_TestingRequestPermissions_UserId",
          table: "TestingRequestPermissions",
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
          name: "IX_TestingSessionPermissions_CreatedAt",
          table: "TestingSessionPermissions",
          column: "CreatedAt");

      migrationBuilder.CreateIndex(
          name: "IX_TestingSessionPermissions_DeletedAt",
          table: "TestingSessionPermissions",
          column: "DeletedAt");

      migrationBuilder.CreateIndex(
          name: "IX_TestingSessionPermissions_Expiration",
          table: "TestingSessionPermissions",
          column: "ExpiresAt");

      migrationBuilder.CreateIndex(
          name: "IX_TestingSessionPermissions_Resource_User",
          table: "TestingSessionPermissions",
          columns: new[] { "ResourceId", "UserId" });

      migrationBuilder.CreateIndex(
          name: "IX_TestingSessionPermissions_TenantId",
          table: "TestingSessionPermissions",
          column: "TenantId");

      migrationBuilder.CreateIndex(
          name: "IX_TestingSessionPermissions_User_Tenant_Resource",
          table: "TestingSessionPermissions",
          columns: new[] { "UserId", "TenantId", "ResourceId" },
          unique: true);

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
          name: "IX_user_achievements_AchievementId",
          table: "user_achievements",
          column: "AchievementId");

      migrationBuilder.CreateIndex(
          name: "IX_user_achievements_EarnedAt",
          table: "user_achievements",
          column: "EarnedAt");

      migrationBuilder.CreateIndex(
          name: "IX_user_achievements_TenantId",
          table: "user_achievements",
          column: "TenantId");

      migrationBuilder.CreateIndex(
          name: "IX_user_achievements_UserId",
          table: "user_achievements",
          column: "UserId");

      migrationBuilder.CreateIndex(
          name: "IX_user_achievements_UserId_AchievementId",
          table: "user_achievements",
          columns: new[] { "UserId", "AchievementId" });

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
          name: "FK_achievement_progress_Users_UserId",
          table: "achievement_progress",
          column: "UserId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

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
          name: "FK_certificate_blockchain_anchors_user_certificates_Certificat~",
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
          name: "FK_content_progress_Tenants_TenantId",
          table: "content_progress",
          column: "TenantId",
          principalTable: "Tenants",
          principalColumn: "Id");

      migrationBuilder.AddForeignKey(
          name: "FK_content_progress_Users_UserId",
          table: "content_progress",
          column: "UserId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
          name: "FK_content_progress_program_enrollments_ProgramEnrollmentId",
          table: "content_progress",
          column: "ProgramEnrollmentId",
          principalTable: "program_enrollments",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
          name: "FK_content_reports_Tenants_TenantId",
          table: "content_reports",
          column: "TenantId",
          principalTable: "Tenants",
          principalColumn: "Id");

      migrationBuilder.AddForeignKey(
          name: "FK_content_reports_Users_ModeratorId",
          table: "content_reports",
          column: "ModeratorId",
          principalTable: "Users",
          principalColumn: "Id");

      migrationBuilder.AddForeignKey(
          name: "FK_content_reports_Users_ReportedUserId",
          table: "content_reports",
          column: "ReportedUserId",
          principalTable: "Users",
          principalColumn: "Id");

      migrationBuilder.AddForeignKey(
          name: "FK_content_reports_Users_ReporterId",
          table: "content_reports",
          column: "ReporterId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
          name: "FK_content_reports_peer_reviews_PeerReviewId",
          table: "content_reports",
          column: "PeerReviewId",
          principalTable: "peer_reviews",
          principalColumn: "Id");

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
          name: "FK_financial_transactions_user_financial_methods_PaymentMethod~",
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
          name: "FK_peer_reviews_Tenants_TenantId",
          table: "peer_reviews",
          column: "TenantId",
          principalTable: "Tenants",
          principalColumn: "Id");

      migrationBuilder.AddForeignKey(
          name: "FK_peer_reviews_Users_RevieweeId",
          table: "peer_reviews",
          column: "RevieweeId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
          name: "FK_peer_reviews_Users_ReviewerId",
          table: "peer_reviews",
          column: "ReviewerId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
          name: "FK_post_comments_Tenants_TenantId",
          table: "post_comments",
          column: "TenantId",
          principalTable: "Tenants",
          principalColumn: "Id");

      migrationBuilder.AddForeignKey(
          name: "FK_post_comments_Users_AuthorId",
          table: "post_comments",
          column: "AuthorId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
          name: "FK_post_comments_posts_PostId",
          table: "post_comments",
          column: "PostId",
          principalTable: "posts",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
          name: "FK_post_content_references_Tenants_TenantId",
          table: "post_content_references",
          column: "TenantId",
          principalTable: "Tenants",
          principalColumn: "Id");

      migrationBuilder.AddForeignKey(
          name: "FK_post_content_references_posts_PostId",
          table: "post_content_references",
          column: "PostId",
          principalTable: "posts",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
          name: "FK_post_followers_Tenants_TenantId",
          table: "post_followers",
          column: "TenantId",
          principalTable: "Tenants",
          principalColumn: "Id");

      migrationBuilder.AddForeignKey(
          name: "FK_post_followers_Users_UserId",
          table: "post_followers",
          column: "UserId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
          name: "FK_post_followers_posts_PostId",
          table: "post_followers",
          column: "PostId",
          principalTable: "posts",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
          name: "FK_post_likes_Tenants_TenantId",
          table: "post_likes",
          column: "TenantId",
          principalTable: "Tenants",
          principalColumn: "Id");

      migrationBuilder.AddForeignKey(
          name: "FK_post_likes_Users_UserId",
          table: "post_likes",
          column: "UserId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
          name: "FK_post_likes_posts_PostId",
          table: "post_likes",
          column: "PostId",
          principalTable: "posts",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
          name: "FK_post_statistics_Tenants_TenantId",
          table: "post_statistics",
          column: "TenantId",
          principalTable: "Tenants",
          principalColumn: "Id");

      migrationBuilder.AddForeignKey(
          name: "FK_post_statistics_posts_PostId",
          table: "post_statistics",
          column: "PostId",
          principalTable: "posts",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
          name: "FK_post_tag_assignments_Tenants_TenantId",
          table: "post_tag_assignments",
          column: "TenantId",
          principalTable: "Tenants",
          principalColumn: "Id");

      migrationBuilder.AddForeignKey(
          name: "FK_post_tag_assignments_post_tags_TagId",
          table: "post_tag_assignments",
          column: "TagId",
          principalTable: "post_tags",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
          name: "FK_post_tag_assignments_posts_PostId",
          table: "post_tag_assignments",
          column: "PostId",
          principalTable: "posts",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
          name: "FK_post_tags_Tenants_TenantId",
          table: "post_tags",
          column: "TenantId",
          principalTable: "Tenants",
          principalColumn: "Id");

      migrationBuilder.AddForeignKey(
          name: "FK_post_views_Tenants_TenantId",
          table: "post_views",
          column: "TenantId",
          principalTable: "Tenants",
          principalColumn: "Id");

      migrationBuilder.AddForeignKey(
          name: "FK_post_views_Users_UserId",
          table: "post_views",
          column: "UserId",
          principalTable: "Users",
          principalColumn: "Id");

      migrationBuilder.AddForeignKey(
          name: "FK_post_views_posts_PostId",
          table: "post_views",
          column: "PostId",
          principalTable: "posts",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
          name: "FK_posts_ResourceMetadata_MetadataId",
          table: "posts",
          column: "MetadataId",
          principalTable: "ResourceMetadata",
          principalColumn: "Id",
          onDelete: ReferentialAction.SetNull);

      migrationBuilder.AddForeignKey(
          name: "FK_posts_Tenants_TenantId",
          table: "posts",
          column: "TenantId",
          principalTable: "Tenants",
          principalColumn: "Id",
          onDelete: ReferentialAction.SetNull);

      migrationBuilder.AddForeignKey(
          name: "FK_posts_Users_AuthorId",
          table: "posts",
          column: "AuthorId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

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
          name: "FK_program_enrollments_Tenants_TenantId",
          table: "program_enrollments",
          column: "TenantId",
          principalTable: "Tenants",
          principalColumn: "Id");

      migrationBuilder.AddForeignKey(
          name: "FK_program_enrollments_Users_UserId",
          table: "program_enrollments",
          column: "UserId",
          principalTable: "Users",
          principalColumn: "Id",
          onDelete: ReferentialAction.Cascade);

      migrationBuilder.AddForeignKey(
          name: "FK_program_enrollments_programs_ProgramId",
          table: "program_enrollments",
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
          name: "FK_program_ratings_program_users_ProgramUserId",
          table: "program_ratings",
          column: "ProgramUserId",
          principalTable: "program_users",
          principalColumn: "Id");

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
          name: "FK_ResourceLocalizations_UserReputationHistory_UserReputationH~",
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
          name: "FK_ResourceLocalizations_UserTenantReputations_UserTenantReput~",
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
    protected override void Down(MigrationBuilder migrationBuilder) {
      migrationBuilder.DropForeignKey(
          name: "FK_ResourceMetadata_Tenants_TenantId",
          table: "ResourceMetadata");

      migrationBuilder.DropTable(
          name: "achievement_levels");

      migrationBuilder.DropTable(
          name: "achievement_prerequisites");

      migrationBuilder.DropTable(
          name: "achievement_progress");

      migrationBuilder.DropTable(
          name: "activity_grades");

      migrationBuilder.DropTable(
          name: "certificate_blockchain_anchors");

      migrationBuilder.DropTable(
          name: "certificate_tags");

      migrationBuilder.DropTable(
          name: "CommentPermissions");

      migrationBuilder.DropTable(
          name: "content_progress");

      migrationBuilder.DropTable(
          name: "content_reports");

      migrationBuilder.DropTable(
          name: "ContentContentLicense");

      migrationBuilder.DropTable(
          name: "ContentTypePermissions");

      migrationBuilder.DropTable(
          name: "Credentials");

      migrationBuilder.DropTable(
          name: "DiscountCodes");

      migrationBuilder.DropTable(
          name: "JamScore");

      migrationBuilder.DropTable(
          name: "PaymentRefunds");

      migrationBuilder.DropTable(
          name: "post_comments");

      migrationBuilder.DropTable(
          name: "post_content_references");

      migrationBuilder.DropTable(
          name: "post_followers");

      migrationBuilder.DropTable(
          name: "post_likes");

      migrationBuilder.DropTable(
          name: "post_statistics");

      migrationBuilder.DropTable(
          name: "post_tag_assignments");

      migrationBuilder.DropTable(
          name: "post_views");

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
          name: "SessionRegistrationPermissions");

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
          name: "TestingFeedbackPermissions");

      migrationBuilder.DropTable(
          name: "TestingParticipants");

      migrationBuilder.DropTable(
          name: "TestingRequestPermissions");

      migrationBuilder.DropTable(
          name: "TestingSessionPermissions");

      migrationBuilder.DropTable(
          name: "user_achievements");

      migrationBuilder.DropTable(
          name: "user_kyc_verifications");

      migrationBuilder.DropTable(
          name: "user_products");

      migrationBuilder.DropTable(
          name: "user_certificates");

      migrationBuilder.DropTable(
          name: "tag_proficiencies");

      migrationBuilder.DropTable(
          name: "program_enrollments");

      migrationBuilder.DropTable(
          name: "peer_reviews");

      migrationBuilder.DropTable(
          name: "Payments");

      migrationBuilder.DropTable(
          name: "post_tags");

      migrationBuilder.DropTable(
          name: "posts");

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
          name: "SessionRegistrations");

      migrationBuilder.DropTable(
          name: "tags");

      migrationBuilder.DropTable(
          name: "TenantUserGroups");

      migrationBuilder.DropTable(
          name: "TestingFeedback");

      migrationBuilder.DropTable(
          name: "achievements");

      migrationBuilder.DropTable(
          name: "user_subscriptions");

      migrationBuilder.DropTable(
          name: "certificates");

      migrationBuilder.DropTable(
          name: "content_interactions");

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
          name: "TestingFeedbackForms");

      migrationBuilder.DropTable(
          name: "TestingSessions");

      migrationBuilder.DropTable(
          name: "product_subscription_plans");

      migrationBuilder.DropTable(
          name: "program_contents");

      migrationBuilder.DropTable(
          name: "program_users");

      migrationBuilder.DropTable(
          name: "ReputationLevels");

      migrationBuilder.DropTable(
          name: "TenantPermissions");

      migrationBuilder.DropTable(
          name: "TestingLocations");

      migrationBuilder.DropTable(
          name: "TestingRequests");

      migrationBuilder.DropTable(
          name: "Products");

      migrationBuilder.DropTable(
          name: "programs");

      migrationBuilder.DropTable(
          name: "ProjectVersion");

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
