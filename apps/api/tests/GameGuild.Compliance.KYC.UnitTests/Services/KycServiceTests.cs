using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Compliance.KYC.Tests;

/// <summary>
/// Unit tests for KycService — the core KYC verification workflow.
/// </summary>
public class KycServiceTests
{
    private readonly Mock<IKycRepository> _repositoryMock;
    private readonly ILogger<KycService> _logger;
    private readonly KycService _sut;

    public KycServiceTests()
    {
        _repositoryMock = new Mock<IKycRepository>();
        _logger = NullLogger<KycService>.Instance;
        _sut = new KycService(
            _repositoryMock.Object,
            _logger,
            Options.Create(new KycPolicyOptions { ApprovedEvidenceLifetime = TimeSpan.FromDays(30) }));
    }

    #region SubmitVerificationAsync

    [Fact]
    public async Task SubmitVerificationAsync_ShouldCreatePendingVerification()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<UserKycVerification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.SubmitVerificationAsync(userId, KycProvider.Onfido, "enhanced", "passport", "US");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.Provider.Should().Be(KycProvider.Onfido);
        result.Value.Status.Should().Be(KycVerificationStatus.Pending);
        result.Value.VerificationLevel.Should().Be("enhanced");
        result.Value.DocumentTypes.Should().Be("passport");
        result.Value.DocumentCountry.Should().Be("US");
        result.Value.SubmittedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<UserKycVerification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitVerificationAsync_WhenRepositoryThrows_ShouldReturnFailure()
    {
        // Arrange
        _repositoryMock.Setup(r => r.CreateAsync(It.IsAny<UserKycVerification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB error"));

        // Act
        var result = await _sut.SubmitVerificationAsync(Guid.NewGuid(), KycProvider.Sumsub, "basic", "id_card", null);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("KYC.SubmitFailed");
    }

    #endregion

    #region UpdateVerificationStatusAsync

    [Fact]
    public async Task UpdateVerificationStatusAsync_WhenNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserKycVerification?)null);

        // Act
        var result = await _sut.UpdateVerificationStatusAsync(Guid.NewGuid(), KycVerificationStatus.Approved, null, null);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("KYC.NotFound");
    }

    [Fact]
    public async Task UpdateVerificationStatusAsync_WhenApproved_ShouldSetCompletedAtAndExpiresAt()
    {
        // Arrange
        var verification = new UserKycVerification
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = KycVerificationStatus.Pending
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(verification);

        // Act
        var result = await _sut.UpdateVerificationStatusAsync(verification.Id, KycVerificationStatus.Approved, "Looks good", null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(KycVerificationStatus.Approved);
        result.Value.Notes.Should().Be("Looks good");
        result.Value.CompletedAt.Should().NotBeNull();
        result.Value.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.Value.ExpiresAt.Should().NotBeNull();
        result.Value.ExpiresAt!.Value.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromSeconds(5));

        _repositoryMock.Verify(r => r.UpdateAsync(verification, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateVerificationStatusAsync_WhenRejected_ShouldSetCompletedAtButNotExpiresAt()
    {
        // Arrange
        var verification = new UserKycVerification
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = KycVerificationStatus.Pending
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(verification);

        // Act
        var result = await _sut.UpdateVerificationStatusAsync(verification.Id, KycVerificationStatus.Rejected, "Blurry", null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(KycVerificationStatus.Rejected);
        result.Value.CompletedAt.Should().NotBeNull();
        result.Value.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task UpdateVerificationStatusAsync_WhenInProgress_ShouldNotSetCompletedAt()
    {
        // Arrange
        var verification = new UserKycVerification
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = KycVerificationStatus.Pending
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(verification);

        // Act
        var result = await _sut.UpdateVerificationStatusAsync(verification.Id, KycVerificationStatus.InProgress, null, null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task UpdateVerificationStatusAsync_WhenExplicitCompletedAt_ShouldUseProvidedValue()
    {
        // Arrange
        var verification = new UserKycVerification
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = KycVerificationStatus.Pending
        };
        var explicitDate = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        _repositoryMock.Setup(r => r.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(verification);

        // Act
        var result = await _sut.UpdateVerificationStatusAsync(verification.Id, KycVerificationStatus.Approved, null, explicitDate);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CompletedAt.Should().Be(explicitDate);
    }

    #endregion

    #region ProcessProviderWebhookAsync

    [Fact]
    public async Task ProcessProviderWebhookAsync_WhenNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByExternalIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserKycVerification?)null);

        // Act
        var result = await _sut.ProcessProviderWebhookAsync(KycProvider.Onfido, "ext-123", KycVerificationStatus.Approved, null);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("KYC.NotFound");
    }

    [Fact]
    public async Task ProcessProviderWebhookAsync_WhenApproved_ShouldSetStatusAndExpiry()
    {
        // Arrange
        var verification = new UserKycVerification
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ExternalVerificationId = "ext-456",
            Status = KycVerificationStatus.InProgress
        };
        _repositoryMock.Setup(r => r.GetByExternalIdAsync("ext-456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(verification);

        // Act
        var result = await _sut.ProcessProviderWebhookAsync(KycProvider.Onfido, "ext-456", KycVerificationStatus.Approved, "{\"score\": 95}");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        verification.Status.Should().Be(KycVerificationStatus.Approved);
        verification.ProviderData.Should().Be("{\"score\": 95}");
        verification.CompletedAt.Should().NotBeNull();
        verification.ExpiresAt.Should().NotBeNull();
    }

    #endregion

    #region GetComplianceReportAsync

    [Fact]
    public async Task GetComplianceReportAsync_ShouldCalculateCorrectStatistics()
    {
        // Arrange
        var verifications = new List<UserKycVerification>
        {
            new() { Status = KycVerificationStatus.Approved, Provider = KycProvider.Onfido, DocumentCountry = "US" },
            new() { Status = KycVerificationStatus.Approved, Provider = KycProvider.Onfido, DocumentCountry = "US" },
            new() { Status = KycVerificationStatus.Rejected, Provider = KycProvider.Sumsub, DocumentCountry = "GB" },
            new() { Status = KycVerificationStatus.Pending, Provider = KycProvider.Onfido, DocumentCountry = "US" },
            new() { Status = KycVerificationStatus.Expired, Provider = KycProvider.Jumio, DocumentCountry = null },
        };

        var start = DateTime.UtcNow.AddDays(-30);
        var end = DateTime.UtcNow;
        _repositoryMock.Setup(r => r.GetByDateRangeAsync(start, end, It.IsAny<CancellationToken>()))
            .ReturnsAsync(verifications);

        // Act
        var result = await _sut.GetComplianceReportAsync(start, end);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var report = result.Value;
        report.TotalVerifications.Should().Be(5);
        report.ApprovedVerifications.Should().Be(2);
        report.RejectedVerifications.Should().Be(1);
        report.PendingVerifications.Should().Be(1);
        report.ExpiredVerifications.Should().Be(1);
        report.ApprovalRate.Should().Be(40.0); // 2/5 * 100
        report.VerificationsByProvider.Should().ContainKey(KycProvider.Onfido).WhoseValue.Should().Be(3);
        report.VerificationsByProvider.Should().ContainKey(KycProvider.Sumsub).WhoseValue.Should().Be(1);
        report.VerificationsByCountry.Should().ContainKey("US").WhoseValue.Should().Be(3);
        report.VerificationsByCountry.Should().ContainKey("GB").WhoseValue.Should().Be(1);
        report.VerificationsByCountry.Should().NotContainKey(""); // null countries excluded
    }

    [Fact]
    public async Task GetComplianceReportAsync_WhenEmpty_ShouldReturnZeroApprovalRate()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserKycVerification>());

        // Act
        var result = await _sut.GetComplianceReportAsync(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalVerifications.Should().Be(0);
        result.Value.ApprovalRate.Should().Be(0); // Division-by-zero guard
    }

    #endregion

    #region UploadDocumentAsync

    [Fact]
    public async Task UploadDocumentAsync_WhenNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserKycVerification?)null);

        // Act
        var result = await _sut.UploadDocumentAsync(Guid.NewGuid(), "passport", Stream.Null, "passport.jpg");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("KYC.NotFound");
    }

    [Fact]
    public async Task UploadDocumentAsync_ShouldAppendDocumentType_WhenNotAlreadyPresent()
    {
        // Arrange
        var verification = new UserKycVerification
        {
            Id = Guid.NewGuid(),
            DocumentTypes = "passport"
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(verification);

        // Act
        var result = await _sut.UploadDocumentAsync(verification.Id, "selfie", Stream.Null, "selfie.jpg");

        // Assert
        result.IsSuccess.Should().BeTrue();
        verification.DocumentTypes.Should().Be("passport,selfie");
    }

    [Fact]
    public async Task UploadDocumentAsync_ShouldNotDuplicate_WhenDocumentTypeAlreadyPresent()
    {
        // Arrange
        var verification = new UserKycVerification
        {
            Id = Guid.NewGuid(),
            DocumentTypes = "passport,selfie"
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(verification.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(verification);

        // Act
        var result = await _sut.UploadDocumentAsync(verification.Id, "passport", Stream.Null, "passport2.jpg");

        // Assert
        result.IsSuccess.Should().BeTrue();
        verification.DocumentTypes.Should().Be("passport,selfie"); // Not duplicated
    }

    #endregion

    #region IsUserVerifiedAsync

    [Fact]
    public async Task IsUserVerifiedAsync_ShouldDelegateToRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.HasApprovedVerificationAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.IsUserVerifiedAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    #endregion
}
