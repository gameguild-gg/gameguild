using FluentAssertions;
using GameGuild.Compliance.Audit;
using GameGuild.Compliance.Consent;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Xunit;

namespace GameGuild.Tests.Audit.Unit.Services;

/// <summary>
/// Field-level data access auditing family (issue #186): automatic sensitive-field classification,
/// masked field read/write tracking, bulk export detection with threshold alerting, cross-border
/// transfer validation, the GDPR Article 30 register, data subject access monitoring with consent
/// state, and sensitive-data retention enforcement.
/// </summary>
public sealed class FieldLevelAuditFamilyTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    // ── Classification (criterion 2) ───────────────────────────────────────────

    [Theory]
    [InlineData("Email", SensitiveDataCategory.PII)]
    [InlineData("DateOfBirth", SensitiveDataCategory.PII)]
    [InlineData("Diagnosis", SensitiveDataCategory.PHI)]
    [InlineData("Medication", SensitiveDataCategory.PHI)]
    [InlineData("Password", SensitiveDataCategory.Credentials)]
    [InlineData("CreditCardNumber", SensitiveDataCategory.Financial)]
    public void ClassifierIdentifiesSensitiveFieldsByName(string fieldName, SensitiveDataCategory expectedCategory)
    {
        var match = SensitiveFieldClassifier.Classify("User", fieldName, null);

        match.Should().NotBeNull();
        match!.Category.Should().Be(expectedCategory);
        match.IsSensitive.Should().BeTrue();
        match.Confidence.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public void ClassifierIdentifiesSensitiveValuesWhenTheNameIsInconclusive()
    {
        var card = SensitiveFieldClassifier.Classify("Order", "Note", "4111111111111111"); // Luhn-valid test card
        var email = SensitiveFieldClassifier.Classify("Order", "Note", "someone@example.com");

        card!.Category.Should().Be(SensitiveDataCategory.Financial);
        email!.Category.Should().Be(SensitiveDataCategory.PII);
        SensitiveFieldClassifier.Classify("Order", "Note", "plain text").Should().BeNull();
        SensitiveFieldClassifier.Classify("Order", "Note", "4111111111111111")!
            .Category.Should().Be(SensitiveDataCategory.Financial);
    }

    [Theory]
    [InlineData("Id")]
    [InlineData("TenantId")]
    [InlineData("CreatedAtUtc")]
    [InlineData("Title")]
    public void ClassifierIgnoresInfrastructureAndNonSensitiveFields(string fieldName)
    {
        SensitiveFieldClassifier.Classify("Invoice", fieldName, "plain text").Should().BeNull();
    }

    [Fact]
    public void MaskingEscalatesWithSensitivity()
    {
        SensitiveFieldClassifier.Mask("hello@example.com", SensitivityLevel.Public).Should().Be("hello@example.com");
        SensitiveFieldClassifier.Mask("hello@example.com", SensitivityLevel.Confidential).Should().NotBe("hello@example.com");
        SensitiveFieldClassifier.Mask("secret-value", SensitivityLevel.HighlyRestricted).Should().Be("[MASKED]");
    }

    // ── Field-level read/write tracking (criteria 1 and 5) ────────────────────

    [Fact]
    public async Task RecordingAFieldWriteClassifiesAndMasksTheValue()
    {
        await using var context = CreateContext();
        var service = new FieldAccessAuditService(context, TimeProvider.System);

        var result = await service.RecordFieldAccessAsync(
            TenantId, UserId, "User", Guid.NewGuid(), "Email",
            FieldAccessType.Write, oldValue: "old@example.com", newValue: "new@example.com",
            isSensitive: false, sensitivityLevel: SensitivityLevel.Public,
            ipAddress: "10.0.0.1", userAgent: "test-agent");

        result.IsSuccess.Should().BeTrue();
        // The classifier is authoritative: the caller could not downgrade the Email field.
        result.Value.IsSensitiveField.Should().BeTrue();
        result.Value.SensitivityLevel.Should().Be(SensitivityLevel.Confidential);
        result.Value.MaskedNewValue.Should().NotBeNull().And.NotBe("new@example.com");

        var classifications = await service.GetClassificationsAsync(TenantId);
        classifications.Should().ContainSingle(classification =>
            classification.EntityType == "User"
            && classification.FieldName == "Email"
            && classification.Category == SensitiveDataCategory.PII);
    }

    [Fact]
    public async Task RecordingAFieldReadPersistsOneObservationPerField()
    {
        await using var context = CreateContext();
        var service = new FieldAccessAuditService(context, TimeProvider.System);
        var entityId = Guid.NewGuid();

        var result = await service.RecordFieldReadAsync(
            TenantId, UserId, "User", entityId, ["Username", "Email", "Bio"],
            ipAddress: "10.0.0.1", userAgent: "test-agent", requestId: "req-1");

        result.IsSuccess.Should().BeTrue();
        var history = await service.GetFieldAccessHistoryAsync(entityId);
        history.Value.Count().Should().Be(3);
        history.Value.Should().OnlyContain(access => access.AccessType == FieldAccessType.Read);
        history.Value.Should().Contain(access => access.IsSensitiveField); // Email read is sensitive
    }

    // ── Bulk export detection with threshold alerting (criterion 4) ───────────

    [Fact]
    public async Task BulkExportBelowTheThresholdIsRecordedWithoutAlert()
    {
        await using var context = CreateContext();
        var service = new BulkExportMonitoringService(context, AuditSettings(), TimeProvider.System);

        var evaluation = await service.EvaluateAndRecordAsync(TenantId, UserId, BulkExportScope.AuditLogExport, recordCount: 10);

        evaluation.ThresholdExceeded.Should().BeFalse();
        evaluation.ShouldAlert.Should().BeFalse();
        (await context.Set<SecurityAlert>().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task BulkExportAboveTheThresholdRaisesASecurityAlert()
    {
        await using var context = CreateContext();
        var service = new BulkExportMonitoringService(context, AuditSettings(), TimeProvider.System);

        var evaluation = await service.EvaluateAndRecordAsync(TenantId, UserId, BulkExportScope.AuditLogExport, recordCount: 11);

        evaluation.ThresholdExceeded.Should().BeTrue();
        evaluation.ShouldAlert.Should().BeTrue();
        evaluation.Blocked.Should().BeFalse(); // BlockExceedingExports defaults to false
        var alert = await context.Set<SecurityAlert>().SingleAsync();
        alert.SourceActionType.Should().Be(AuditActionTypes.BulkExportThresholdExceeded);
        var exports = await service.GetExportsAsync(TenantId, thresholdExceeded: true, skip: 0, take: 10);
        exports.Should().HaveCount(1);
    }

    [Fact]
    public async Task BulkExportCanBeBlockedWhenConfigured()
    {
        await using var context = CreateContext();
        var options = AuditSettings(new FieldLevelAuditOptions { MaxRecordsPerExport = 10, BlockExceedingExports = true });
        var service = new BulkExportMonitoringService(context, options, TimeProvider.System);

        var evaluation = await service.EvaluateAndRecordAsync(TenantId, UserId, BulkExportScope.AuditLogExport, recordCount: 11);

        evaluation.Blocked.Should().BeTrue();
    }

    // ── Cross-border transfer validation (criterion 9) ────────────────────────

    [Fact]
    public async Task EveryCrossBorderTransferEvaluationIsLoggedWithItsOutcome()
    {
        await using var context = CreateContext();
        var service = new CrossBorderTransferService(context, AuditSettings(), TimeProvider.System);

        var domestic = await service.ValidateAndLogAsync(TenantId, UserId, "br", "PII", 100, "support access");
        var allowed = await service.ValidateAndLogAsync(TenantId, UserId, "gb", "PII", 100, "support access");
        var blocked = await service.ValidateAndLogAsync(TenantId, UserId, "zz", "PII", 100, "support access");

        domestic.Status.Should().Be(CrossBorderTransferStatus.Allowed);
        domestic.IsCrossBorder.Should().BeFalse("a domestic transfer is not cross-border");
        allowed.Status.Should().Be(CrossBorderTransferStatus.Allowed);
        allowed.IsCrossBorder.Should().BeTrue();
        blocked.Status.Should().Be(CrossBorderTransferStatus.Blocked);
        var logs = await service.GetTransfersAsync(TenantId, null, skip: 0, take: 10);
        logs.Should().HaveCount(3);
        logs.Count(log => log.Status == CrossBorderTransferStatus.Blocked).Should().Be(1);
    }

    // ── GDPR Article 30 register (criterion 7) ────────────────────────────────

    [Fact]
    public async Task Article30SeedingDerivesThePlatformProcessingRegister()
    {
        await using var context = CreateContext();
        var service = new GdprArticle30Service(context, TimeProvider.System);

        var seeded = await service.SeedPlatformActivitiesAsync(TenantId);

        seeded.Should().BePositive();
        var activities = await service.GetActivitiesAsync(TenantId);
        activities.Should().OnlyContain(activity => activity.IsAutoDerived || activity.TenantId == TenantId);
        activities.Select(activity => activity.ActivityName).Should().Contain("Identity and authentication");
        var reviewGaps = await service.GetCompletenessGapsAsync(TenantId);
        reviewGaps.Should().BeEquivalentTo(activities.Select(activity => $"{activity.ActivityName}: never reviewed."),
            "seeding supplies the register fields but cannot claim a human review");
        foreach (var activity in activities)
        {
            await service.MarkReviewedAsync(activity.Id, UserId);
        }
        (await service.GetCompletenessGapsAsync(TenantId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Article30ManualEntriesSurviveReseedsAndRecordReviews()
    {
        await using var context = CreateContext();
        var service = new GdprArticle30Service(context, TimeProvider.System);
        await service.SeedPlatformActivitiesAsync(TenantId);
        var manual = GdprProcessingActivity.Create(
            TenantId, "Tenant CRM sync", "Synchronizing customer records", "consent",
            "Tenant customers", "name, email", "CRM provider", true, "CRM provider jurisdictions",
            "2 years", "encryption in transit", isAutoDerived: false, DateTime.UtcNow);
        await service.SaveActivityAsync(manual);

        await service.SeedPlatformActivitiesAsync(TenantId);
        var reviewer = Guid.NewGuid();
        var reviewed = await service.MarkReviewedAsync(manual.Id, reviewer);

        reviewed.Should().NotBeNull();
        reviewed!.IsAutoDerived.Should().BeFalse("manual entries must never be overwritten by reseeds");
        reviewed.LastReviewedByUserId.Should().Be(reviewer);
        (await service.GetActivitiesAsync(TenantId)).Should().Contain(activity => activity.Id == manual.Id);
    }

    // ── Data subject access tracking with consent monitoring (criterion 8) ────

    [Fact]
    public async Task DataSubjectMonitoringSurfacesRequestsDeadlinesAndConsentState()
    {
        await using var context = CreateContext();
        var service = new DataSubjectAccessMonitoringService(context);
        var subject = Guid.NewGuid();
        context.Set<UserConsent>().Add(new UserConsent
        {
            UserId = subject,
            PolicyVersionId = Guid.NewGuid(),
            IsGranted = true,
            ConsentGivenAt = DateTime.UtcNow.AddDays(-1),
            ConsentMethod = "banner"
        });
        context.Set<DataSubjectRequest>().Add(new DataSubjectRequest
        {
            UserId = subject,
            RequestType = DataSubjectRequestType.Access,
            Status = DataSubjectRequestStatus.Pending,
            Deadline = DateTime.UtcNow.AddDays(-2) // statutory window already elapsed
        });
        await context.SaveChangesAsync();

        var monitoring = await service.GetSubjectMonitoringAsync(subject);
        var summary = await service.GetSummaryAsync(TenantId);

        monitoring.GrantedCount.Should().Be(1);
        monitoring.HasOpenRequests.Should().BeTrue();
        monitoring.Requests.Should().ContainSingle().Which.IsOverdue.Should().BeTrue();
        summary.OverdueRequests.Should().Be(1);
        summary.ActiveConsents.Should().Be(1);
    }

    // ── Retention period enforcement (criterion 10) ───────────────────────────

    [Fact]
    public async Task RetentionEnforcementReportsDryRunsAndPurgesExpiredObservations()
    {
        var startTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var clock = new FrozenTimeProvider(startTime);
        await using var context = CreateContext();
        var fieldAccess = new FieldAccessAuditService(context, clock);
        var retention = new SensitiveDataRetentionService(context, AuditSettings(), clock);

        SystemClock.SetProvider(clock);
        try
        {
            // One sensitive observation (Email -> PII) and one public observation, both at T0.
            await fieldAccess.RecordFieldAccessAsync(
                TenantId, UserId, "User", Guid.NewGuid(), "Email", FieldAccessType.Read,
                null, "someone@example.com", isSensitive: false, sensitivityLevel: SensitivityLevel.Public,
                ipAddress: "10.0.0.1", userAgent: "test");
            await fieldAccess.RecordFieldAccessAsync(
                TenantId, UserId, "Invoice", Guid.NewGuid(), "Number", FieldAccessType.Read,
                null, "INV-1", isSensitive: false, sensitivityLevel: SensitivityLevel.Public,
                ipAddress: "10.0.0.1", userAgent: "test");

            await retention.ConfigureAsync(TenantId, SensitiveDataCategory.PII, retentionDays: 30,
                SensitiveDataRetentionAction.Purge, updatedByUserId: UserId);

            clock.Advance(TimeSpan.FromDays(45));

            var dryRun = await retention.EnforceAsync(TenantId, triggeredByUserId: UserId, dryRun: true);
            dryRun.DryRun.Should().BeTrue();
            dryRun.PurgedFieldAccessRecords.Should().Be(1);
            (await context.Set<FieldAccessAudit>().CountAsync()).Should().Be(2, "dry runs change nothing");

            var enforced = await retention.EnforceAsync(TenantId, triggeredByUserId: UserId, dryRun: false);
            enforced.PurgedFieldAccessRecords.Should().Be(1);
            var remaining = await context.Set<FieldAccessAudit>().ToListAsync();
            remaining.Should().ContainSingle(access => access.FieldName == "Number",
                "only the expired PII observation is purged; public observations follow the security log baseline");
        }
        finally
        {
            SystemClock.Reset();
        }
    }

    // ── Access-pattern anomaly analysis (criterion 3) ─────────────────────────

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PatternAnalysisFlagsUnusualVolumeAgainstTheBaseline(bool analyzeWholeTenant)
    {
        var startTime = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var clock = new FrozenTimeProvider(startTime);
        await using var context = CreateContext();
        var fieldAccess = new FieldAccessAuditService(context, clock);
        var analysis = new FieldAccessPatternAnalysisService(context, clock);

        SystemClock.SetProvider(clock);
        try
        {
            var entityId = Guid.NewGuid();
            for (var i = 0; i < 6; i++)
            {
                await fieldAccess.RecordFieldAccessAsync(
                    TenantId, UserId, "Invoice", entityId, "Number", FieldAccessType.Read,
                    null, null, isSensitive: false, sensitivityLevel: SensitivityLevel.Public,
                    ipAddress: "10.0.0.1", userAgent: "test");
            }

            clock.Advance(TimeSpan.FromHours(2));

            for (var i = 0; i < 30; i++)
            {
                await fieldAccess.RecordFieldAccessAsync(
                    TenantId, UserId, "Invoice", entityId, "Number", FieldAccessType.Read,
                    null, null, isSensitive: false, sensitivityLevel: SensitivityLevel.Public,
                    ipAddress: "10.0.0.1", userAgent: "test");
            }

            var result = analyzeWholeTenant
                ? (await analysis.AnalyzeTenantAsync(
                    TenantId,
                    windowStartUtc: startTime.AddHours(2), windowEndUtc: startTime.AddHours(3),
                    baselineStartUtc: startTime, baselineEndUtc: startTime.AddHours(1),
                    offHoursStartHour: 20, offHoursEndHour: 6)).Single()
                : await analysis.AnalyzeUserAsync(
                    TenantId, UserId,
                    windowStartUtc: startTime.AddHours(2), windowEndUtc: startTime.AddHours(3),
                    baselineStartUtc: startTime, baselineEndUtc: startTime.AddHours(1),
                    offHoursStartHour: 20, offHoursEndHour: 6);

            result.ObservedAccesses.Should().Be(30);
            result.BaselineAccesses.Should().Be(6);
            result.HasAnomalies.Should().BeTrue();
            result.Anomalies.Should().Contain(anomaly => anomaly.AnomalyType == FieldAccessAnomalyType.UnusualVolume);
            context.ChangeTracker.Clear();
            (await analysis.GetAnomaliesAsync(TenantId, FieldAccessAnomalyStatus.Open, skip: 0, take: 10))
                .Should().NotBeEmpty();
        }
        finally
        {
            SystemClock.Reset();
        }
    }

    // ── Batch read capture (criterion 1 read surface) ─────────────────────────

    [Fact]
    public async Task RecordingAFieldReadBatchPersistsClassifiedRowsWithoutValues()
    {
        await using var context = CreateContext();
        var service = new FieldAccessAuditService(context, TimeProvider.System);
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        var recorded = await service.RecordFieldReadsAsync(
            TenantId, UserId, "User",
            [
                new FieldReadObservation(userA, "Email", "a@example.com"),
                new FieldReadObservation(userA, "Email", "a@example.com"), // duplicates collapse to one row
                new FieldReadObservation(userA, "Bio", null), // non-sensitive fields are recorded too
                new FieldReadObservation(userB, "PhoneNumber", "+15550100")
            ],
            ipAddress: null, userAgent: null, requestId: "req-9", apiEndpoint: "/v1/users");

        recorded.Should().Be(3);
        var rows = await context.Set<FieldAccessAudit>().ToListAsync();
        rows.Should().HaveCount(3);
        rows.Should().OnlyContain(row => row.AccessType == FieldAccessType.Read);
        rows.Should().OnlyContain(row => row.RequestId == "req-9" && row.ApiEndpoint == "/v1/users");
        rows.Should().Contain(row => row.EntityId == userA && row.FieldName == "Email" && row.IsSensitiveField);
        rows.Should().Contain(row => row.EntityId == userB && row.FieldName == "PhoneNumber" && row.IsSensitiveField);
        // Read observations record the fact and context of the access, never field values.
        rows.Should().OnlyContain(row =>
            row.OldValue == null && row.NewValue == null
            && row.MaskedOldValue == null && row.MaskedNewValue == null);
    }

    // ── Harness ────────────────────────────────────────────────────────────────

    private static IOptions<FieldLevelAuditOptions> AuditSettings(FieldLevelAuditOptions? settings = null) =>
        Options.Create(settings ?? new FieldLevelAuditOptions { MaxRecordsPerExport = 10, MaxRecordsPerUserPerDay = 100 });

    private static FieldLevelAuditSqliteContext CreateContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<FieldLevelAuditSqliteContext>().UseSqlite(connection).Options;
        var context = new FieldLevelAuditSqliteContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class FieldLevelAuditSqliteContext(DbContextOptions<FieldLevelAuditSqliteContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Database.BeginTransactionAsync(cancellationToken);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FieldAccessAudit>(entity => entity.HasKey(access => access.Id));
            modelBuilder.Entity<SensitiveFieldClassification>(entity => entity.HasKey(classification => classification.Id));
            modelBuilder.Entity<FieldAccessAnomaly>(entity => entity.HasKey(anomaly => anomaly.Id));
            modelBuilder.Entity<BulkExportEvent>(entity => entity.HasKey(export => export.Id));
            modelBuilder.Entity<SecurityAlert>(entity => entity.HasKey(alert => alert.Id));
            modelBuilder.Entity<CrossBorderTransferLog>(entity => entity.HasKey(log => log.Id));
            modelBuilder.Entity<GdprProcessingActivity>(entity => entity.HasKey(activity => activity.Id));
            modelBuilder.Entity<SensitiveDataRetentionPolicy>(entity => entity.HasKey(policy => policy.Id));
            modelBuilder.Entity<UserConsent>(entity =>
            {
                entity.HasKey(consent => consent.Id);
                entity.Ignore(consent => consent.PolicyVersion);
            });
            modelBuilder.Entity<DataSubjectRequest>(entity => entity.HasKey(request => request.Id));
        }
    }

    /// <summary>A controllable time provider: the frozen instant advances explicitly per test phase.</summary>
    private sealed class FrozenTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }
}
