using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using GameGuild.Identity.Users;
using Moq;
using Xunit;

namespace GameGuild.Compliance.KYC.Tests;

public sealed class KycCoverageCompletionTests
{
    [Fact]
    public async Task KycRepository_ShouldCover_AllQueriesAndMutations()
    {
        await using var db = CreateDbContext();
        var repository = new KycRepository(db);
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        db.Set<User>().AddRange(
            new User { Id = userId, Email = "kyc-user@example.com", Name = "KYC User" },
            new User { Id = otherUserId, Email = "other-kyc-user@example.com", Name = "Other KYC User" });
        await db.SaveChangesAsync();
        var current = new UserKycVerification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = KycProvider.Onfido,
            Status = KycVerificationStatus.Approved,
            ExternalVerificationId = "external-current",
            SubmittedAt = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc),
            ExpiresAt = DateTime.UtcNow.AddDays(10),
            DocumentCountry = "US"
        };
        var older = new UserKycVerification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = KycProvider.Sumsub,
            Status = KycVerificationStatus.Rejected,
            ExternalVerificationId = "external-older",
            SubmittedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
        var other = new UserKycVerification
        {
            Id = Guid.NewGuid(),
            UserId = otherUserId,
            Provider = KycProvider.Jumio,
            Status = KycVerificationStatus.Pending,
            SubmittedAt = new DateTime(2026, 1, 5, 12, 0, 0, DateTimeKind.Utc)
        };

        await repository.CreateAsync(current);
        await repository.CreateAsync(older);
        await repository.CreateAsync(other);

        (await repository.GetByIdAsync(current.Id)).Should().BeSameAs(current);
        (await repository.GetByUserIdAsync(userId)).Select(v => v.Id).Should().Equal(current.Id, older.Id);
        (await repository.GetLatestVerificationAsync(userId))!.Id.Should().Be(current.Id);
        (await repository.HasApprovedVerificationAsync(userId)).Should().BeTrue();
        (await repository.HasApprovedVerificationAsync(otherUserId)).Should().BeFalse();
        (await repository.GetByStatusAsync(KycVerificationStatus.Pending)).Should().ContainSingle().Which.Id.Should().Be(other.Id);
        (await repository.GetByExternalIdAsync("external-current"))!.Id.Should().Be(current.Id);
        (await repository.GetByDateRangeAsync(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31))).Should().HaveCount(3);

        current.Status = KycVerificationStatus.Suspended;
        await repository.UpdateAsync(current);
        (await repository.GetByIdAsync(current.Id))!.Status.Should().Be(KycVerificationStatus.Suspended);

        await repository.DeleteAsync(older.Id);
        await repository.DeleteAsync(Guid.NewGuid());
        (await repository.GetByIdAsync(older.Id)).Should().BeNull();
    }

    [Fact]
    public void ModelConfiguration_AndContracts_ShouldCoverRemainingMembers()
    {
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<User>();
        new UserKycVerificationConfiguration().Configure(modelBuilder.Entity<UserKycVerification>());
        var entity = modelBuilder.Model.FindEntityType(typeof(UserKycVerification));

        entity.Should().NotBeNull();
        entity!.GetForeignKeys().Should().Contain(key => key.PrincipalEntityType.ClrType == typeof(User));

        var report = new KycComplianceReportDto
        {
            TotalVerifications = 1,
            ApprovedVerifications = 1,
            RejectedVerifications = 0,
            PendingVerifications = 0,
            ExpiredVerifications = 0,
            ApprovalRate = 100,
            VerificationsByProvider = new Dictionary<KycProvider, int> { [KycProvider.Custom] = 1 },
            VerificationsByCountry = new Dictionary<string, int> { ["US"] = 1 }
        };
        report.VerificationsByProvider[KycProvider.Custom].Should().Be(1);
        report.VerificationsByCountry["US"].Should().Be(1);

        var stream = new MemoryStream([1, 2, 3]);
        new SubmitKycVerificationCommand(Guid.NewGuid(), KycProvider.Shufti, "basic", "id", "US").Provider.Should().Be(KycProvider.Shufti);
        new UpdateKycVerificationStatusCommand(Guid.NewGuid(), KycVerificationStatus.Suspended, "note", DateTime.UtcNow).Status.Should().Be(KycVerificationStatus.Suspended);
        new UploadKycDocumentCommand(Guid.NewGuid(), "passport", stream, "passport.png").DocumentStream.Should().BeSameAs(stream);
        new ProcessKycProviderWebhookCommand(KycProvider.Jumio, "external", KycVerificationStatus.InProgress, "{}").ProviderData.Should().Be("{}");
        new DeleteKycVerificationCommand(Guid.NewGuid()).VerificationId.Should().NotBeEmpty();
        new GetKycVerificationByIdQuery(Guid.NewGuid()).VerificationId.Should().NotBeEmpty();
        new GetKycVerificationsByUserIdQuery(Guid.NewGuid()).UserId.Should().NotBeEmpty();
        new GetLatestKycVerificationQuery(Guid.NewGuid()).UserId.Should().NotBeEmpty();
        new IsUserVerifiedQuery(Guid.NewGuid()).UserId.Should().NotBeEmpty();
        new GetKycVerificationsByStatusQuery(KycVerificationStatus.Expired).Status.Should().Be(KycVerificationStatus.Expired);
        new GetKycComplianceReportQuery(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow).EndDate.Should().BeAfter(DateTime.UtcNow.AddDays(-2));
    }

    [Fact]
    public async Task KycService_ShouldCover_RemainingSuccessBranches()
    {
        var repository = new Mock<IKycRepository>();
        var service = CreateService(repository);
        var userId = Guid.NewGuid();
        var verification = new UserKycVerification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = KycVerificationStatus.Pending,
            ExternalVerificationId = "external",
            DocumentTypes = string.Empty
        };
        var completedAt = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);

        repository.Setup(r => r.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>())).ReturnsAsync(verification);
        repository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync([verification]);
        repository.Setup(r => r.GetLatestVerificationAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(verification);
        repository.Setup(r => r.HasApprovedVerificationAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(r => r.GetByStatusAsync(KycVerificationStatus.Pending, It.IsAny<CancellationToken>())).ReturnsAsync([verification]);
        repository.Setup(r => r.GetByExternalIdAsync("external", It.IsAny<CancellationToken>())).ReturnsAsync(verification);

        (await service.GetVerificationByIdAsync(verification.Id)).Value.Should().BeSameAs(verification);
        (await service.GetVerificationsByUserIdAsync(userId)).Value.Should().ContainSingle();
        (await service.GetLatestVerificationAsync(userId)).Value.Should().BeSameAs(verification);
        (await service.IsUserVerifiedAsync(userId)).Value.Should().BeTrue();
        (await service.GetVerificationsByStatusAsync(KycVerificationStatus.Pending)).Value.Should().ContainSingle();

        var upload = await service.UploadDocumentAsync(verification.Id, "passport", Stream.Null, "passport.png");
        upload.Value.Should().EndWith("passport.png");
        verification.DocumentTypes.Should().Be("passport");

        verification.DocumentTypes = "passport";
        (await service.UploadDocumentAsync(verification.Id, "selfie", Stream.Null, "selfie.png")).Value.Should().EndWith("selfie.png");
        verification.DocumentTypes.Should().Be("passport,selfie");

        var webhook = await service.ProcessProviderWebhookAsync(KycProvider.Onfido, "external", KycVerificationStatus.Rejected, "{}", CancellationToken.None);
        webhook.Value.Should().BeTrue();
        verification.CompletedAt.Should().NotBeNull();
        verification.ProviderData.Should().Be("{}");

        var updated = await service.UpdateVerificationStatusAsync(verification.Id, KycVerificationStatus.Approved, "ok", completedAt);
        updated.Value.CompletedAt.Should().Be(completedAt);
        updated.Value.ExpiresAt.Should().NotBeNull();
    }

    [Fact]
    public async Task KycService_ShouldReturnFailures_ForRepositoryExceptions()
    {
        var repository = new Mock<IKycRepository>();
        var service = CreateService(repository);
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var failure = new InvalidOperationException("repository unavailable");

        repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        repository.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        repository.Setup(r => r.GetLatestVerificationAsync(userId, It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        repository.Setup(r => r.HasApprovedVerificationAsync(userId, It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        repository.Setup(r => r.GetByStatusAsync(KycVerificationStatus.Pending, It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        repository.Setup(r => r.GetByExternalIdAsync("external", It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        repository.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ThrowsAsync(failure);

        (await service.UpdateVerificationStatusAsync(id, KycVerificationStatus.Approved, null, null)).Error.Code.Should().Be("KYC.UpdateFailed");
        (await service.GetVerificationByIdAsync(id)).Error.Code.Should().Be("KYC.GetFailed");
        (await service.GetVerificationsByUserIdAsync(userId)).Error.Code.Should().Be("KYC.GetFailed");
        (await service.GetLatestVerificationAsync(userId)).Error.Code.Should().Be("KYC.GetFailed");
        (await service.IsUserVerifiedAsync(userId)).Error.Code.Should().Be("KYC.CheckFailed");
        (await service.GetVerificationsByStatusAsync(KycVerificationStatus.Pending)).Error.Code.Should().Be("KYC.GetFailed");
        (await service.UploadDocumentAsync(id, "passport", Stream.Null, "passport.png")).Error.Code.Should().Be("KYC.UploadFailed");
        (await service.ProcessProviderWebhookAsync(KycProvider.Onfido, "external", KycVerificationStatus.Approved, null)).Error.Code.Should().Be("KYC.WebhookFailed");
        (await service.GetComplianceReportAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow)).Error.Code.Should().Be("KYC.ReportFailed");
    }

    [Fact]
    public async Task KycHandlers_ShouldDelegate_AndReturnFailures_WhenDependenciesThrow()
    {
        var service = new Mock<IKycService>();
        var repository = new Mock<IKycRepository>();
        var verification = new UserKycVerification { Id = Guid.NewGuid(), UserId = Guid.NewGuid() };
        var report = new KycComplianceReportDto { TotalVerifications = 1 };
        var list = new List<UserKycVerification> { verification };
        var stream = new MemoryStream([1]);
        var start = DateTime.UtcNow.AddDays(-1);
        var end = DateTime.UtcNow;

        service.Setup(s => s.SubmitVerificationAsync(verification.UserId, KycProvider.Onfido, "basic", "id", "US", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserKycVerification>.Success(verification));
        service.Setup(s => s.UpdateVerificationStatusAsync(verification.Id, KycVerificationStatus.Approved, "ok", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserKycVerification>.Success(verification));
        service.Setup(s => s.UploadDocumentAsync(verification.Id, "passport", stream, "passport.png", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("url"));
        service.Setup(s => s.ProcessProviderWebhookAsync(KycProvider.Onfido, "external", KycVerificationStatus.Approved, "{}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));
        service.Setup(s => s.GetVerificationByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserKycVerification>.Success(verification));
        service.Setup(s => s.GetVerificationsByUserIdAsync(verification.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<UserKycVerification>>.Success(list));
        service.Setup(s => s.GetLatestVerificationAsync(verification.UserId, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Result<UserKycVerification?>.Success(verification)));
        service.Setup(s => s.IsUserVerifiedAsync(verification.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));
        service.Setup(s => s.GetVerificationsByStatusAsync(KycVerificationStatus.Pending, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<UserKycVerification>>.Success(list));
        service.Setup(s => s.GetComplianceReportAsync(start, end, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<KycComplianceReportDto>.Success(report));

        (await new SubmitKycVerificationHandler(service.Object, NullLogger<SubmitKycVerificationHandler>.Instance)
                .Handle(new SubmitKycVerificationCommand(verification.UserId, KycProvider.Onfido, "basic", "id", "US"), CancellationToken.None)).Value.Should().BeSameAs(verification);
        (await new UpdateKycVerificationStatusHandler(service.Object, NullLogger<UpdateKycVerificationStatusHandler>.Instance)
                .Handle(new UpdateKycVerificationStatusCommand(verification.Id, KycVerificationStatus.Approved, "ok", null), CancellationToken.None)).Value.Should().BeSameAs(verification);
        (await new UploadKycDocumentHandler(service.Object, NullLogger<UploadKycDocumentHandler>.Instance)
                .Handle(new UploadKycDocumentCommand(verification.Id, "passport", stream, "passport.png"), CancellationToken.None)).Value.Should().Be("url");
        (await new ProcessKycProviderWebhookHandler(service.Object, NullLogger<ProcessKycProviderWebhookHandler>.Instance)
                .Handle(new ProcessKycProviderWebhookCommand(KycProvider.Onfido, "external", KycVerificationStatus.Approved, "{}"), CancellationToken.None)).Value.Should().BeTrue();
        (await new DeleteKycVerificationHandler(repository.Object, NullLogger<DeleteKycVerificationHandler>.Instance)
                .Handle(new DeleteKycVerificationCommand(verification.Id), CancellationToken.None)).Value.Should().BeTrue();
        (await new GetKycVerificationByIdHandler(service.Object, NullLogger<GetKycVerificationByIdHandler>.Instance)
                .Handle(new GetKycVerificationByIdQuery(verification.Id), CancellationToken.None)).Value.Should().BeSameAs(verification);
        (await new GetKycVerificationsByUserIdHandler(service.Object, NullLogger<GetKycVerificationsByUserIdHandler>.Instance)
                .Handle(new GetKycVerificationsByUserIdQuery(verification.UserId), CancellationToken.None)).Value.Should().ContainSingle();
        (await new GetLatestKycVerificationHandler(service.Object, NullLogger<GetLatestKycVerificationHandler>.Instance)
                .Handle(new GetLatestKycVerificationQuery(verification.UserId), CancellationToken.None)).Value.Should().BeSameAs(verification);
        (await new IsUserVerifiedHandler(service.Object, NullLogger<IsUserVerifiedHandler>.Instance)
                .Handle(new IsUserVerifiedQuery(verification.UserId), CancellationToken.None)).Value.Should().BeTrue();
        (await new GetKycVerificationsByStatusHandler(service.Object, NullLogger<GetKycVerificationsByStatusHandler>.Instance)
                .Handle(new GetKycVerificationsByStatusQuery(KycVerificationStatus.Pending), CancellationToken.None)).Value.Should().ContainSingle();
        (await new GetKycComplianceReportHandler(service.Object, NullLogger<GetKycComplianceReportHandler>.Instance)
                .Handle(new GetKycComplianceReportQuery(start, end), CancellationToken.None)).Value.Should().BeSameAs(report);

        var throwingService = new Mock<IKycService>();
        var throwingRepository = new Mock<IKycRepository>();
        throwingService.Setup(s => s.SubmitVerificationAsync(It.IsAny<Guid>(), It.IsAny<KycProvider>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        throwingService.Setup(s => s.UpdateVerificationStatusAsync(It.IsAny<Guid>(), It.IsAny<KycVerificationStatus>(), It.IsAny<string?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        throwingService.Setup(s => s.UploadDocumentAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        throwingService.Setup(s => s.ProcessProviderWebhookAsync(It.IsAny<KycProvider>(), It.IsAny<string>(), It.IsAny<KycVerificationStatus>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        throwingService.Setup(s => s.GetVerificationByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        throwingService.Setup(s => s.GetVerificationsByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        throwingService.Setup(s => s.GetLatestVerificationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        throwingService.Setup(s => s.IsUserVerifiedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        throwingService.Setup(s => s.GetVerificationsByStatusAsync(It.IsAny<KycVerificationStatus>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        throwingService.Setup(s => s.GetComplianceReportAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));
        throwingRepository.Setup(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("boom"));

        (await new SubmitKycVerificationHandler(throwingService.Object, NullLogger<SubmitKycVerificationHandler>.Instance)
                .Handle(new SubmitKycVerificationCommand(Guid.NewGuid(), KycProvider.Onfido, "basic", "id", null), CancellationToken.None)).Error.Code.Should().Be("KYC.SubmitFailed");
        (await new UpdateKycVerificationStatusHandler(throwingService.Object, NullLogger<UpdateKycVerificationStatusHandler>.Instance)
                .Handle(new UpdateKycVerificationStatusCommand(Guid.NewGuid(), KycVerificationStatus.Approved, null, null), CancellationToken.None)).Error.Code.Should().Be("KYC.UpdateFailed");
        (await new UploadKycDocumentHandler(throwingService.Object, NullLogger<UploadKycDocumentHandler>.Instance)
                .Handle(new UploadKycDocumentCommand(Guid.NewGuid(), "id", Stream.Null, "id.png"), CancellationToken.None)).Error.Code.Should().Be("KYC.UploadFailed");
        (await new ProcessKycProviderWebhookHandler(throwingService.Object, NullLogger<ProcessKycProviderWebhookHandler>.Instance)
                .Handle(new ProcessKycProviderWebhookCommand(KycProvider.Onfido, "external", KycVerificationStatus.Approved, null), CancellationToken.None)).Error.Code.Should().Be("KYC.WebhookFailed");
        (await new DeleteKycVerificationHandler(throwingRepository.Object, NullLogger<DeleteKycVerificationHandler>.Instance)
                .Handle(new DeleteKycVerificationCommand(Guid.NewGuid()), CancellationToken.None)).Error.Code.Should().Be("KYC.DeleteFailed");
        (await new GetKycVerificationByIdHandler(throwingService.Object, NullLogger<GetKycVerificationByIdHandler>.Instance)
                .Handle(new GetKycVerificationByIdQuery(Guid.NewGuid()), CancellationToken.None)).Error.Code.Should().Be("KYC.GetFailed");
        (await new GetKycVerificationsByUserIdHandler(throwingService.Object, NullLogger<GetKycVerificationsByUserIdHandler>.Instance)
                .Handle(new GetKycVerificationsByUserIdQuery(Guid.NewGuid()), CancellationToken.None)).Error.Code.Should().Be("KYC.GetFailed");
        (await new GetLatestKycVerificationHandler(throwingService.Object, NullLogger<GetLatestKycVerificationHandler>.Instance)
                .Handle(new GetLatestKycVerificationQuery(Guid.NewGuid()), CancellationToken.None)).Error.Code.Should().Be("KYC.GetFailed");
        (await new IsUserVerifiedHandler(throwingService.Object, NullLogger<IsUserVerifiedHandler>.Instance)
                .Handle(new IsUserVerifiedQuery(Guid.NewGuid()), CancellationToken.None)).Error.Code.Should().Be("KYC.CheckFailed");
        (await new GetKycVerificationsByStatusHandler(throwingService.Object, NullLogger<GetKycVerificationsByStatusHandler>.Instance)
                .Handle(new GetKycVerificationsByStatusQuery(KycVerificationStatus.Pending), CancellationToken.None)).Error.Code.Should().Be("KYC.GetFailed");
        (await new GetKycComplianceReportHandler(throwingService.Object, NullLogger<GetKycComplianceReportHandler>.Instance)
                .Handle(new GetKycComplianceReportQuery(start, end), CancellationToken.None)).Error.Code.Should().Be("KYC.ReportFailed");
    }

    private static KycService CreateService(Mock<IKycRepository> repository)
        => new(repository.Object, NullLogger<KycService>.Instance);

    private static KycTestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<KycTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new KycTestDbContext(options);
    }

    private sealed class KycTestDbContext(DbContextOptions<KycTestDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>();
            modelBuilder.Entity<UserKycVerification>();
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
