using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddLedgersRatingsAchievementsVerificationTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "finance");

            migrationBuilder.CreateTable(
                name: "achievements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IconUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsSecret = table.Column<bool>(type: "boolean", nullable: false),
                    IsRepeatable = table.Column<bool>(type: "boolean", nullable: false),
                    Conditions = table.Column<string>(type: "jsonb", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_achievements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ledgers",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    FiscalYearStartMonth = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Tags = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ParentLedgerId = table.Column<Guid>(type: "uuid", nullable: true),
                    HierarchyPath = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    HierarchyDepth = table.Column<int>(type: "integer", nullable: false),
                    VirtualFilterSpec = table.Column<string>(type: "jsonb", nullable: true),
                    VirtualSourceLedgerIds = table.Column<List<Guid>>(type: "uuid[]", nullable: true),
                    IsShared = table.Column<bool>(type: "boolean", nullable: false),
                    AllowDirectEntries = table.Column<bool>(type: "boolean", nullable: false),
                    BudgetLimit = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    ProjectStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ProjectEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CachedTotalDebit = table.Column<decimal>(type: "numeric", nullable: false),
                    CachedTotalCredit = table.Column<decimal>(type: "numeric", nullable: false),
                    CachedNetBalance = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    CachedEntryCount = table.Column<int>(type: "integer", nullable: false),
                    StatsCalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ledgers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ledgers_ParentLedger",
                        column: x => x.ParentLedgerId,
                        principalSchema: "finance",
                        principalTable: "Ledgers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rating_helpful_votes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RatingId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsHelpful = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rating_helpful_votes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rating_summaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AverageRating = table.Column<decimal>(type: "numeric(3,2)", nullable: false),
                    TotalRatings = table.Column<int>(type: "integer", nullable: false),
                    OneStar = table.Column<int>(type: "integer", nullable: false),
                    TwoStar = table.Column<int>(type: "integer", nullable: false),
                    ThreeStar = table.Column<int>(type: "integer", nullable: false),
                    FourStar = table.Column<int>(type: "integer", nullable: false),
                    FiveStar = table.Column<int>(type: "integer", nullable: false),
                    TotalReviews = table.Column<int>(type: "integer", nullable: false),
                    LastCalculatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rating_summaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ratings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: false),
                    ReviewText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ReviewTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    HelpfulCount = table.Column<int>(type: "integer", nullable: false),
                    ReportCount = table.Column<int>(type: "integer", nullable: false),
                    ModerationStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ratings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "verification_tokens",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    token_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    token_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verification_tokens", x => x.token_hash);
                });

            migrationBuilder.CreateTable(
                name: "achievement_levels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AchievementId = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    RequiredProgress = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    IconUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
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
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AchievementId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrerequisiteAchievementId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequiresCompletion = table.Column<bool>(type: "boolean", nullable: false),
                    MinimumLevel = table.Column<int>(type: "integer", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
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
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "achievement_progress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AchievementId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentProgress = table.Column<int>(type: "integer", nullable: false),
                    TargetProgress = table.Column<int>(type: "integer", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    Context = table.Column<string>(type: "jsonb", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_achievement_progress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_achievement_progress_achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalTable: "achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_achievements",
                columns: table => new
                {
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
                    EarnCount = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_achievements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_achievements_achievements_AchievementId",
                        column: x => x.AchievementId,
                        principalTable: "achievements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LedgerClosures",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AncestorId = table.Column<Guid>(type: "uuid", nullable: false),
                    DescendantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Depth = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedgerClosures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LedgerClosures_AncestorLedger",
                        column: x => x.AncestorId,
                        principalSchema: "finance",
                        principalTable: "Ledgers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LedgerClosures_DescendantLedger",
                        column: x => x.DescendantId,
                        principalSchema: "finance",
                        principalTable: "Ledgers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LedgerEntries",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExternalReferenceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LedgerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentLedgerIds = table.Column<string>(type: "jsonb", nullable: false),
                    LedgerPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    OriginalAmount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    OriginalCurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    ExchangeRate = table.Column<decimal>(type: "numeric(19,10)", precision: 19, scale: 10, nullable: true),
                    TransactionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PostingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Tags = table.Column<string>(type: "jsonb", nullable: false),
                    CounterpartyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CounterpartyReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TransferPairEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransferDestinationLedgerId = table.Column<Guid>(type: "uuid", nullable: true),
                    BudgetId = table.Column<Guid>(type: "uuid", nullable: true),
                    BudgetCategoryCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BudgetPeriodId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReversesEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReversedByEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    AttachmentIds = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LedgerEntries_Ledger",
                        column: x => x.LedgerId,
                        principalSchema: "finance",
                        principalTable: "Ledgers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LedgerEntries_ReversedByEntry",
                        column: x => x.ReversedByEntryId,
                        principalSchema: "finance",
                        principalTable: "LedgerEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LedgerEntries_ReversesEntry",
                        column: x => x.ReversesEntryId,
                        principalSchema: "finance",
                        principalTable: "LedgerEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LedgerEntries_TransferPair",
                        column: x => x.TransferPairEntryId,
                        principalSchema: "finance",
                        principalTable: "LedgerEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_achievement_levels_AchievementId",
                table: "achievement_levels",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_achievement_levels_AchievementId_Level",
                table: "achievement_levels",
                columns: new[] { "AchievementId", "Level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_achievement_levels_Level",
                table: "achievement_levels",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_achievement_prerequisites_AchievementId",
                table: "achievement_prerequisites",
                column: "AchievementId");

            migrationBuilder.CreateIndex(
                name: "IX_achievement_prerequisites_AchievementId_PrerequisiteAchieve~",
                table: "achievement_prerequisites",
                columns: new[] { "AchievementId", "PrerequisiteAchievementId" },
                unique: true);

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
                name: "IX_achievements_Category_IsActive",
                table: "achievements",
                columns: new[] { "Category", "IsActive" });

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
                name: "IX_LedgerClosures_Ancestor_Depth",
                schema: "finance",
                table: "LedgerClosures",
                columns: new[] { "AncestorId", "Depth" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerClosures_Ancestor_Descendant",
                schema: "finance",
                table: "LedgerClosures",
                columns: new[] { "AncestorId", "DescendantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LedgerClosures_Descendant_Depth",
                schema: "finance",
                table: "LedgerClosures",
                columns: new[] { "DescendantId", "Depth" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerClosures_TenantId",
                schema: "finance",
                table: "LedgerClosures",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerClosures_TenantId_Ancestor",
                schema: "finance",
                table: "LedgerClosures",
                columns: new[] { "TenantId", "AncestorId" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_ExternalReferenceId",
                schema: "finance",
                table: "LedgerEntries",
                column: "ExternalReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_LedgerId_Category",
                schema: "finance",
                table: "LedgerEntries",
                columns: new[] { "LedgerId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_LedgerId_Status",
                schema: "finance",
                table: "LedgerEntries",
                columns: new[] { "LedgerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_LedgerId_TransactionDate",
                schema: "finance",
                table: "LedgerEntries",
                columns: new[] { "LedgerId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_ReferenceNumber",
                schema: "finance",
                table: "LedgerEntries",
                column: "ReferenceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_ReversedByEntryId",
                schema: "finance",
                table: "LedgerEntries",
                column: "ReversedByEntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_ReversesEntryId",
                schema: "finance",
                table: "LedgerEntries",
                column: "ReversesEntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_TransferPairEntryId",
                schema: "finance",
                table: "LedgerEntries",
                column: "TransferPairEntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ledgers_ParentLedgerId",
                schema: "finance",
                table: "Ledgers",
                column: "ParentLedgerId");

            migrationBuilder.CreateIndex(
                name: "IX_Ledgers_TenantId",
                schema: "finance",
                table: "Ledgers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Ledgers_TenantId_Code",
                schema: "finance",
                table: "Ledgers",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ledgers_TenantId_Slug",
                schema: "finance",
                table: "Ledgers",
                columns: new[] { "TenantId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ledgers_TenantId_Status",
                schema: "finance",
                table: "Ledgers",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Ledgers_TenantId_Type",
                schema: "finance",
                table: "Ledgers",
                columns: new[] { "TenantId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_rating_helpful_votes_RatingId",
                table: "rating_helpful_votes",
                column: "RatingId");

            migrationBuilder.CreateIndex(
                name: "IX_rating_helpful_votes_RatingId_UserId",
                table: "rating_helpful_votes",
                columns: new[] { "RatingId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rating_helpful_votes_UserId",
                table: "rating_helpful_votes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_rating_summaries_AverageRating",
                table: "rating_summaries",
                column: "AverageRating");

            migrationBuilder.CreateIndex(
                name: "IX_rating_summaries_EntityId_EntityType",
                table: "rating_summaries",
                columns: new[] { "EntityId", "EntityType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rating_summaries_TotalRatings",
                table: "rating_summaries",
                column: "TotalRatings");

            migrationBuilder.CreateIndex(
                name: "IX_ratings_CreatedAt",
                table: "ratings",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ratings_EntityId_EntityType",
                table: "ratings",
                columns: new[] { "EntityId", "EntityType" });

            migrationBuilder.CreateIndex(
                name: "IX_ratings_EntityId_EntityType_UserId",
                table: "ratings",
                columns: new[] { "EntityId", "EntityType", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ratings_UserId",
                table: "ratings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ratings_Value",
                table: "ratings",
                column: "Value");

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
                name: "IX_user_achievements_UserId_IsNotified",
                table: "user_achievements",
                columns: new[] { "UserId", "IsNotified" });

            migrationBuilder.CreateIndex(
                name: "ix_verification_tokens_expires_at",
                schema: "gameguild.authentication",
                table: "verification_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_verification_tokens_token_type",
                schema: "gameguild.authentication",
                table: "verification_tokens",
                column: "token_type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "achievement_levels");

            migrationBuilder.DropTable(
                name: "achievement_prerequisites");

            migrationBuilder.DropTable(
                name: "achievement_progress");

            migrationBuilder.DropTable(
                name: "LedgerClosures",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "LedgerEntries",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "rating_helpful_votes");

            migrationBuilder.DropTable(
                name: "rating_summaries");

            migrationBuilder.DropTable(
                name: "ratings");

            migrationBuilder.DropTable(
                name: "user_achievements");

            migrationBuilder.DropTable(
                name: "verification_tokens",
                schema: "gameguild.authentication");

            migrationBuilder.DropTable(
                name: "Ledgers",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "achievements");
        }
    }
}
