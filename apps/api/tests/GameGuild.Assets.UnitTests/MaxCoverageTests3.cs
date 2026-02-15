// Wave 3 coverage tests — targeting 87.6% → 90%+
// Covers: Controller records, Query validators, StorageConfigurationEncryption.Decrypt,
//   Storage-namespace StorageUploadResult/StorageMetadata duplicates

using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.DataProtection;
using Moq;
using GameGuild.Assets;
using GameGuild.Assets.Controllers;
using GameGuild.Assets.Queries;
using GameGuild.Assets.Storage;
using Xunit;

namespace GameGuild.Assets.UnitTests;

// ═══════════════════════════════════════════════════════════════════
//  Controller record types — 18 uncovered lines
// ═══════════════════════════════════════════════════════════════════
public class ControllerRecordsCoverageTests
{
    [Fact]
    public void ContentModerationRequest_Properties()
    {
        var r = new ContentModerationRequest(ModerationStatus.Approved, new[] { "Adult" }, "notes");
        r.Status.Should().Be(ModerationStatus.Approved);
        r.Labels.Should().Contain("Adult");
        r.Notes.Should().Be("notes");
    }

    [Fact]
    public void ContentModerationRequest_OptionalDefaults()
    {
        var r = new ContentModerationRequest(ModerationStatus.Pending);
        r.Labels.Should().BeNull();
        r.Notes.Should().BeNull();
    }

    [Fact]
    public void UpdateVirusScanRequest_Properties()
    {
        var r = new UpdateVirusScanRequest(VirusScanStatus.Infected, "Trojan.Test");
        r.Status.Should().Be(VirusScanStatus.Infected);
        r.ScanResult.Should().Be("Trojan.Test");
    }

    [Fact]
    public void UpdateVirusScanRequest_OptionalDefault()
    {
        var r = new UpdateVirusScanRequest(VirusScanStatus.Clean);
        r.ScanResult.Should().BeNull();
    }

    [Fact]
    public void ReviewReportRequest_Properties()
    {
        var r = new ReviewReportRequest(ReviewDecision.ContentRemoved, "removed");
        r.Decision.Should().Be(ReviewDecision.ContentRemoved);
        r.Notes.Should().Be("removed");
    }

    [Fact]
    public void ReviewReportRequest_OptionalDefault()
    {
        var r = new ReviewReportRequest(ReviewDecision.NoAction);
        r.Notes.Should().BeNull();
    }

    [Fact]
    public void MarkNonDeletableRequest_Properties()
    {
        var r = new MarkNonDeletableRequest("compliance hold");
        r.Reason.Should().Be("compliance hold");
    }

    [Fact]
    public void MarkNonDeletableRequest_OptionalDefault()
    {
        var r = new MarkNonDeletableRequest();
        r.Reason.Should().BeNull();
    }

    [Fact]
    public void UpdateAssetRequest_Properties()
    {
        var r = new UpdateAssetRequest("new name", AssetAccessPolicy.Authenticated);
        r.DisplayName.Should().Be("new name");
        r.AccessPolicy.Should().Be(AssetAccessPolicy.Authenticated);
    }

    [Fact]
    public void UpdateAssetRequest_OptionalDefaults()
    {
        var r = new UpdateAssetRequest();
        r.DisplayName.Should().BeNull();
        r.AccessPolicy.Should().BeNull();
    }

    [Fact]
    public void ReportAssetRequest_Properties()
    {
        var r = new ReportAssetRequest(ReportReason.Copyright, "copied content");
        r.Reason.Should().Be(ReportReason.Copyright);
        r.Description.Should().Be("copied content");
    }

    [Fact]
    public void ReportAssetRequest_OptionalDefault()
    {
        var r = new ReportAssetRequest(ReportReason.Spam);
        r.Description.Should().BeNull();
    }
}

// ═══════════════════════════════════════════════════════════════════
//  Query validators — 8 uncovered lines
// ═══════════════════════════════════════════════════════════════════
public class QueryValidatorsCoverageTests
{
    [Fact]
    public void GetModerationQueueValidator_ValidLimit_Passes()
    {
        var v = new GetModerationQueueValidator();
        var result = v.Validate(new GetModerationQueueQuery(100));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetModerationQueueValidator_ZeroLimit_Fails()
    {
        var v = new GetModerationQueueValidator();
        var result = v.Validate(new GetModerationQueueQuery(0));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetModerationQueueValidator_MaxLimit_Passes()
    {
        var v = new GetModerationQueueValidator();
        var result = v.Validate(new GetModerationQueueQuery(500));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetModerationQueueValidator_OverLimit_Fails()
    {
        var v = new GetModerationQueueValidator();
        var result = v.Validate(new GetModerationQueueQuery(501));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetAssetReportsValidator_ValidId_Passes()
    {
        var v = new GetAssetReportsValidator();
        var result = v.Validate(new GetAssetReportsQuery(Guid.NewGuid()));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetAssetReportsValidator_EmptyId_Fails()
    {
        var v = new GetAssetReportsValidator();
        var result = v.Validate(new GetAssetReportsQuery(Guid.Empty));
        result.IsValid.Should().BeFalse();
    }
}

// ═══════════════════════════════════════════════════════════════════
//  StorageConfigurationEncryption.Decrypt — 19 uncovered lines
// ═══════════════════════════════════════════════════════════════════
public class StorageEncryptionDecryptCoverageTests
{
    private static (StorageConfigurationEncryption svc, Mock<IDataProtector> protector) CreateSvc()
    {
        var mockProtector = new Mock<IDataProtector>();
        // Identity transform: Protect/Unprotect return the input unchanged
        mockProtector.Setup(p => p.Protect(It.IsAny<byte[]>()))
            .Returns<byte[]>(data => data);
        mockProtector.Setup(p => p.Unprotect(It.IsAny<byte[]>()))
            .Returns<byte[]>(data => data);

        var mockProvider = new Mock<IDataProtectionProvider>();
        mockProvider.Setup(p => p.CreateProtector(It.IsAny<string>())).Returns(mockProtector.Object);

        return (new StorageConfigurationEncryption(mockProvider.Object), mockProtector);
    }

    [Fact]
    public void Decrypt_S3Compatible_RoundTrips()
    {
        var (svc, _) = CreateSvc();
        var config = new S3CompatibleConfiguration { AccessKeyId = "ak", SecretAccessKey = "sk", Region = "us-east-1" };
        var encrypted = svc.Encrypt(config);
        var decrypted = svc.Decrypt(encrypted, StorageProviderType.S3Compatible);
        decrypted.Should().BeOfType<S3CompatibleConfiguration>();
        ((S3CompatibleConfiguration)decrypted).AccessKeyId.Should().Be("ak");
    }

    [Fact]
    public void Decrypt_GoogleCloudStorage_RoundTrips()
    {
        var (svc, _) = CreateSvc();
        var config = new GoogleCloudStorageConfiguration { ProjectId = "proj1", CredentialsJson = "{}" };
        var encrypted = svc.Encrypt(config);
        var decrypted = svc.Decrypt(encrypted, StorageProviderType.GoogleCloudStorage);
        decrypted.Should().BeOfType<GoogleCloudStorageConfiguration>();
    }

    [Fact]
    public void Decrypt_AzureBlobStorage_RoundTrips()
    {
        var (svc, _) = CreateSvc();
        var config = new AzureBlobStorageConfiguration { ConnectionString = "DefaultEndpointsProtocol=https" };
        var encrypted = svc.Encrypt(config);
        var decrypted = svc.Decrypt(encrypted, StorageProviderType.AzureBlobStorage);
        decrypted.Should().BeOfType<AzureBlobStorageConfiguration>();
    }

    [Fact]
    public void Decrypt_CloudflareR2_RoundTrips()
    {
        var (svc, _) = CreateSvc();
        var config = new CloudflareR2Configuration { AccountId = "acc", AccessKeyId = "ak", SecretAccessKey = "sk" };
        var encrypted = svc.Encrypt(config);
        var decrypted = svc.Decrypt(encrypted, StorageProviderType.CloudflareR2);
        decrypted.Should().BeOfType<CloudflareR2Configuration>();
    }

    [Fact]
    public void Decrypt_BackblazeB2_RoundTrips()
    {
        var (svc, _) = CreateSvc();
        var config = new BackblazeB2Configuration { ApplicationKeyId = "akid", ApplicationKey = "key", Endpoint = "https://s3.us-west.backblazeb2.com" };
        var encrypted = svc.Encrypt(config);
        var decrypted = svc.Decrypt(encrypted, StorageProviderType.BackblazeB2);
        decrypted.Should().BeOfType<BackblazeB2Configuration>();
    }

    [Fact]
    public void Decrypt_LocalFileSystem_RoundTrips()
    {
        var (svc, _) = CreateSvc();
        var config = new LocalFileSystemConfiguration { BasePath = "/tmp/storage" };
        var encrypted = svc.Encrypt(config);
        var decrypted = svc.Decrypt(encrypted, StorageProviderType.LocalFileSystem);
        decrypted.Should().BeOfType<LocalFileSystemConfiguration>();
    }

    [Fact]
    public void Decrypt_UnsupportedProvider_Throws()
    {
        var (svc, _) = CreateSvc();
        var config = new S3CompatibleConfiguration { AccessKeyId = "ak", SecretAccessKey = "sk" };
        var encrypted = svc.Encrypt(config);
        var act = () => svc.Decrypt(encrypted, (StorageProviderType)999);
        act.Should().Throw<NotSupportedException>();
    }
}

// ═══════════════════════════════════════════════════════════════════
//  Storage-namespace duplicate types — 10 uncovered lines
// ═══════════════════════════════════════════════════════════════════
public class StorageNamespaceTypesCoverageTests
{
    [Fact]
    public void StorageUploadResult_StorageNamespace_Properties()
    {
        var r = new GameGuild.Assets.Storage.StorageUploadResult("bucket", "key/obj.png", "etag123", 1024);
        r.BucketName.Should().Be("bucket");
        r.ObjectKey.Should().Be("key/obj.png");
        r.ETag.Should().Be("etag123");
        r.SizeBytes.Should().Be(1024);
    }

    [Fact]
    public void StorageUploadResult_StorageNamespace_Defaults()
    {
        var r = new GameGuild.Assets.Storage.StorageUploadResult("b", "k");
        r.ETag.Should().BeNull();
        r.SizeBytes.Should().BeNull();
    }

    [Fact]
    public void StorageMetadata_StorageNamespace_Properties()
    {
        var dt = DateTime.UtcNow;
        var m = new GameGuild.Assets.Storage.StorageMetadata(2048, "application/pdf", "etag", dt);
        m.SizeBytes.Should().Be(2048);
        m.MimeType.Should().Be("application/pdf");
        m.ETag.Should().Be("etag");
        m.LastModified.Should().Be(dt);
    }
}
