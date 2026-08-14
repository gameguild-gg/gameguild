using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Compliance.Consent.Tests;

public sealed class ConsentCoverageCompletionTests
{
    [Fact]
    public async Task ConsentService_ShouldCover_AllWorkflowBranches()
    {
        var policies = new MemoryConsentPolicyRepository();
        var versions = new MemoryPolicyVersionRepository();
        var consents = new MemoryUserConsentRepository();
        var requests = new MemoryDataSubjectRequestRepository();
        var service = new ConsentService(policies, versions, consents, requests);
        var userId = Guid.NewGuid();
        var processedBy = Guid.NewGuid();

        var policy = await service.CreatePolicyAsync("Privacy", PolicyType.PrivacyPolicy, true, "Primary privacy policy");
        var firstVersion = await service.PublishVersionAsync(policy.Id, "1.0", "Initial", ContentType.Markdown);
        var secondVersion = await service.PublishVersionAsync(policy.Id, "2.0", "<p>Updated</p>", ContentType.Html);
        policies.Items.Single().Versions = versions.Items;
        var activePolicies = await service.GetActivePoliciesAsync(null);

        policy.Name.Should().Be("Privacy");
        firstVersion.IsCurrent.Should().BeTrue();
        versions.Items.Single(version => version.Id == firstVersion.Id).IsCurrent.Should().BeFalse();
        versions.Items.Single(version => version.Id == firstVersion.Id).EffectiveUntil.Should().NotBeNull();
        secondVersion.ContentType.Should().Be(ContentType.Html);
        activePolicies.Should().ContainSingle().Which.CurrentVersion.Should().Be("2.0");
        policies.Items.Add(new ConsentPolicy { Id = Guid.NewGuid(), Name = "No versions", PolicyType = PolicyType.Custom });
        policies.Items.Add(new ConsentPolicy { Id = Guid.NewGuid(), Name = "Null versions", PolicyType = PolicyType.Custom, Versions = null! });
        var policiesWithoutCurrentVersion = (await service.GetActivePoliciesAsync(null))
            .Where(current => current.Name is "No versions" or "Null versions")
            .ToList();
        policiesWithoutCurrentVersion.Count(current => current.CurrentVersion is null).Should().Be(2);

        var granted = await service.GrantConsentAsync(userId, secondVersion.Id, "127.0.0.1", "agent", "registration");
        var existingGranted = await service.GrantConsentAsync(userId, secondVersion.Id, null, null, null);
        granted.Id.Should().Be(existingGranted.Id);

        consents.Items.Single().IsGranted = false;
        var regranted = await service.GrantConsentAsync(userId, secondVersion.Id, null, null, "settings");
        regranted.ConsentMethod.Should().Be("settings");

        await service.RevokeConsentAsync(Guid.NewGuid(), secondVersion.Id);
        await service.RevokeConsentAsync(userId, secondVersion.Id);
        consents.Items.Last().IsGranted.Should().BeFalse();

        var userConsents = await service.GetUserConsentsAsync(userId);
        userConsents.Should().HaveCount(2);

        var submitted = await service.SubmitDataSubjectRequestAsync(userId, DataSubjectRequestType.Access, "export my data");
        submitted.Status.Should().Be(DataSubjectRequestStatus.Pending);
        submitted.Deadline.Should().BeAfter(DateTime.UtcNow.AddDays(29));

        var processed = await service.ProcessDataSubjectRequestAsync(submitted.Id, processedBy, "done");
        processed.Status.Should().Be(DataSubjectRequestStatus.Completed);
        processed.ProcessedAt.Should().NotBeNull();
        processed.ProcessingNotes.Should().Be("done");

        await service.Invoking(current => current.ProcessDataSubjectRequestAsync(Guid.NewGuid(), processedBy, null))
            .Should().ThrowAsync<KeyNotFoundException>();

        await service.SubmitDataSubjectRequestAsync(Guid.NewGuid(), DataSubjectRequestType.Erasure, null);
        (await service.GetPendingRequestsAsync()).Should().ContainSingle();
    }

    [Fact]
    public async Task ConsentRepositories_ShouldCover_AllQueriesAndMutations()
    {
        await using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var policyRepository = new ConsentPolicyRepository(db);
        var versionRepository = new PolicyVersionRepository(db);
        var consentRepository = new UserConsentRepository(db);
        var requestRepository = new DataSubjectRequestRepository(db);
        var policy = await policyRepository.AddAsync(new ConsentPolicy
        {
            Id = Guid.NewGuid(),
            Name = "Privacy",
            PolicyType = PolicyType.PrivacyPolicy,
            IsMandatory = true,
            TenantId = tenantId
        });
        await policyRepository.AddAsync(new ConsentPolicy
        {
            Id = Guid.NewGuid(),
            Name = "Inactive",
            PolicyType = PolicyType.Custom,
            IsActive = false,
            TenantId = tenantId
        });
        await policyRepository.AddAsync(new ConsentPolicy
        {
            Id = Guid.NewGuid(),
            Name = "Other",
            PolicyType = PolicyType.Custom,
            TenantId = otherTenantId
        });
        db.Set<ConsentPolicy>().Add(new ConsentPolicy
        {
            Id = Guid.NewGuid(),
            Name = "Deleted",
            PolicyType = PolicyType.Custom,
            DeletedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var version = await versionRepository.AddAsync(new PolicyVersion
        {
            Id = Guid.NewGuid(),
            ConsentPolicyId = policy.Id,
            VersionNumber = "1.0",
            Content = "Policy",
            ContentType = ContentType.PlainText,
            IsCurrent = true
        });
        await versionRepository.AddAsync(new PolicyVersion
        {
            Id = Guid.NewGuid(),
            ConsentPolicyId = policy.Id,
            VersionNumber = "0.9",
            Content = "Old",
            IsCurrent = false
        });

        (await policyRepository.GetByIdAsync(policy.Id))!.Name.Should().Be("Privacy");
        (await policyRepository.GetByIdAsync(Guid.NewGuid())).Should().BeNull();
        (await policyRepository.GetAllActiveAsync(null)).Should().HaveCount(2);
        (await policyRepository.GetAllActiveAsync(tenantId)).Should().ContainSingle().Which.Id.Should().Be(policy.Id);
        policy.Description = "Updated";
        await policyRepository.UpdateAsync(policy);

        (await versionRepository.GetCurrentVersionAsync(policy.Id))!.Id.Should().Be(version.Id);
        (await versionRepository.GetCurrentVersionAsync(Guid.NewGuid())).Should().BeNull();

        var userId = Guid.NewGuid();
        var consent = await consentRepository.AddAsync(new UserConsent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PolicyVersionId = version.Id,
            IsGranted = true,
            ConsentMethod = "banner"
        });
        (await consentRepository.GetAsync(userId, version.Id))!.ConsentMethod.Should().Be("banner");
        (await consentRepository.GetAsync(Guid.NewGuid(), version.Id)).Should().BeNull();
        (await consentRepository.GetByUserAsync(userId)).Should().ContainSingle();
        consent.ConsentMethod = "settings";
        await consentRepository.UpdateAsync(consent);

        var request = await requestRepository.AddAsync(new DataSubjectRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RequestType = DataSubjectRequestType.Portability,
            Deadline = DateTime.UtcNow.AddDays(30)
        });
        await requestRepository.AddAsync(new DataSubjectRequest
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            RequestType = DataSubjectRequestType.Rectification,
            Status = DataSubjectRequestStatus.InProgress,
            Deadline = DateTime.UtcNow.AddDays(20)
        });
        (await requestRepository.GetByIdAsync(request.Id))!.RequestType.Should().Be(DataSubjectRequestType.Portability);
        (await requestRepository.GetByIdAsync(Guid.NewGuid())).Should().BeNull();
        (await requestRepository.GetByUserAsync(userId)).Should().ContainSingle();
        (await requestRepository.GetPendingAsync()).Should().ContainSingle().Which.Id.Should().Be(request.Id);
        request.Status = DataSubjectRequestStatus.Completed;
        await requestRepository.UpdateAsync(request);
    }

    [Fact]
    public async Task CommandsQueriesAndController_ShouldDelegateThroughSender()
    {
        var service = new Mock<IConsentService>();
        var sender = new Mock<ISender>();
        var policyId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var processedBy = Guid.NewGuid();
        var policy = new ConsentPolicyDto(policyId, "Privacy", PolicyType.PrivacyPolicy, true, true, "1.0");
        var version = new PolicyVersionDto(versionId, policyId, "1.0", ContentType.Markdown, DateTime.UtcNow, true);
        var consent = new UserConsentDto(Guid.NewGuid(), userId, versionId, true, DateTime.UtcNow, null, "banner");
        var dsr = new DataSubjectRequestDto(requestId, userId, DataSubjectRequestType.Access, DataSubjectRequestStatus.Pending, DateTime.UtcNow.AddDays(30), null, null);

        service.Setup(s => s.CreatePolicyAsync("Privacy", PolicyType.PrivacyPolicy, true, "desc", It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        service.Setup(s => s.PublishVersionAsync(policyId, "1.0", "content", ContentType.Markdown, It.IsAny<CancellationToken>())).ReturnsAsync(version);
        service.Setup(s => s.GrantConsentAsync(userId, versionId, "ip", "ua", "banner", It.IsAny<CancellationToken>())).ReturnsAsync(consent);
        service.Setup(s => s.GetActivePoliciesAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync([policy]);
        service.Setup(s => s.GetUserConsentsAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync([consent]);
        service.Setup(s => s.SubmitDataSubjectRequestAsync(userId, DataSubjectRequestType.Access, "desc", It.IsAny<CancellationToken>())).ReturnsAsync(dsr);
        service.Setup(s => s.ProcessDataSubjectRequestAsync(requestId, processedBy, "done", It.IsAny<CancellationToken>())).ReturnsAsync(dsr);
        service.Setup(s => s.GetPendingRequestsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([dsr]);

        (await new CreateConsentPolicyCommandHandler(service.Object).Handle(new CreateConsentPolicyCommand("Privacy", PolicyType.PrivacyPolicy, true, "desc"), CancellationToken.None)).Should().Be(policyId);
        (await new PublishPolicyVersionCommandHandler(service.Object).Handle(new PublishPolicyVersionCommand(policyId, "1.0", "content"), CancellationToken.None)).Should().Be(version);
        (await new GrantConsentCommandHandler(service.Object).Handle(new GrantConsentCommand(userId, versionId, "ip", "ua", "banner"), CancellationToken.None)).Should().Be(consent);
        (await new RevokeConsentCommandHandler(service.Object).Handle(new RevokeConsentCommand(userId, versionId), CancellationToken.None)).Should().Be(Unit.Value);
        (await new SubmitDataSubjectRequestCommandHandler(service.Object).Handle(new SubmitDataSubjectRequestCommand(userId, DataSubjectRequestType.Access, "desc"), CancellationToken.None)).Should().Be(dsr);
        (await new ProcessDataSubjectRequestCommandHandler(service.Object).Handle(new ProcessDataSubjectRequestCommand(requestId, processedBy, "done"), CancellationToken.None)).Should().Be(dsr);
        (await new GetActivePoliciesQueryHandler(service.Object).Handle(new GetActivePoliciesQuery(), CancellationToken.None)).Should().ContainSingle();
        (await new GetUserConsentsQueryHandler(service.Object).Handle(new GetUserConsentsQuery(userId), CancellationToken.None)).Should().ContainSingle();
        (await new GetPendingDataSubjectRequestsQueryHandler(service.Object).Handle(new GetPendingDataSubjectRequestsQuery(), CancellationToken.None)).Should().ContainSingle();

        sender.Setup(s => s.Send(It.IsAny<GetActivePoliciesQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<ConsentPolicyDto> { policy });
        sender.Setup(s => s.Send(It.IsAny<CreateConsentPolicyCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(policyId);
        sender.Setup(s => s.Send(It.IsAny<PublishPolicyVersionCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(version);
        sender.Setup(s => s.Send(It.IsAny<GetUserConsentsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserConsentDto> { consent });
        sender.Setup(s => s.Send(It.IsAny<GrantConsentCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(consent);
        sender.Setup(s => s.Send(It.IsAny<RevokeConsentCommand>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        sender.Setup(s => s.Send(It.IsAny<SubmitDataSubjectRequestCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(dsr);
        sender.Setup(s => s.Send(It.IsAny<ProcessDataSubjectRequestCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(dsr);
        sender.Setup(s => s.Send(It.IsAny<GetPendingDataSubjectRequestsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<DataSubjectRequestDto> { dsr });
        var controller = new ConsentController(sender.Object);

        (await controller.GetActivePolicies(null, CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.CreatePolicy(new CreateConsentPolicyCommand("Privacy", PolicyType.PrivacyPolicy, true), CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.PublishVersion(policyId, new PublishVersionRequest("1.0", "content"), CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.GetUserConsents(userId, CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.GrantConsent(new GrantConsentCommand(userId, versionId), CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.RevokeConsent(new RevokeConsentCommand(userId, versionId), CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.SubmitRequest(new SubmitDataSubjectRequestCommand(userId, DataSubjectRequestType.Access), CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.ProcessRequest(requestId, new ProcessRequestBody(processedBy, "done"), CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.GetPendingRequests(CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void EntitiesConfigurationAndDependencyInjection_ShouldCoverRemainingMembers()
    {
        var consent = new UserConsent { IsGranted = true };
        consent.Revoke();
        consent.IsGranted.Should().BeFalse();
        consent.ConsentRevokedAt.Should().NotBeNull();

        var request = new DataSubjectRequest { Status = DataSubjectRequestStatus.Pending };
        var processedBy = Guid.NewGuid();
        request.Complete(processedBy, "complete");
        request.Status.Should().Be(DataSubjectRequestStatus.Completed);
        request.Reject(processedBy, "reject");
        request.Status.Should().Be(DataSubjectRequestStatus.Rejected);
        request.ProcessingNotes.Should().Be("reject");

        var modelBuilder = new ModelBuilder();
        new ConsentModelConfiguration().Configure(modelBuilder);
        var policyType = modelBuilder.Model.FindEntityType(typeof(ConsentPolicy));
        var versionType = modelBuilder.Model.FindEntityType(typeof(PolicyVersion));
        var consentType = modelBuilder.Model.FindEntityType(typeof(UserConsent));
        var requestType = modelBuilder.Model.FindEntityType(typeof(DataSubjectRequest));

        policyType!.FindProperty(nameof(ConsentPolicy.Name))!.GetMaxLength().Should().Be(200);
        policyType.GetNavigations().Should().Contain(navigation => navigation.Name == nameof(ConsentPolicy.Versions));
        versionType!.FindProperty(nameof(PolicyVersion.ContentType))!.GetMaxLength().Should().Be(50);
        consentType!.GetForeignKeys().Should().Contain(key => key.Properties.Any(property => property.Name == nameof(UserConsent.PolicyVersionId)));
        requestType!.FindProperty(nameof(DataSubjectRequest.Status))!.GetMaxLength().Should().Be(50);

        var services = new ServiceCollection();
        services.AddConsentModule().Should().BeSameAs(services);
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IConsentPolicyRepository) && descriptor.ImplementationType == typeof(ConsentPolicyRepository));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IPolicyVersionRepository) && descriptor.ImplementationType == typeof(PolicyVersionRepository));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IUserConsentRepository) && descriptor.ImplementationType == typeof(UserConsentRepository));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IDataSubjectRequestRepository) && descriptor.ImplementationType == typeof(DataSubjectRequestRepository));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IConsentService) && descriptor.ImplementationType == typeof(ConsentService));

        new ConsentPolicy { Versions = null! }.Should().NotBeNull();
        new PublishVersionRequest("2.0", "url", ContentType.Url).ContentType.Should().Be(ContentType.Url);
        new ProcessRequestBody(processedBy).Notes.Should().BeNull();
    }

    private static ConsentTestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ConsentTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ConsentTestDbContext(options);
    }

    private sealed class ConsentTestDbContext(DbContextOptions<ConsentTestDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new ConsentPolicyConfiguration().Configure(modelBuilder.Entity<ConsentPolicy>());
            new PolicyVersionConfiguration().Configure(modelBuilder.Entity<PolicyVersion>());
            new UserConsentConfiguration().Configure(modelBuilder.Entity<UserConsent>());
            new DataSubjectRequestConfiguration().Configure(modelBuilder.Entity<DataSubjectRequest>());
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class MemoryConsentPolicyRepository : IConsentPolicyRepository
    {
        public List<ConsentPolicy> Items { get; } = [];

        public Task<ConsentPolicy?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(Items.FirstOrDefault(item => item.Id == id && !item.IsDeleted));

        public Task<List<ConsentPolicy>> GetAllActiveAsync(Guid? tenantId, CancellationToken ct = default)
            => Task.FromResult(Items.Where(item => item.IsActive && !item.IsDeleted && (!tenantId.HasValue || item.TenantId == tenantId)).ToList());

        public Task<ConsentPolicy> AddAsync(ConsentPolicy policy, CancellationToken ct = default)
        {
            if (policy.Id == Guid.Empty)
            {
                policy.Id = Guid.NewGuid();
            }

            Items.Add(policy);
            return Task.FromResult(policy);
        }

        public Task UpdateAsync(ConsentPolicy policy, CancellationToken ct = default)
        {
            policy.Touch();
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryPolicyVersionRepository : IPolicyVersionRepository
    {
        public List<PolicyVersion> Items { get; } = [];

        public Task<PolicyVersion?> GetCurrentVersionAsync(Guid policyId, CancellationToken ct = default)
            => Task.FromResult(Items.FirstOrDefault(item => item.ConsentPolicyId == policyId && item.IsCurrent && !item.IsDeleted));

        public Task<PolicyVersion> AddAsync(PolicyVersion version, CancellationToken ct = default)
        {
            if (version.Id == Guid.Empty)
            {
                version.Id = Guid.NewGuid();
            }

            Items.Add(version);
            return Task.FromResult(version);
        }
    }

    private sealed class MemoryUserConsentRepository : IUserConsentRepository
    {
        public List<UserConsent> Items { get; } = [];

        public Task<UserConsent?> GetAsync(Guid userId, Guid policyVersionId, CancellationToken ct = default)
            => Task.FromResult(Items.LastOrDefault(item => item.UserId == userId && item.PolicyVersionId == policyVersionId && !item.IsDeleted));

        public Task<List<UserConsent>> GetByUserAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult(Items.Where(item => item.UserId == userId && !item.IsDeleted).ToList());

        public Task<UserConsent> AddAsync(UserConsent consent, CancellationToken ct = default)
        {
            if (consent.Id == Guid.Empty)
            {
                consent.Id = Guid.NewGuid();
            }

            Items.Add(consent);
            return Task.FromResult(consent);
        }

        public Task UpdateAsync(UserConsent consent, CancellationToken ct = default)
        {
            consent.Touch();
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryDataSubjectRequestRepository : IDataSubjectRequestRepository
    {
        public List<DataSubjectRequest> Items { get; } = [];

        public Task<DataSubjectRequest?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(Items.FirstOrDefault(item => item.Id == id && !item.IsDeleted));

        public Task<List<DataSubjectRequest>> GetByUserAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult(Items.Where(item => item.UserId == userId && !item.IsDeleted).OrderByDescending(item => item.CreatedAt).ToList());

        public Task<List<DataSubjectRequest>> GetPendingAsync(CancellationToken ct = default)
            => Task.FromResult(Items.Where(item => item.Status == DataSubjectRequestStatus.Pending && !item.IsDeleted).OrderBy(item => item.Deadline).ToList());

        public Task<DataSubjectRequest> AddAsync(DataSubjectRequest request, CancellationToken ct = default)
        {
            if (request.Id == Guid.Empty)
            {
                request.Id = Guid.NewGuid();
            }

            Items.Add(request);
            return Task.FromResult(request);
        }

        public Task UpdateAsync(DataSubjectRequest request, CancellationToken ct = default)
        {
            request.Touch();
            return Task.CompletedTask;
        }
    }
}
