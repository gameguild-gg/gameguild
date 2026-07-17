using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Compliance.FERPA.UnitTests.Services;

public sealed class FerpaServiceTests
{
    private readonly Mock<IFerpaEducationRecordRepository> _records = new(MockBehavior.Strict);
    private readonly Mock<IFerpaDirectoryInformationPolicyRepository> _policies = new(MockBehavior.Strict);
    private readonly Mock<IFerpaDisclosureConsentRepository> _consents = new(MockBehavior.Strict);
    private readonly Mock<IFerpaDisclosureLogRepository> _logs = new(MockBehavior.Strict);
    private readonly Mock<IFerpaInspectionRequestRepository> _requests = new(MockBehavior.Strict);

    [Fact]
    public async Task RegisterEducationRecord_MapsEveryCommandFieldAndForwardsCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var tenantId = Guid.NewGuid();
        var retention = DateTime.UtcNow.AddYears(2);
        var command = new RegisterEducationRecordCommand(
            Guid.NewGuid(),
            EducationRecordKind.Grade,
            "grade-2026-final",
            "Final grade",
            FerpaRecordProtectionLevel.SensitiveEducationRecord,
            true,
            tenantId,
            retention,
            "{\"score\":98}");
        FerpaEducationRecord? persisted = null;
        _records.Setup(repository => repository.AddAsync(It.IsAny<FerpaEducationRecord>(), cancellation.Token))
            .Callback<FerpaEducationRecord, CancellationToken>((record, _) => persisted = record)
            .ReturnsAsync((FerpaEducationRecord record, CancellationToken _) => record);

        var result = await CreateSubject().RegisterEducationRecordAsync(command, cancellation.Token);

        persisted.Should().NotBeNull();
        persisted.Should().Match<FerpaEducationRecord>(record =>
            record.StudentUserId == command.StudentUserId &&
            record.TenantId == tenantId &&
            record.RecordKind == command.RecordKind &&
            record.ExternalRecordId == command.ExternalRecordId &&
            record.Title == command.Title &&
            record.ProtectionLevel == command.ProtectionLevel &&
            record.IsDirectoryInformation &&
            record.RetentionUntil == retention &&
            record.MetadataJson == command.MetadataJson);
        result.ExternalRecordId.Should().Be(command.ExternalRecordId);
    }

    [Fact]
    public async Task RecordQueries_MapDtosAndPreserveRepositoryOrdering()
    {
        var studentId = Guid.NewGuid();
        var first = new FerpaEducationRecord { StudentUserId = studentId, ExternalRecordId = "new", Title = "New" };
        var second = new FerpaEducationRecord { StudentUserId = studentId, ExternalRecordId = "old", Title = "Old" };
        _records.Setup(repository => repository.GetByStudentAsync(studentId, default)).ReturnsAsync([first, second]);
        _records.Setup(repository => repository.GetDirectoryInformationAsync(studentId, default)).ReturnsAsync([second]);

        var all = await CreateSubject().GetStudentRecordsAsync(studentId);
        var directory = await CreateSubject().GetDirectoryInformationAsync(studentId);

        all.Select(record => record.Id).Should().Equal(first.Id, second.Id);
        directory.Should().ContainSingle().Which.Id.Should().Be(second.Id);
    }

    [Fact]
    public async Task UpsertDirectoryPolicy_WhenMissingAddsTenantPolicy()
    {
        var command = new UpsertDirectoryInformationPolicyCommand(Guid.NewGuid(), "[\"name\"]", false, DateTime.UtcNow, "https://notice.test");
        _policies.Setup(repository => repository.GetByTenantAsync(command.TenantId, default)).ReturnsAsync((FerpaDirectoryInformationPolicy?)null);
        _policies.Setup(repository => repository.AddAsync(It.IsAny<FerpaDirectoryInformationPolicy>(), default))
            .ReturnsAsync((FerpaDirectoryInformationPolicy policy, CancellationToken _) => policy);

        var result = await CreateSubject().UpsertDirectoryPolicyAsync(command);

        result.Should().Match<FerpaDirectoryInformationPolicyDto>(policy =>
            policy.TenantId == command.TenantId &&
            policy.AllowedFieldsJson == command.AllowedFieldsJson &&
            policy.OptOutEnabled == command.OptOutEnabled &&
            policy.NoticeUrl == command.NoticeUrl);
        _policies.Verify(repository => repository.UpdateAsync(It.IsAny<FerpaDirectoryInformationPolicy>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpsertDirectoryPolicy_WhenPresentUpdatesSamePolicy()
    {
        var tenantId = Guid.NewGuid();
        var existing = new FerpaDirectoryInformationPolicy { TenantId = tenantId, AllowedFieldsJson = "[]" };
        var command = new UpsertDirectoryInformationPolicyCommand(tenantId, "[\"avatar\"]", false, DateTime.UtcNow, null);
        _policies.Setup(repository => repository.GetByTenantAsync(tenantId, default)).ReturnsAsync(existing);
        _policies.Setup(repository => repository.UpdateAsync(existing, default)).Returns(Task.CompletedTask);

        var result = await CreateSubject().UpsertDirectoryPolicyAsync(command);

        result.Id.Should().Be(existing.Id);
        result.AllowedFieldsJson.Should().Be(command.AllowedFieldsJson);
        result.OptOutEnabled.Should().BeFalse();
        _policies.Verify(repository => repository.AddAsync(It.IsAny<FerpaDirectoryInformationPolicy>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PolicyQuery_MapsExistingPolicyAndPreservesMissingResult()
    {
        var tenantId = Guid.NewGuid();
        var policy = new FerpaDirectoryInformationPolicy { TenantId = tenantId, AllowedFieldsJson = "[\"name\"]" };
        _policies.Setup(repository => repository.GetByTenantAsync(tenantId, default)).ReturnsAsync(policy);
        _policies.Setup(repository => repository.GetByTenantAsync(null, default)).ReturnsAsync((FerpaDirectoryInformationPolicy?)null);

        var existing = await CreateSubject().GetDirectoryPolicyAsync(tenantId);
        var missingGlobal = await CreateSubject().GetDirectoryPolicyAsync(null);

        existing!.Id.Should().Be(policy.Id);
        existing.AllowedFieldsJson.Should().Be(policy.AllowedFieldsJson);
        missingGlobal.Should().BeNull();
    }

    [Fact]
    public async Task GrantDisclosureConsent_MapsEveryCommandField()
    {
        var command = new GrantFerpaDisclosureConsentCommand(
            Guid.NewGuid(),
            "Scholarship Board",
            "Scholarship review",
            "grades",
            DateTime.UtcNow.AddHours(-1),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(30));
        _consents.Setup(repository => repository.AddAsync(It.IsAny<FerpaDisclosureConsent>(), default))
            .ReturnsAsync((FerpaDisclosureConsent consent, CancellationToken _) => consent);

        var result = await CreateSubject().GrantDisclosureConsentAsync(command);

        result.Should().Match<FerpaDisclosureConsentDto>(consent =>
            consent.StudentUserId == command.StudentUserId &&
            consent.GuardianUserId == command.GuardianUserId &&
            consent.Recipient == command.Recipient &&
            consent.Purpose == command.Purpose &&
            consent.Scope == command.Scope &&
            consent.EffectiveFrom == command.EffectiveFrom &&
            consent.ExpiresAt == command.ExpiresAt &&
            consent.IsActive);
    }

    [Theory]
    [InlineData(FerpaDisclosureBasis.StudentConsent)]
    [InlineData(FerpaDisclosureBasis.GuardianConsent)]
    public async Task RecordDisclosure_ConsentBasisWithoutMatchingActiveConsentIsRejected(FerpaDisclosureBasis basis)
    {
        var command = CreateDisclosureCommand(basis);
        _consents.Setup(repository => repository.GetActiveAsync(
                command.StudentUserId,
                command.Recipient,
                command.Scope,
                command.DisclosedAt,
                default))
            .ReturnsAsync((FerpaDisclosureConsent?)null);

        var action = () => CreateSubject().RecordDisclosureAsync(command);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*active matching consent*");
        _logs.Verify(repository => repository.AddAsync(It.IsAny<FerpaDisclosureLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordDisclosure_ActiveConsentPersistsAuditLogWithOriginalInstant()
    {
        var command = CreateDisclosureCommand(FerpaDisclosureBasis.StudentConsent);
        var consent = new FerpaDisclosureConsent
        {
            StudentUserId = command.StudentUserId,
            Recipient = command.Recipient,
            Scope = command.Scope,
            EffectiveFrom = command.DisclosedAt.AddDays(-1)
        };
        _consents.Setup(repository => repository.GetActiveAsync(
                command.StudentUserId,
                command.Recipient,
                command.Scope,
                command.DisclosedAt,
                default))
            .ReturnsAsync(consent);
        _logs.Setup(repository => repository.AddAsync(It.IsAny<FerpaDisclosureLog>(), default))
            .ReturnsAsync((FerpaDisclosureLog log, CancellationToken _) => log);

        var result = await CreateSubject().RecordDisclosureAsync(command);

        result.Should().Match<FerpaDisclosureLogDto>(log =>
            log.StudentUserId == command.StudentUserId &&
            log.DisclosedByUserId == command.DisclosedByUserId &&
            log.Recipient == command.Recipient &&
            log.Basis == command.Basis &&
            log.Purpose == command.Purpose &&
            log.RecordIdsJson == command.RecordIdsJson &&
            log.DisclosedAt == command.DisclosedAt);
    }

    [Fact]
    public async Task RecordDisclosure_StatutoryBasisDoesNotQueryConsent()
    {
        var command = CreateDisclosureCommand(FerpaDisclosureBasis.HealthOrSafetyEmergency);
        _logs.Setup(repository => repository.AddAsync(It.IsAny<FerpaDisclosureLog>(), default))
            .ReturnsAsync((FerpaDisclosureLog log, CancellationToken _) => log);

        var result = await CreateSubject().RecordDisclosureAsync(command);

        result.Basis.Should().Be(FerpaDisclosureBasis.HealthOrSafetyEmergency);
        _consents.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RevokeDisclosureConsent_WhenMissingReturnsFalseWithoutUpdate()
    {
        var consentId = Guid.NewGuid();
        _consents.Setup(repository => repository.GetByIdAsync(consentId, default)).ReturnsAsync((FerpaDisclosureConsent?)null);

        var result = await CreateSubject().RevokeDisclosureConsentAsync(consentId);

        result.Should().BeFalse();
        _consents.Verify(repository => repository.UpdateAsync(It.IsAny<FerpaDisclosureConsent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RevokeDisclosureConsent_WhenPresentRevokesAndPersists()
    {
        var consent = new FerpaDisclosureConsent
        {
            StudentUserId = Guid.NewGuid(),
            EffectiveFrom = DateTime.UtcNow.AddDays(-1)
        };
        _consents.Setup(repository => repository.GetByIdAsync(consent.Id, default)).ReturnsAsync(consent);
        _consents.Setup(repository => repository.UpdateAsync(consent, default)).Returns(Task.CompletedTask);

        var result = await CreateSubject().RevokeDisclosureConsentAsync(consent.Id);

        result.Should().BeTrue();
        consent.RevokedAt.Should().NotBeNull();
        _consents.VerifyAll();
    }

    [Fact]
    public async Task ConsentQuery_MapsDtosInRepositoryOrder()
    {
        var studentId = Guid.NewGuid();
        var newer = new FerpaDisclosureConsent { StudentUserId = studentId, Recipient = "New", EffectiveFrom = DateTime.UtcNow.AddDays(-1) };
        var older = new FerpaDisclosureConsent { StudentUserId = studentId, Recipient = "Old", EffectiveFrom = DateTime.UtcNow.AddDays(-2) };
        _consents.Setup(repository => repository.GetByStudentAsync(studentId, default)).ReturnsAsync([newer, older]);

        var result = await CreateSubject().GetStudentConsentsAsync(studentId);

        result.Select(consent => consent.Id).Should().Equal(newer.Id, older.Id);
    }

    [Fact]
    public async Task DisclosureLogQuery_MapsDtosInRepositoryOrder()
    {
        var studentId = Guid.NewGuid();
        var first = new FerpaDisclosureLog { StudentUserId = studentId, Recipient = "First" };
        var second = new FerpaDisclosureLog { StudentUserId = studentId, Recipient = "Second" };
        _logs.Setup(repository => repository.GetByStudentAsync(studentId, default)).ReturnsAsync([first, second]);

        var result = await CreateSubject().GetDisclosureLogsAsync(studentId);

        result.Select(log => log.Id).Should().Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task SubmitInspectionRequest_MapsRequesterDeadlineAndDescription()
    {
        var command = new SubmitFerpaInspectionRequestCommand(
            Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(45), "Inspect transcript");
        _requests.Setup(repository => repository.AddAsync(It.IsAny<FerpaInspectionRequest>(), default))
            .ReturnsAsync((FerpaInspectionRequest request, CancellationToken _) => request);

        var result = await CreateSubject().SubmitInspectionRequestAsync(command);

        result.Should().Match<FerpaInspectionRequestDto>(request =>
            request.StudentUserId == command.StudentUserId &&
            request.RequestedByUserId == command.RequestedByUserId &&
            request.Status == FerpaRequestStatus.Pending &&
            request.Deadline == command.Deadline &&
            request.ProcessingNotes == null);
    }

    [Theory]
    [InlineData(true, FerpaRequestStatus.Completed, null)]
    [InlineData(false, FerpaRequestStatus.Denied, "Denied")]
    public async Task CompleteInspectionRequest_AppliesDecisionAndPersists(bool approved, FerpaRequestStatus expectedStatus, string? defaultNotes)
    {
        var request = new FerpaInspectionRequest();
        var command = new CompleteFerpaInspectionRequestCommand(request.Id, Guid.NewGuid(), approved);
        _requests.Setup(repository => repository.GetByIdAsync(request.Id, default)).ReturnsAsync(request);
        _requests.Setup(repository => repository.UpdateAsync(request, default)).Returns(Task.CompletedTask);

        var result = await CreateSubject().CompleteInspectionRequestAsync(command);

        result.Status.Should().Be(expectedStatus);
        result.ProcessedByUserId.Should().Be(command.ProcessedByUserId);
        result.ProcessingNotes.Should().Be(defaultNotes);
    }

    [Fact]
    public async Task CompleteInspectionRequest_WhenMissingThrowsKeyNotFound()
    {
        var command = new CompleteFerpaInspectionRequestCommand(Guid.NewGuid(), Guid.NewGuid(), true);
        _requests.Setup(repository => repository.GetByIdAsync(command.RequestId, default)).ReturnsAsync((FerpaInspectionRequest?)null);

        var action = () => CreateSubject().CompleteInspectionRequestAsync(command);

        await action.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{command.RequestId}*");
    }

    [Fact]
    public async Task CompleteInspectionRequest_WhenAlreadyCompletedDoesNotPersistAnotherDecision()
    {
        var request = new FerpaInspectionRequest { Status = FerpaRequestStatus.Completed };
        var command = new CompleteFerpaInspectionRequestCommand(request.Id, Guid.NewGuid(), false, "Changed");
        _requests.Setup(repository => repository.GetByIdAsync(request.Id, default)).ReturnsAsync(request);

        var action = () => CreateSubject().CompleteInspectionRequestAsync(command);

        await action.Should().ThrowAsync<InvalidOperationException>();
        _requests.Verify(repository => repository.UpdateAsync(It.IsAny<FerpaInspectionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PendingInspectionQuery_MapsDtosInDeadlineOrderFromRepository()
    {
        var early = new FerpaInspectionRequest { Deadline = DateTime.UtcNow.AddDays(5) };
        var late = new FerpaInspectionRequest { Deadline = DateTime.UtcNow.AddDays(20) };
        _requests.Setup(repository => repository.GetPendingAsync(default)).ReturnsAsync([early, late]);

        var result = await CreateSubject().GetPendingInspectionRequestsAsync();

        result.Select(request => request.Id).Should().Equal(early.Id, late.Id);
    }

    private FerpaService CreateSubject() => new(
        _records.Object,
        _policies.Object,
        _consents.Object,
        _logs.Object,
        _requests.Object);

    private static RecordFerpaDisclosureCommand CreateDisclosureCommand(FerpaDisclosureBasis basis) => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "Accreditation Board",
        basis,
        "Program accreditation",
        "grades",
        "[\"grade-1\"]",
        new DateTime(2026, 7, 17, 10, 0, 0, DateTimeKind.Utc));
}
