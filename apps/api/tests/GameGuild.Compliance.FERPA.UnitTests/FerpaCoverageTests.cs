using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GameGuild.Compliance.FERPA.UnitTests;

public sealed class FerpaCoverageTests
{
    [Fact]
    public async Task FerpaService_ShouldCover_RecordPolicyConsentDisclosureAndInspectionFlows()
    {
        var records = new MemoryRecordRepository();
        var policies = new MemoryPolicyRepository();
        var consents = new MemoryConsentRepository();
        var logs = new MemoryLogRepository();
        var requests = new MemoryInspectionRepository();
        var service = new FerpaService(records, policies, consents, logs, requests);
        var tenantId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var record = await service.RegisterEducationRecordAsync(new RegisterEducationRecordCommand(
            studentId,
            EducationRecordKind.Grade,
            "grade-1",
            "Final Grade",
            FerpaRecordProtectionLevel.SensitiveEducationRecord,
            true,
            tenantId,
            now.AddYears(1),
            "{\"score\":100}"));

        record.StudentUserId.Should().Be(studentId);
        (await service.GetStudentRecordsAsync(studentId)).Should().ContainSingle();
        (await service.GetDirectoryInformationAsync(studentId)).Should().ContainSingle();

        var createdPolicy = await service.UpsertDirectoryPolicyAsync(new UpsertDirectoryInformationPolicyCommand(
            tenantId,
            "[\"displayName\"]",
            true,
            now,
            "https://example.test/notice"));
        var updatedPolicy = await service.UpsertDirectoryPolicyAsync(new UpsertDirectoryInformationPolicyCommand(
            tenantId,
            "[\"displayName\",\"avatar\"]",
            false,
            now.AddDays(1),
            null));

        createdPolicy.TenantId.Should().Be(tenantId);
        updatedPolicy.OptOutEnabled.Should().BeFalse();
        (await service.GetDirectoryPolicyAsync(tenantId))!.AllowedFieldsJson.Should().Contain("avatar");
        (await service.GetDirectoryPolicyAsync(Guid.NewGuid())).Should().BeNull();

        var consent = await service.GrantDisclosureConsentAsync(new GrantFerpaDisclosureConsentCommand(
            studentId,
            "Scholarship Board",
            "Scholarship review",
            "grades",
            now.AddMinutes(-5),
            GuardianUserId: Guid.NewGuid(),
            ExpiresAt: now.AddDays(5)));
        consent.IsActive.Should().BeTrue();
        (await service.GetStudentConsentsAsync(studentId)).Should().ContainSingle();

        var consentLog = await service.RecordDisclosureAsync(new RecordFerpaDisclosureCommand(
            studentId,
            adminId,
            "Scholarship Board",
            FerpaDisclosureBasis.StudentConsent,
            "Scholarship review",
            "grades",
            $"[\"{record.Id}\"]",
            now));
        consentLog.Basis.Should().Be(FerpaDisclosureBasis.StudentConsent);

        await service.Invoking(current => current.RecordDisclosureAsync(new RecordFerpaDisclosureCommand(
                studentId,
                adminId,
                "No Consent",
                FerpaDisclosureBasis.GuardianConsent,
                "Review",
                "grades",
                "[]",
                now)))
            .Should().ThrowAsync<InvalidOperationException>();

        var officialLog = await service.RecordDisclosureAsync(new RecordFerpaDisclosureCommand(
            studentId,
            adminId,
            "Registrar",
            FerpaDisclosureBasis.SchoolOfficial,
            "Academic support",
            "records",
            "[]",
            now));
        officialLog.Recipient.Should().Be("Registrar");
        (await service.GetDisclosureLogsAsync(studentId)).Should().HaveCount(2);

        (await service.RevokeDisclosureConsentAsync(Guid.NewGuid())).Should().BeFalse();
        (await service.RevokeDisclosureConsentAsync(consent.Id)).Should().BeTrue();
        consents.Items.Single().RevokedAt.Should().NotBeNull();

        var request = await service.SubmitInspectionRequestAsync(new SubmitFerpaInspectionRequestCommand(
            studentId,
            studentId,
            now.AddDays(45),
            "Inspect records"));
        request.Status.Should().Be(FerpaRequestStatus.Pending);
        (await service.GetPendingInspectionRequestsAsync()).Should().ContainSingle();

        var completed = await service.CompleteInspectionRequestAsync(new CompleteFerpaInspectionRequestCommand(
            request.Id,
            adminId,
            true,
            "Ready"));
        completed.Status.Should().Be(FerpaRequestStatus.Completed);

        var deniedRequest = await service.SubmitInspectionRequestAsync(new SubmitFerpaInspectionRequestCommand(
            Guid.NewGuid(),
            studentId,
            now.AddDays(45)));
        var denied = await service.CompleteInspectionRequestAsync(new CompleteFerpaInspectionRequestCommand(
            deniedRequest.Id,
            adminId,
            false,
            null));
        denied.Status.Should().Be(FerpaRequestStatus.Denied);
        denied.ProcessingNotes.Should().Be("Denied");

        await service.Invoking(current => current.CompleteInspectionRequestAsync(new CompleteFerpaInspectionRequestCommand(
                Guid.NewGuid(),
                adminId,
                true)))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task FerpaRepositories_ShouldCover_EfQueriesAndMutations()
    {
        await using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var recordRepository = new FerpaEducationRecordRepository(db);
        var policyRepository = new FerpaDirectoryInformationPolicyRepository(db);
        var consentRepository = new FerpaDisclosureConsentRepository(db);
        var logRepository = new FerpaDisclosureLogRepository(db);
        var requestRepository = new FerpaInspectionRequestRepository(db);
        var now = DateTime.UtcNow;

        var record = await recordRepository.AddAsync(new FerpaEducationRecord
        {
            StudentUserId = studentId,
            TenantId = tenantId,
            RecordKind = EducationRecordKind.AssessmentSubmission,
            ExternalRecordId = "assessment-1",
            Title = "Assessment",
            IsDirectoryInformation = true
        });
        await recordRepository.AddAsync(new FerpaEducationRecord
        {
            StudentUserId = Guid.NewGuid(),
            RecordKind = EducationRecordKind.Custom,
            ExternalRecordId = "other",
            Title = "Other"
        });

        (await recordRepository.GetByStudentAsync(studentId)).Should().ContainSingle().Which.Id.Should().Be(record.Id);
        (await recordRepository.GetDirectoryInformationAsync(studentId)).Should().ContainSingle();

        var policy = await policyRepository.AddAsync(new FerpaDirectoryInformationPolicy { TenantId = tenantId, AllowedFieldsJson = "[]" });
        (await policyRepository.GetByTenantAsync(tenantId))!.Id.Should().Be(policy.Id);
        (await policyRepository.GetByTenantAsync(null)).Should().BeNull();
        policy.Update("[\"handle\"]", false, now, null);
        await policyRepository.UpdateAsync(policy);

        var activeConsent = await consentRepository.AddAsync(new FerpaDisclosureConsent
        {
            StudentUserId = studentId,
            Recipient = "Registrar",
            Scope = "records",
            Purpose = "support",
            EffectiveFrom = now.AddDays(-1),
            ExpiresAt = now.AddDays(1)
        });
        await consentRepository.AddAsync(new FerpaDisclosureConsent
        {
            StudentUserId = studentId,
            Recipient = "Expired",
            Scope = "records",
            Purpose = "support",
            EffectiveFrom = now.AddDays(-10),
            ExpiresAt = now.AddDays(-1)
        });
        (await consentRepository.GetByIdAsync(activeConsent.Id))!.Recipient.Should().Be("Registrar");
        (await consentRepository.GetByIdAsync(Guid.NewGuid())).Should().BeNull();
        (await consentRepository.GetActiveAsync(studentId, "Registrar", "records", now)).Should().NotBeNull();
        (await consentRepository.GetActiveAsync(studentId, "Expired", "records", now)).Should().BeNull();
        (await consentRepository.GetByStudentAsync(studentId)).Should().HaveCount(2);
        activeConsent.Revoke();
        await consentRepository.UpdateAsync(activeConsent);

        var log = await logRepository.AddAsync(new FerpaDisclosureLog
        {
            StudentUserId = studentId,
            DisclosedByUserId = Guid.NewGuid(),
            Recipient = "Registrar",
            Basis = FerpaDisclosureBasis.SchoolOfficial,
            Purpose = "support"
        });
        (await logRepository.GetByStudentAsync(studentId)).Should().ContainSingle().Which.Id.Should().Be(log.Id);

        var request = await requestRepository.AddAsync(new FerpaInspectionRequest
        {
            StudentUserId = studentId,
            RequestedByUserId = studentId
        });
        await requestRepository.AddAsync(new FerpaInspectionRequest
        {
            StudentUserId = studentId,
            RequestedByUserId = studentId,
            Status = FerpaRequestStatus.InReview
        });
        (await requestRepository.GetByIdAsync(request.Id))!.Id.Should().Be(request.Id);
        (await requestRepository.GetByIdAsync(Guid.NewGuid())).Should().BeNull();
        (await requestRepository.GetPendingAsync()).Should().ContainSingle().Which.Id.Should().Be(request.Id);
        request.Complete(Guid.NewGuid(), "done");
        await requestRepository.UpdateAsync(request);
    }

    [Fact]
    public async Task CqrsHandlersAndController_ShouldDelegateThroughSender()
    {
        var service = new Mock<IFerpaService>();
        var sender = new Mock<ISender>();
        var studentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var record = new FerpaEducationRecordDto(Guid.NewGuid(), studentId, EducationRecordKind.Grade, "g-1", "Grade", FerpaRecordProtectionLevel.EducationRecord, false, null, "{}", DateTime.UtcNow);
        var policy = new FerpaDirectoryInformationPolicyDto(Guid.NewGuid(), tenantId, "[]", true, null, null);
        var consent = new FerpaDisclosureConsentDto(Guid.NewGuid(), studentId, null, "Registrar", "support", "records", DateTime.UtcNow, null, null, true);
        var log = new FerpaDisclosureLogDto(Guid.NewGuid(), studentId, userId, "Registrar", FerpaDisclosureBasis.SchoolOfficial, "support", "[]", DateTime.UtcNow);
        var request = new FerpaInspectionRequestDto(Guid.NewGuid(), studentId, studentId, FerpaRequestStatus.Pending, DateTime.UtcNow.AddDays(45), null, null, null);

        service.Setup(current => current.RegisterEducationRecordAsync(It.IsAny<RegisterEducationRecordCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(record);
        service.Setup(current => current.UpsertDirectoryPolicyAsync(It.IsAny<UpsertDirectoryInformationPolicyCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        service.Setup(current => current.GrantDisclosureConsentAsync(It.IsAny<GrantFerpaDisclosureConsentCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(consent);
        service.Setup(current => current.RevokeDisclosureConsentAsync(consent.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        service.Setup(current => current.RecordDisclosureAsync(It.IsAny<RecordFerpaDisclosureCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(log);
        service.Setup(current => current.SubmitInspectionRequestAsync(It.IsAny<SubmitFerpaInspectionRequestCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(request);
        service.Setup(current => current.CompleteInspectionRequestAsync(It.IsAny<CompleteFerpaInspectionRequestCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(request);
        service.Setup(current => current.GetStudentRecordsAsync(studentId, It.IsAny<CancellationToken>())).ReturnsAsync([record]);
        service.Setup(current => current.GetDirectoryInformationAsync(studentId, It.IsAny<CancellationToken>())).ReturnsAsync([record]);
        service.Setup(current => current.GetDirectoryPolicyAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        service.Setup(current => current.GetStudentConsentsAsync(studentId, It.IsAny<CancellationToken>())).ReturnsAsync([consent]);
        service.Setup(current => current.GetDisclosureLogsAsync(studentId, It.IsAny<CancellationToken>())).ReturnsAsync([log]);
        service.Setup(current => current.GetPendingInspectionRequestsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([request]);

        (await new RegisterEducationRecordCommandHandler(service.Object).Handle(new RegisterEducationRecordCommand(studentId, EducationRecordKind.Grade, "g-1", "Grade"), CancellationToken.None)).Should().Be(record);
        (await new UpsertDirectoryInformationPolicyCommandHandler(service.Object).Handle(new UpsertDirectoryInformationPolicyCommand(tenantId, "[]"), CancellationToken.None)).Should().Be(policy);
        (await new GrantFerpaDisclosureConsentCommandHandler(service.Object).Handle(new GrantFerpaDisclosureConsentCommand(studentId, "Registrar", "support", "records", DateTime.UtcNow), CancellationToken.None)).Should().Be(consent);
        (await new RevokeFerpaDisclosureConsentCommandHandler(service.Object).Handle(new RevokeFerpaDisclosureConsentCommand(consent.Id), CancellationToken.None)).Should().BeTrue();
        (await new RecordFerpaDisclosureCommandHandler(service.Object).Handle(new RecordFerpaDisclosureCommand(studentId, userId, "Registrar", FerpaDisclosureBasis.SchoolOfficial, "support", "records", "[]", DateTime.UtcNow), CancellationToken.None)).Should().Be(log);
        (await new SubmitFerpaInspectionRequestCommandHandler(service.Object).Handle(new SubmitFerpaInspectionRequestCommand(studentId, studentId, DateTime.UtcNow.AddDays(45)), CancellationToken.None)).Should().Be(request);
        (await new CompleteFerpaInspectionRequestCommandHandler(service.Object).Handle(new CompleteFerpaInspectionRequestCommand(request.Id, userId, true), CancellationToken.None)).Should().Be(request);
        (await new GetStudentEducationRecordsQueryHandler(service.Object).Handle(new GetStudentEducationRecordsQuery(studentId), CancellationToken.None)).Should().ContainSingle();
        (await new GetStudentDirectoryInformationQueryHandler(service.Object).Handle(new GetStudentDirectoryInformationQuery(studentId), CancellationToken.None)).Should().ContainSingle();
        (await new GetDirectoryInformationPolicyQueryHandler(service.Object).Handle(new GetDirectoryInformationPolicyQuery(tenantId), CancellationToken.None)).Should().Be(policy);
        (await new GetStudentFerpaConsentsQueryHandler(service.Object).Handle(new GetStudentFerpaConsentsQuery(studentId), CancellationToken.None)).Should().ContainSingle();
        (await new GetStudentFerpaDisclosureLogsQueryHandler(service.Object).Handle(new GetStudentFerpaDisclosureLogsQuery(studentId), CancellationToken.None)).Should().ContainSingle();
        (await new GetPendingFerpaInspectionRequestsQueryHandler(service.Object).Handle(new GetPendingFerpaInspectionRequestsQuery(), CancellationToken.None)).Should().ContainSingle();

        sender.Setup(current => current.Send(It.IsAny<GetStudentEducationRecordsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([record]);
        sender.Setup(current => current.Send(It.IsAny<GetStudentDirectoryInformationQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([record]);
        sender.Setup(current => current.Send(It.IsAny<RegisterEducationRecordCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(record);
        sender.Setup(current => current.Send(It.IsAny<GetDirectoryInformationPolicyQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        sender.Setup(current => current.Send(It.IsAny<UpsertDirectoryInformationPolicyCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        sender.Setup(current => current.Send(It.IsAny<GetStudentFerpaConsentsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([consent]);
        sender.Setup(current => current.Send(It.IsAny<GrantFerpaDisclosureConsentCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(consent);
        sender.Setup(current => current.Send(new RevokeFerpaDisclosureConsentCommand(consent.Id), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        sender.Setup(current => current.Send(new RevokeFerpaDisclosureConsentCommand(Guid.Empty), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        sender.Setup(current => current.Send(It.IsAny<RecordFerpaDisclosureCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(log);
        sender.Setup(current => current.Send(It.IsAny<GetStudentFerpaDisclosureLogsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([log]);
        sender.Setup(current => current.Send(It.IsAny<SubmitFerpaInspectionRequestCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(request);
        sender.Setup(current => current.Send(It.IsAny<CompleteFerpaInspectionRequestCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(request);
        sender.Setup(current => current.Send(It.IsAny<GetPendingFerpaInspectionRequestsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([request]);
        var controller = new FerpaController(sender.Object);

        (await controller.GetStudentRecords(studentId, CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.GetDirectoryInformation(studentId, CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.RegisterRecord(new RegisterEducationRecordCommand(studentId, EducationRecordKind.Grade, "g-1", "Grade"), CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.GetDirectoryPolicy(tenantId, CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.UpsertDirectoryPolicy(new UpsertDirectoryInformationPolicyCommand(tenantId, "[]"), CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.GetConsents(studentId, CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.GrantConsent(new GrantFerpaDisclosureConsentCommand(studentId, "Registrar", "support", "records", DateTime.UtcNow), CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.RevokeConsent(consent.Id, CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.RevokeConsent(Guid.Empty, CancellationToken.None)).Should().BeOfType<NotFoundResult>();
        (await controller.RecordDisclosure(new RecordFerpaDisclosureCommand(studentId, userId, "Registrar", FerpaDisclosureBasis.SchoolOfficial, "support", "records", "[]", DateTime.UtcNow), CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.GetDisclosures(studentId, CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.SubmitInspectionRequest(new SubmitFerpaInspectionRequestCommand(studentId, studentId, DateTime.UtcNow.AddDays(45)), CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.CompleteInspectionRequest(request.Id, new CompleteFerpaInspectionRequestBody(userId, true, "done"), CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.GetPendingInspectionRequests(CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void EntitiesConfigurationAndDependencyInjection_ShouldCoverRemainingMembers()
    {
        var now = DateTime.UtcNow;
        var consent = new FerpaDisclosureConsent
        {
            EffectiveFrom = now.AddDays(-1),
            ExpiresAt = now.AddDays(1)
        };
        consent.IsActiveAt(now).Should().BeTrue();
        consent.ExpiresAt = null;
        consent.IsActiveAt(now).Should().BeTrue();
        consent.EffectiveFrom = now.AddDays(1);
        consent.IsActiveAt(now).Should().BeFalse();
        consent.EffectiveFrom = now.AddDays(-1);
        consent.ExpiresAt = now.AddDays(1);
        consent.IsActiveAt(now.AddDays(2)).Should().BeFalse();
        consent.Revoke();
        consent.Revoke();
        consent.IsActiveAt(now).Should().BeFalse();

        var request = new FerpaInspectionRequest();
        request.Complete(Guid.NewGuid(), "complete");
        request.Status.Should().Be(FerpaRequestStatus.Completed);
        request.Deny(Guid.NewGuid(), "deny");
        request.Status.Should().Be(FerpaRequestStatus.Denied);

        var policy = new FerpaDirectoryInformationPolicy();
        policy.Update("[\"name\"]", false, now, "https://example.test");
        policy.ToDto().AllowedFieldsJson.Should().Contain("name");

        var modelBuilder = new ModelBuilder();
        new FerpaModelConfiguration().Configure(modelBuilder);
        modelBuilder.Model.FindEntityType(typeof(FerpaEducationRecord))!.FindProperty(nameof(FerpaEducationRecord.Title))!.GetMaxLength().Should().Be(300);
        modelBuilder.Model.FindEntityType(typeof(FerpaDirectoryInformationPolicy))!.FindProperty(nameof(FerpaDirectoryInformationPolicy.NoticeUrl))!.GetMaxLength().Should().Be(500);
        modelBuilder.Model.FindEntityType(typeof(FerpaDisclosureConsent))!.FindProperty(nameof(FerpaDisclosureConsent.Scope))!.GetMaxLength().Should().Be(500);
        modelBuilder.Model.FindEntityType(typeof(FerpaDisclosureLog))!.FindProperty(nameof(FerpaDisclosureLog.Basis))!.GetMaxLength().Should().Be(80);
        modelBuilder.Model.FindEntityType(typeof(FerpaInspectionRequest))!.FindProperty(nameof(FerpaInspectionRequest.Status))!.GetMaxLength().Should().Be(80);

        var services = new ServiceCollection();
        services.AddFerpaModule().Should().BeSameAs(services);
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IFerpaService) && descriptor.ImplementationType == typeof(FerpaService));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IFerpaEducationRecordRepository) && descriptor.ImplementationType == typeof(FerpaEducationRecordRepository));

        var module = new FerpaModule();
        module.Name.Should().Be("FERPA");
        module.Order.Should().Be(95);
        module.ConfigureServices(new ServiceCollection(), new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build()).Should().NotBeNull();
        var endpoints = new Mock<Microsoft.AspNetCore.Routing.IEndpointRouteBuilder>().Object;
        module.MapEndpoints(endpoints).Should().BeSameAs(endpoints);
    }

    private static FerpaTestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FerpaTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new FerpaTestDbContext(options);
    }

    private sealed class FerpaTestDbContext(DbContextOptions<FerpaTestDbContext> options) : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new FerpaEducationRecordConfiguration().Configure(modelBuilder.Entity<FerpaEducationRecord>());
            new FerpaDirectoryInformationPolicyConfiguration().Configure(modelBuilder.Entity<FerpaDirectoryInformationPolicy>());
            new FerpaDisclosureConsentConfiguration().Configure(modelBuilder.Entity<FerpaDisclosureConsent>());
            new FerpaDisclosureLogConfiguration().Configure(modelBuilder.Entity<FerpaDisclosureLog>());
            new FerpaInspectionRequestConfiguration().Configure(modelBuilder.Entity<FerpaInspectionRequest>());
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class MemoryRecordRepository : IFerpaEducationRecordRepository
    {
        public List<FerpaEducationRecord> Items { get; } = [];
        public Task<FerpaEducationRecord> AddAsync(FerpaEducationRecord record, CancellationToken ct = default)
        {
            Items.Add(record);
            return Task.FromResult(record);
        }

        public Task<List<FerpaEducationRecord>> GetByStudentAsync(Guid studentUserId, CancellationToken ct = default)
            => Task.FromResult(Items.Where(item => item.StudentUserId == studentUserId && !item.IsDeleted).ToList());

        public Task<List<FerpaEducationRecord>> GetDirectoryInformationAsync(Guid studentUserId, CancellationToken ct = default)
            => Task.FromResult(Items.Where(item => item.StudentUserId == studentUserId && item.IsDirectoryInformation && !item.IsDeleted).ToList());
    }

    private sealed class MemoryPolicyRepository : IFerpaDirectoryInformationPolicyRepository
    {
        public List<FerpaDirectoryInformationPolicy> Items { get; } = [];
        public Task<FerpaDirectoryInformationPolicy?> GetByTenantAsync(Guid? tenantId, CancellationToken ct = default)
            => Task.FromResult(Items.FirstOrDefault(item => item.TenantId == tenantId && !item.IsDeleted));

        public Task<FerpaDirectoryInformationPolicy> AddAsync(FerpaDirectoryInformationPolicy policy, CancellationToken ct = default)
        {
            Items.Add(policy);
            return Task.FromResult(policy);
        }

        public Task UpdateAsync(FerpaDirectoryInformationPolicy policy, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class MemoryConsentRepository : IFerpaDisclosureConsentRepository
    {
        public List<FerpaDisclosureConsent> Items { get; } = [];
        public Task<FerpaDisclosureConsent> AddAsync(FerpaDisclosureConsent consent, CancellationToken ct = default)
        {
            Items.Add(consent);
            return Task.FromResult(consent);
        }

        public Task<FerpaDisclosureConsent?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(Items.FirstOrDefault(item => item.Id == id && !item.IsDeleted));

        public Task<FerpaDisclosureConsent?> GetActiveAsync(Guid studentUserId, string recipient, string scope, DateTime instant, CancellationToken ct = default)
            => Task.FromResult(Items.FirstOrDefault(item => item.StudentUserId == studentUserId && item.Recipient == recipient && item.Scope == scope && item.IsActiveAt(instant) && !item.IsDeleted));

        public Task<List<FerpaDisclosureConsent>> GetByStudentAsync(Guid studentUserId, CancellationToken ct = default)
            => Task.FromResult(Items.Where(item => item.StudentUserId == studentUserId && !item.IsDeleted).ToList());

        public Task UpdateAsync(FerpaDisclosureConsent consent, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class MemoryLogRepository : IFerpaDisclosureLogRepository
    {
        public List<FerpaDisclosureLog> Items { get; } = [];
        public Task<FerpaDisclosureLog> AddAsync(FerpaDisclosureLog log, CancellationToken ct = default)
        {
            Items.Add(log);
            return Task.FromResult(log);
        }

        public Task<List<FerpaDisclosureLog>> GetByStudentAsync(Guid studentUserId, CancellationToken ct = default)
            => Task.FromResult(Items.Where(item => item.StudentUserId == studentUserId && !item.IsDeleted).ToList());
    }

    private sealed class MemoryInspectionRepository : IFerpaInspectionRequestRepository
    {
        public List<FerpaInspectionRequest> Items { get; } = [];
        public Task<FerpaInspectionRequest> AddAsync(FerpaInspectionRequest request, CancellationToken ct = default)
        {
            Items.Add(request);
            return Task.FromResult(request);
        }

        public Task<FerpaInspectionRequest?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(Items.FirstOrDefault(item => item.Id == id && !item.IsDeleted));

        public Task<List<FerpaInspectionRequest>> GetPendingAsync(CancellationToken ct = default)
            => Task.FromResult(Items.Where(item => item.Status == FerpaRequestStatus.Pending && !item.IsDeleted).ToList());

        public Task UpdateAsync(FerpaInspectionRequest request, CancellationToken ct = default) => Task.CompletedTask;
    }
}
