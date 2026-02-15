using GameGuild.Assets.Storage;
using GameGuild.Assets.Services;
using GameGuild.Assets.Commands;
using FluentValidation.TestHelper;

namespace GameGuild.Assets.UnitTests;

#region Storage Provider Configuration Tests

public class S3CompatibleConfigurationTests
{
    [Fact]
    public void ProviderType_ShouldBeS3Compatible()
    {
        var config = new S3CompatibleConfiguration();
        config.ProviderType.Should().Be(StorageProviderType.S3Compatible);
    }

    [Fact]
    public void Validate_WithValidConfig_ShouldBeValid()
    {
        var config = new S3CompatibleConfiguration
        {
            AccessKeyId = "AKIAIOSFODNN7EXAMPLE",
            SecretAccessKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
            Region = "us-east-1"
        };
        var result = config.Validate();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyAccessKeyId_ShouldFail()
    {
        var config = new S3CompatibleConfiguration
        {
            AccessKeyId = "",
            SecretAccessKey = "secret",
            Region = "us-east-1"
        };
        var result = config.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("AccessKeyId is required");
    }

    [Fact]
    public void Validate_WithEmptySecretAccessKey_ShouldFail()
    {
        var config = new S3CompatibleConfiguration
        {
            AccessKeyId = "key",
            SecretAccessKey = "",
            Region = "us-east-1"
        };
        var result = config.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("SecretAccessKey is required");
    }

    [Fact]
    public void Validate_WithEmptyRegion_ShouldFail()
    {
        var config = new S3CompatibleConfiguration
        {
            AccessKeyId = "key",
            SecretAccessKey = "secret",
            Region = ""
        };
        var result = config.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Region is required");
    }

    [Fact]
    public void Validate_WithAllEmpty_ShouldReturnMultipleErrors()
    {
        var config = new S3CompatibleConfiguration
        {
            AccessKeyId = "",
            SecretAccessKey = "",
            Region = ""
        };
        var result = config.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
    }

    [Fact]
    public void Defaults_ShouldBeReasonable()
    {
        var config = new S3CompatibleConfiguration();
        config.Region.Should().Be("us-east-1");
        config.ForcePathStyle.Should().BeFalse();
        config.UseHttp.Should().BeFalse();
        config.SessionToken.Should().BeNull();
        config.ServiceUrl.Should().BeNull();
    }
}

public class GoogleCloudStorageConfigurationTests
{
    [Fact]
    public void ProviderType_ShouldBeGoogleCloudStorage()
    {
        var config = new GoogleCloudStorageConfiguration();
        config.ProviderType.Should().Be(StorageProviderType.GoogleCloudStorage);
    }

    [Fact]
    public void Validate_WithValidConfig_ShouldBeValid()
    {
        var config = new GoogleCloudStorageConfiguration
        {
            ProjectId = "my-project",
            CredentialsJson = "{\"type\":\"service_account\"}"
        };
        var result = config.Validate();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyProjectId_ShouldFail()
    {
        var config = new GoogleCloudStorageConfiguration
        {
            ProjectId = "",
            CredentialsJson = "{}"
        };
        var result = config.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("ProjectId is required");
    }

    [Fact]
    public void Validate_WithoutCredentials_AndNotUsingADC_ShouldFail()
    {
        var config = new GoogleCloudStorageConfiguration
        {
            ProjectId = "my-project",
            CredentialsJson = "",
            UseApplicationDefaultCredentials = false
        };
        var result = config.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("CredentialsJson is required when not using Application Default Credentials");
    }

    [Fact]
    public void Validate_WithADC_NoCredentialsNeeded()
    {
        var config = new GoogleCloudStorageConfiguration
        {
            ProjectId = "my-project",
            UseApplicationDefaultCredentials = true,
            CredentialsJson = ""
        };
        var result = config.Validate();
        result.IsValid.Should().BeTrue();
    }
}

public class AzureBlobStorageConfigurationTests
{
    [Fact]
    public void ProviderType_ShouldBeAzureBlobStorage()
    {
        var config = new AzureBlobStorageConfiguration();
        config.ProviderType.Should().Be(StorageProviderType.AzureBlobStorage);
    }

    [Fact]
    public void Validate_WithConnectionString_ShouldBeValid()
    {
        var config = new AzureBlobStorageConfiguration
        {
            ConnectionString = "DefaultEndpointsProtocol=https;AccountName=test"
        };
        var result = config.Validate();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithAccountNameAndKey_ShouldBeValid()
    {
        var config = new AzureBlobStorageConfiguration
        {
            AccountName = "myaccount",
            AccountKey = "mykey"
        };
        var result = config.Validate();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithManagedIdentity_AndAccountName_ShouldBeValid()
    {
        var config = new AzureBlobStorageConfiguration
        {
            UseManagedIdentity = true,
            AccountName = "myaccount"
        };
        var result = config.Validate();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithManagedIdentity_AndBlobServiceUri_ShouldBeValid()
    {
        var config = new AzureBlobStorageConfiguration
        {
            UseManagedIdentity = true,
            BlobServiceUri = "https://myaccount.blob.core.windows.net"
        };
        var result = config.Validate();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithManagedIdentity_NoAccountOrUri_ShouldFail()
    {
        var config = new AzureBlobStorageConfiguration
        {
            UseManagedIdentity = true
        };
        var result = config.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("AccountName or BlobServiceUri is required when using Managed Identity");
    }

    [Fact]
    public void Validate_WithoutConnectionString_MissingAccountName_ShouldFail()
    {
        var config = new AzureBlobStorageConfiguration
        {
            AccountKey = "key"
        };
        var result = config.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("AccountName is required");
    }

    [Fact]
    public void Validate_WithoutConnectionString_MissingAccountKey_ShouldFail()
    {
        var config = new AzureBlobStorageConfiguration
        {
            AccountName = "acct"
        };
        var result = config.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("AccountKey is required");
    }
}

public class CloudflareR2ConfigurationTests
{
    [Fact]
    public void ProviderType_ShouldBeCloudflareR2()
    {
        var config = new CloudflareR2Configuration();
        config.ProviderType.Should().Be(StorageProviderType.CloudflareR2);
    }

    [Fact]
    public void Validate_WithValidConfig_ShouldBeValid()
    {
        var config = new CloudflareR2Configuration
        {
            AccountId = "abc123",
            AccessKeyId = "key",
            SecretAccessKey = "secret"
        };
        var result = config.Validate();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyAccountId_ShouldFail()
    {
        var config = new CloudflareR2Configuration
        {
            AccountId = "",
            AccessKeyId = "key",
            SecretAccessKey = "secret"
        };
        var result = config.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("AccountId is required");
    }

    [Fact]
    public void Validate_WithEmptyAccessKeyId_ShouldFail()
    {
        var config = new CloudflareR2Configuration
        {
            AccountId = "abc",
            AccessKeyId = "",
            SecretAccessKey = "secret"
        };
        var result = config.Validate();
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithEmptySecretAccessKey_ShouldFail()
    {
        var config = new CloudflareR2Configuration
        {
            AccountId = "abc",
            AccessKeyId = "key",
            SecretAccessKey = ""
        };
        var result = config.Validate();
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetEndpointUrl_WithoutJurisdiction_ShouldReturnStandardUrl()
    {
        var config = new CloudflareR2Configuration { AccountId = "abc123" };
        config.GetEndpointUrl().Should().Be("https://abc123.r2.cloudflarestorage.com");
    }

    [Fact]
    public void GetEndpointUrl_WithJurisdiction_ShouldIncludeJurisdiction()
    {
        var config = new CloudflareR2Configuration
        {
            AccountId = "abc123",
            Jurisdiction = "eu"
        };
        config.GetEndpointUrl().Should().Be("https://abc123.eu.r2.cloudflarestorage.com");
    }
}

public class BackblazeB2ConfigurationTests
{
    [Fact]
    public void ProviderType_ShouldBeBackblazeB2()
    {
        var config = new BackblazeB2Configuration();
        config.ProviderType.Should().Be(StorageProviderType.BackblazeB2);
    }

    [Fact]
    public void Validate_WithValidConfig_ShouldBeValid()
    {
        var config = new BackblazeB2Configuration
        {
            ApplicationKeyId = "keyid",
            ApplicationKey = "appkey",
            Endpoint = "s3.us-west-004.backblazeb2.com"
        };
        var result = config.Validate();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyApplicationKeyId_ShouldFail()
    {
        var config = new BackblazeB2Configuration
        {
            ApplicationKeyId = "",
            ApplicationKey = "key",
            Endpoint = "endpoint"
        };
        var result = config.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("ApplicationKeyId is required");
    }

    [Fact]
    public void Validate_WithEmptyApplicationKey_ShouldFail()
    {
        var config = new BackblazeB2Configuration
        {
            ApplicationKeyId = "id",
            ApplicationKey = "",
            Endpoint = "endpoint"
        };
        var result = config.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("ApplicationKey is required");
    }

    [Fact]
    public void Validate_WithEmptyEndpoint_ShouldFail()
    {
        var config = new BackblazeB2Configuration
        {
            ApplicationKeyId = "id",
            ApplicationKey = "key",
            Endpoint = ""
        };
        var result = config.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Endpoint is required");
    }
}

public class LocalFileSystemConfigurationTests
{
    [Fact]
    public void ProviderType_ShouldBeLocalFileSystem()
    {
        var config = new LocalFileSystemConfiguration();
        config.ProviderType.Should().Be(StorageProviderType.LocalFileSystem);
    }

    [Fact]
    public void Validate_WithBasePath_ShouldBeValid()
    {
        var config = new LocalFileSystemConfiguration { BasePath = "/tmp/storage" };
        var result = config.Validate();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyBasePath_ShouldFail()
    {
        var config = new LocalFileSystemConfiguration { BasePath = "" };
        var result = config.Validate();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("BasePath is required");
    }

    [Fact]
    public void Defaults_ShouldHaveDefaultBasePath()
    {
        var config = new LocalFileSystemConfiguration();
        config.BasePath.Should().Be("./storage");
        config.ServeUrlPrefix.Should().BeNull();
    }
}

public class ValidationResultTests
{
    [Fact]
    public void Success_ShouldReturnValid()
    {
        var result = Storage.ValidationResult.Success();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Failure_ShouldReturnInvalidWithErrors()
    {
        var result = Storage.ValidationResult.Failure("error1", "error2");
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain("error1");
        result.Errors.Should().Contain("error2");
    }
}

#endregion

#region TenantStorageConfiguration Tests

public class TenantStorageConfigurationTests
{
    [Fact]
    public void Create_WithValidArgs_ShouldCreateConfiguration()
    {
        var tenantId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var config = TenantStorageConfiguration.Create(
            tenantId,
            StorageProviderType.S3Compatible,
            "My Storage",
            "encrypted-config",
            "my-bucket",
            "my-transformed-bucket",
            "us-east-1",
            "https://cdn.example.com",
            createdBy);

        config.Should().NotBeNull();
        config.Id.Should().NotBe(Guid.Empty);
        config.TenantId.Should().Be(tenantId);
        config.ProviderType.Should().Be(StorageProviderType.S3Compatible);
        config.Name.Should().Be("My Storage");
        config.EncryptedConfiguration.Should().Be("encrypted-config");
        config.BucketName.Should().Be("my-bucket");
        config.TransformedBucketName.Should().Be("my-transformed-bucket");
        config.Region.Should().Be("us-east-1");
        config.CdnUrlPrefix.Should().Be("https://cdn.example.com");
        config.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Create_WithEmptyTenantId_ShouldThrow()
    {
        var act = () => TenantStorageConfiguration.Create(
            Guid.Empty, StorageProviderType.S3Compatible, "name",
            "enc", "bucket", "transformed", null, null, Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithMessage("*TenantId*");
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => TenantStorageConfiguration.Create(
            Guid.NewGuid(), StorageProviderType.S3Compatible, "",
            "enc", "bucket", "transformed", null, null, Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithMessage("*Name*");
    }

    [Fact]
    public void Create_WithEmptyBucketName_ShouldThrow()
    {
        var act = () => TenantStorageConfiguration.Create(
            Guid.NewGuid(), StorageProviderType.S3Compatible, "name",
            "enc", "", "transformed", null, null, Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithMessage("*BucketName*");
    }

    [Fact]
    public void Enable_WhenNotValidated_ShouldThrow()
    {
        var config = TenantStorageConfiguration.Create(
            Guid.NewGuid(), StorageProviderType.S3Compatible, "test",
            "enc", "bucket", "transformed", null, null, Guid.NewGuid());

        var act = () => config.Enable();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Enable_WhenValidatedSuccessfully_ShouldEnable()
    {
        var config = TenantStorageConfiguration.Create(
            Guid.NewGuid(), StorageProviderType.S3Compatible, "test",
            "enc", "bucket", "transformed", null, null, Guid.NewGuid());
        config.RecordValidation(true);
        config.Enable();
        config.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Enable_WhenValidationFailed_ShouldThrow()
    {
        var config = TenantStorageConfiguration.Create(
            Guid.NewGuid(), StorageProviderType.S3Compatible, "test",
            "enc", "bucket", "transformed", null, null, Guid.NewGuid());
        config.RecordValidation(false, "Connection failed");

        var act = () => config.Enable();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Disable_ShouldSetIsEnabledFalse()
    {
        var config = TenantStorageConfiguration.Create(
            Guid.NewGuid(), StorageProviderType.S3Compatible, "test",
            "enc", "bucket", "transformed", null, null, Guid.NewGuid());
        config.RecordValidation(true);
        config.Enable();
        config.IsEnabled.Should().BeTrue();
        config.Disable();
        config.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void UpdateConfiguration_ShouldUpdateAndRequireRevalidation()
    {
        var config = TenantStorageConfiguration.Create(
            Guid.NewGuid(), StorageProviderType.S3Compatible, "test",
            "enc", "bucket", "transformed", "us-east-1", null, Guid.NewGuid());
        config.RecordValidation(true);
        config.Enable();
        config.IsEnabled.Should().BeTrue();

        var updatedBy = Guid.NewGuid();
        config.UpdateConfiguration("new-enc", "new-bucket", "new-transformed", "eu-west-1", "https://cdn.new.com", updatedBy);

        config.EncryptedConfiguration.Should().Be("new-enc");
        config.BucketName.Should().Be("new-bucket");
        config.TransformedBucketName.Should().Be("new-transformed");
        config.Region.Should().Be("eu-west-1");
        config.CdnUrlPrefix.Should().Be("https://cdn.new.com");
        config.IsEnabled.Should().BeFalse(); // re-validation required
        config.LastValidated.Should().BeNull();
        config.LastValidationSuccess.Should().BeNull();
        config.LastValidationError.Should().BeNull();
    }

    [Fact]
    public void RecordValidation_Success_ShouldSetFields()
    {
        var config = TenantStorageConfiguration.Create(
            Guid.NewGuid(), StorageProviderType.S3Compatible, "test",
            "enc", "bucket", "transformed", null, null, Guid.NewGuid());
        config.RecordValidation(true);
        config.LastValidationSuccess.Should().BeTrue();
        config.LastValidated.Should().NotBeNull();
        config.LastValidationError.Should().BeNull();
    }

    [Fact]
    public void RecordValidation_Failure_ShouldSetErrorMessage()
    {
        var config = TenantStorageConfiguration.Create(
            Guid.NewGuid(), StorageProviderType.S3Compatible, "test",
            "enc", "bucket", "transformed", null, null, Guid.NewGuid());
        config.RecordValidation(false, "Bucket not found");
        config.LastValidationSuccess.Should().BeFalse();
        config.LastValidated.Should().NotBeNull();
        config.LastValidationError.Should().Be("Bucket not found");
    }
}

#endregion

#region Command Validator Tests

public class UploadAssetValidatorTests
{
    private readonly UploadAssetValidator _validator = new();

    [Fact]
    public void Valid_Command_ShouldPassValidation()
    {
        var command = new UploadAssetCommand(
            Stream.Null, "test.png", "image/png",
            Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void NullContent_ShouldFail()
    {
        var command = new UploadAssetCommand(
            null!, "", "image/png", Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Content);
    }

    [Fact]
    public void EmptyFileName_ShouldFail()
    {
        var command = new UploadAssetCommand(
            Stream.Null, "", "image/png", Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Fact]
    public void FileNameTooLong_ShouldFail()
    {
        var command = new UploadAssetCommand(
            Stream.Null, new string('a', 256), "image/png",
            Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }

    [Fact]
    public void EmptyMimeType_ShouldFail()
    {
        var command = new UploadAssetCommand(
            Stream.Null, "test.png", "", Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.MimeType);
    }

    [Fact]
    public void MimeTypeTooLong_ShouldFail()
    {
        var command = new UploadAssetCommand(
            Stream.Null, "test.png", new string('x', 101),
            Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.MimeType);
    }

    [Fact]
    public void EmptyUserId_ShouldFail()
    {
        var command = new UploadAssetCommand(
            Stream.Null, "test.png", "image/png", Guid.Empty, Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void NullTenantId_ShouldFail()
    {
        var command = new UploadAssetCommand(
            Stream.Null, "test.png", "image/png", Guid.NewGuid(), null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }

    [Fact]
    public void DisplayNameTooLong_ShouldFail()
    {
        var command = new UploadAssetCommand(
            Stream.Null, "test.png", "image/png",
            Guid.NewGuid(), Guid.NewGuid(),
            DisplayName: new string('a', 256));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.DisplayName);
    }

    [Fact]
    public void ParentResourceTypeTooLong_ShouldFail()
    {
        var command = new UploadAssetCommand(
            Stream.Null, "test.png", "image/png",
            Guid.NewGuid(), Guid.NewGuid(),
            ParentResourceType: new string('x', 101));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ParentResourceType);
    }
}

public class UpdateAssetValidatorTests
{
    private readonly UpdateAssetValidator _validator = new();

    [Fact]
    public void Valid_ShouldPass()
    {
        var command = new UpdateAssetCommand(Guid.NewGuid(), Guid.NewGuid(), "name");
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyAssetReferenceId_ShouldFail()
    {
        var command = new UpdateAssetCommand(Guid.Empty, Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AssetReferenceId);
    }

    [Fact]
    public void EmptyUserId_ShouldFail()
    {
        var command = new UpdateAssetCommand(Guid.NewGuid(), Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void DisplayNameTooLong_ShouldFail()
    {
        var command = new UpdateAssetCommand(Guid.NewGuid(), Guid.NewGuid(), new string('x', 256));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.DisplayName);
    }
}

public class ReportAssetValidatorTests
{
    private readonly ReportAssetValidator _validator = new();

    [Fact]
    public void Valid_ShouldPass()
    {
        var command = new ReportAssetCommand(Guid.NewGuid(), Guid.NewGuid(), ReportReason.Inappropriate);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyAssetReferenceId_ShouldFail()
    {
        var command = new ReportAssetCommand(Guid.Empty, Guid.NewGuid(), ReportReason.Inappropriate);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AssetReferenceId);
    }

    [Fact]
    public void EmptyReportedByUserId_ShouldFail()
    {
        var command = new ReportAssetCommand(Guid.NewGuid(), Guid.Empty, ReportReason.Inappropriate);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ReportedByUserId);
    }

    [Fact]
    public void InvalidReason_ShouldFail()
    {
        var command = new ReportAssetCommand(Guid.NewGuid(), Guid.NewGuid(), (ReportReason)999);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void DescriptionTooLong_ShouldFail()
    {
        var command = new ReportAssetCommand(Guid.NewGuid(), Guid.NewGuid(), ReportReason.Inappropriate, new string('x', 2001));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }
}

public class GenerateAccessUrlValidatorTests
{
    private readonly GenerateAccessUrlValidator _validator = new();

    [Fact]
    public void Valid_ShouldPass()
    {
        var command = new GenerateAccessUrlCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyAssetReferenceId_ShouldFail()
    {
        var command = new GenerateAccessUrlCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AssetReferenceId);
    }

    [Fact]
    public void NullTenantId_ShouldFail()
    {
        var command = new GenerateAccessUrlCommand(Guid.NewGuid(), Guid.NewGuid(), null);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TenantId);
    }
}

public class DeleteAssetValidatorTests
{
    private readonly DeleteAssetValidator _validator = new();

    [Fact]
    public void Valid_ShouldPass()
    {
        var command = new DeleteAssetCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyAssetReferenceId_ShouldFail()
    {
        var command = new DeleteAssetCommand(Guid.Empty, Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AssetReferenceId);
    }

    [Fact]
    public void EmptyUserId_ShouldFail()
    {
        var command = new DeleteAssetCommand(Guid.NewGuid(), Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}

public class ReviewReportValidatorTests
{
    private readonly ReviewReportValidator _validator = new();

    [Fact]
    public void Valid_ShouldPass()
    {
        var command = new ReviewReportCommand(Guid.NewGuid(), Guid.NewGuid(), ReviewDecision.NoAction);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EmptyReportId_ShouldFail()
    {
        var command = new ReviewReportCommand(Guid.Empty, Guid.NewGuid(), ReviewDecision.NoAction);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ReportId);
    }

    [Fact]
    public void EmptyReviewerId_ShouldFail()
    {
        var command = new ReviewReportCommand(Guid.NewGuid(), Guid.Empty, ReviewDecision.NoAction);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ReviewerId);
    }

    [Fact]
    public void InvalidDecision_ShouldFail()
    {
        var command = new ReviewReportCommand(Guid.NewGuid(), Guid.NewGuid(), (ReviewDecision)999);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Decision);
    }

    [Fact]
    public void NotesTooLong_ShouldFail()
    {
        var command = new ReviewReportCommand(Guid.NewGuid(), Guid.NewGuid(), ReviewDecision.NoAction, new string('x', 2001));
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }
}

#endregion

#region AssetLocalizationService Tests

public class AssetLocalizationServiceTests
{
    private readonly AssetLocalizationService _service = new();

    // GetModerationRejectionReason
    [Fact]
    public void GetModerationRejectionReason_English_ShouldFormatLabels()
    {
        var result = _service.GetModerationRejectionReason(new[] { "explicit", "violence" }, "en");
        result.Should().Contain("explicit content");
        result.Should().Contain("violent content");
        result.Should().Contain("policy violation");
    }

    [Fact]
    public void GetModerationRejectionReason_Spanish_ShouldReturnSpanish()
    {
        var result = _service.GetModerationRejectionReason(new[] { "spam" }, "es");
        result.Should().Contain("spam");
        result.Should().Contain("violación de políticas");
    }

    [Fact]
    public void GetModerationRejectionReason_Portuguese_ShouldReturnPortuguese()
    {
        var result = _service.GetModerationRejectionReason(new[] { "hate" }, "pt-BR");
        result.Should().Contain("conteúdo de ódio");
        result.Should().Contain("violação de política");
    }

    [Fact]
    public void GetModerationRejectionReason_UnknownLabel_ShouldFallbackToRawLabel()
    {
        var result = _service.GetModerationRejectionReason(new[] { "custom_label" }, "en");
        result.Should().Contain("custom_label");
    }

    // GetAccessDeniedMessage
    [Fact]
    public void GetAccessDeniedMessage_Private_English()
    {
        var result = _service.GetAccessDeniedMessage(AssetAccessPolicy.Private, "en");
        result.Should().Contain("private");
    }

    [Fact]
    public void GetAccessDeniedMessage_TenantPublic_English()
    {
        var result = _service.GetAccessDeniedMessage(AssetAccessPolicy.TenantPublic, "en");
        result.Should().Contain("different tenant");
    }

    [Fact]
    public void GetAccessDeniedMessage_Authenticated_English()
    {
        var result = _service.GetAccessDeniedMessage(AssetAccessPolicy.Authenticated, "en");
        result.Should().Contain("authenticated");
    }

    [Fact]
    public void GetAccessDeniedMessage_OwnerOnly_English()
    {
        var result = _service.GetAccessDeniedMessage(AssetAccessPolicy.OwnerOnly, "en");
        result.Should().Contain("owner");
    }

    [Fact]
    public void GetAccessDeniedMessage_DefaultCase_ShouldFallbackToPrivate()
    {
        // Use a policy that's not explicitly handled (e.g. Public or PaidContent)
        var result = _service.GetAccessDeniedMessage(AssetAccessPolicy.Public, "en");
        result.Should().Contain("private");
    }

    [Fact]
    public void GetAccessDeniedMessage_Spanish()
    {
        var result = _service.GetAccessDeniedMessage(AssetAccessPolicy.Private, "es");
        result.Should().Contain("privado");
    }

    // GetQuotaExceededMessage
    [Fact]
    public void GetQuotaExceededMessage_Assets_English()
    {
        var result = _service.GetQuotaExceededMessage("assets", 100, 200, "en");
        result.Should().Contain("100");
        result.Should().Contain("200");
        result.Should().Contain("asset limit");
    }

    [Fact]
    public void GetQuotaExceededMessage_Storage_English()
    {
        var result = _service.GetQuotaExceededMessage("storage", 500, 1000, "en");
        result.Should().Contain("storage quota");
    }

    [Fact]
    public void GetQuotaExceededMessage_AssetStorage_English()
    {
        var result = _service.GetQuotaExceededMessage("assetstorage", 500, 1000, "en");
        result.Should().Contain("storage quota");
    }

    [Fact]
    public void GetQuotaExceededMessage_UnknownType_FallsBackToAssets()
    {
        var result = _service.GetQuotaExceededMessage("unknown", 50, 100, "en");
        result.Should().Contain("asset limit");
    }

    // GetVirusDetectedMessage
    [Fact]
    public void GetVirusDetectedMessage_English()
    {
        var result = _service.GetVirusDetectedMessage("malware.exe", "en");
        result.Should().Contain("malware.exe");
        result.Should().Contain("malicious");
    }

    [Fact]
    public void GetVirusDetectedMessage_Portuguese()
    {
        var result = _service.GetVirusDetectedMessage("virus.exe", "pt-BR");
        result.Should().Contain("virus.exe");
        result.Should().Contain("malicioso");
    }

    // GetUploadFailedMessage
    [Fact]
    public void GetUploadFailedMessage_Size_English()
    {
        var result = _service.GetUploadFailedMessage("size", "en");
        result.Should().Contain("maximum allowed size");
    }

    [Fact]
    public void GetUploadFailedMessage_FileTooBig_English()
    {
        var result = _service.GetUploadFailedMessage("filetoobig", "en");
        result.Should().Contain("maximum allowed size");
    }

    [Fact]
    public void GetUploadFailedMessage_Type_English()
    {
        var result = _service.GetUploadFailedMessage("type", "en");
        result.Should().Contain("file type is not allowed");
    }

    [Fact]
    public void GetUploadFailedMessage_MimeType_English()
    {
        var result = _service.GetUploadFailedMessage("mimetype", "en");
        result.Should().Contain("file type is not allowed");
    }

    [Fact]
    public void GetUploadFailedMessage_Generic_English()
    {
        var result = _service.GetUploadFailedMessage("unknown", "en");
        result.Should().Contain("try again");
    }

    // Language normalization
    [Fact]
    public void NormalizesLanguage_pt_ToPtBR()
    {
        var result = _service.GetVirusDetectedMessage("test.exe", "pt");
        result.Should().Contain("malicioso"); // pt-BR fallback
    }

    [Fact]
    public void NormalizesLanguage_esMx_ToEs()
    {
        var result = _service.GetAccessDeniedMessage(AssetAccessPolicy.Private, "es-mx");
        result.Should().Contain("privado");
    }

    [Fact]
    public void NormalizesLanguage_esAr_ToEs()
    {
        var result = _service.GetAccessDeniedMessage(AssetAccessPolicy.Private, "es-ar");
        result.Should().Contain("privado");
    }

    [Fact]
    public void NormalizesLanguage_esCo_ToEs()
    {
        var result = _service.GetAccessDeniedMessage(AssetAccessPolicy.Private, "es-co");
        result.Should().Contain("privado");
    }

    [Fact]
    public void UnknownLanguage_FallsBackToEnglish()
    {
        var result = _service.GetAccessDeniedMessage(AssetAccessPolicy.Private, "fr");
        result.Should().Contain("private");
    }

    [Fact]
    public void EmptyLanguageCode_FallsBackToEnglish()
    {
        var result = _service.GetAccessDeniedMessage(AssetAccessPolicy.Private, "");
        result.Should().Contain("private");
    }
}

#endregion

#region StorageProviderType Enum Tests

public class StorageProviderTypeTests
{
    [Theory]
    [InlineData(StorageProviderType.S3Compatible, 0)]
    [InlineData(StorageProviderType.GoogleCloudStorage, 1)]
    [InlineData(StorageProviderType.AzureBlobStorage, 2)]
    [InlineData(StorageProviderType.CloudflareR2, 3)]
    [InlineData(StorageProviderType.BackblazeB2, 4)]
    [InlineData(StorageProviderType.LocalFileSystem, 99)]
    public void EnumValues_ShouldMatchExpected(StorageProviderType type, int expected)
    {
        ((int)type).Should().Be(expected);
    }
}

#endregion

#region GlobalStorageOptions Tests

public class GlobalStorageOptionsTests
{
    [Fact]
    public void Defaults_ShouldBeReasonable()
    {
        var opts = new GlobalStorageOptions();
        opts.DefaultProviderType.Should().Be(StorageProviderType.S3Compatible);
        opts.AllowTenantStorage.Should().BeTrue();
        opts.BucketName.Should().Be("assets");
        opts.TransformedBucketName.Should().Be("assets-transformed");
        opts.QuarantineBucketName.Should().Be("assets-quarantine");
        opts.CdnUrlPrefix.Should().BeNull();
        opts.PresignedUrlExpiryMinutes.Should().Be(60);
    }

    [Fact]
    public void SectionName_ShouldBeCorrect()
    {
        GlobalStorageOptions.SectionName.Should().Be("Assets:Storage");
    }

    [Fact]
    public void GetActiveConfiguration_S3Compatible()
    {
        var config = new S3CompatibleConfiguration { AccessKeyId = "key" };
        var opts = new GlobalStorageOptions
        {
            DefaultProviderType = StorageProviderType.S3Compatible,
            S3Compatible = config
        };
        opts.GetActiveConfiguration().Should().BeSameAs(config);
    }

    [Fact]
    public void GetActiveConfiguration_GoogleCloudStorage()
    {
        var config = new GoogleCloudStorageConfiguration { ProjectId = "proj" };
        var opts = new GlobalStorageOptions
        {
            DefaultProviderType = StorageProviderType.GoogleCloudStorage,
            GoogleCloudStorage = config
        };
        opts.GetActiveConfiguration().Should().BeSameAs(config);
    }

    [Fact]
    public void GetActiveConfiguration_AzureBlobStorage()
    {
        var config = new AzureBlobStorageConfiguration { AccountName = "acct" };
        var opts = new GlobalStorageOptions
        {
            DefaultProviderType = StorageProviderType.AzureBlobStorage,
            AzureBlobStorage = config
        };
        opts.GetActiveConfiguration().Should().BeSameAs(config);
    }

    [Fact]
    public void GetActiveConfiguration_CloudflareR2()
    {
        var config = new CloudflareR2Configuration { AccountId = "abc" };
        var opts = new GlobalStorageOptions
        {
            DefaultProviderType = StorageProviderType.CloudflareR2,
            CloudflareR2 = config
        };
        opts.GetActiveConfiguration().Should().BeSameAs(config);
    }

    [Fact]
    public void GetActiveConfiguration_BackblazeB2()
    {
        var config = new BackblazeB2Configuration { ApplicationKeyId = "id" };
        var opts = new GlobalStorageOptions
        {
            DefaultProviderType = StorageProviderType.BackblazeB2,
            BackblazeB2 = config
        };
        opts.GetActiveConfiguration().Should().BeSameAs(config);
    }

    [Fact]
    public void GetActiveConfiguration_LocalFileSystem()
    {
        var config = new LocalFileSystemConfiguration { BasePath = "/tmp" };
        var opts = new GlobalStorageOptions
        {
            DefaultProviderType = StorageProviderType.LocalFileSystem,
            LocalFileSystem = config
        };
        opts.GetActiveConfiguration().Should().BeSameAs(config);
    }

    [Fact]
    public void GetActiveConfiguration_UnknownType_FallsBackToS3()
    {
        var s3 = new S3CompatibleConfiguration { AccessKeyId = "fallback" };
        var opts = new GlobalStorageOptions
        {
            DefaultProviderType = (StorageProviderType)999,
            S3Compatible = s3
        };
        opts.GetActiveConfiguration().Should().BeSameAs(s3);
    }

    [Fact]
    public void GetActiveConfiguration_NullConfig_ReturnsNull()
    {
        var opts = new GlobalStorageOptions
        {
            DefaultProviderType = StorageProviderType.GoogleCloudStorage,
            GoogleCloudStorage = null
        };
        opts.GetActiveConfiguration().Should().BeNull();
    }
}

#endregion
