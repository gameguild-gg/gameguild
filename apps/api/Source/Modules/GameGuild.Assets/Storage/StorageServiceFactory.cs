using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Assets.Storage;

/// <summary>
/// Factory for creating storage service instances based on provider configuration.
/// Supports both global (application-level) and tenant-level storage configurations.
/// </summary>
public interface IStorageServiceFactory
{
    /// <summary>
    /// Gets the storage service for the current tenant.
    /// Falls back to global configuration if tenant doesn't have custom storage.
    /// </summary>
    Task<IStorageService> GetStorageServiceAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>
    /// Gets the global (application-level) storage service.
    /// </summary>
    IStorageService GetGlobalStorageService();

    /// <summary>
    /// Creates a storage service from a specific configuration.
    /// Used for testing tenant configurations before saving.
    /// </summary>
    IStorageService CreateFromConfiguration(StorageProviderConfiguration configuration, string bucketName);

    /// <summary>
    /// Tests connectivity and permissions for a storage configuration.
    /// </summary>
    Task<StorageTestResult> TestConfigurationAsync(
        StorageProviderConfiguration configuration,
        string bucketName,
        CancellationToken ct = default);
}

/// <summary>
/// Result of testing a storage configuration.
/// </summary>
public sealed record StorageTestResult(
    bool Success,
    bool CanRead,
    bool CanWrite,
    bool CanDelete,
    bool BucketExists,
    string? ErrorMessage = null,
    TimeSpan? Latency = null);

/// <summary>
/// Repository for tenant storage configurations.
/// </summary>
public interface ITenantStorageConfigurationRepository
{
    Task<TenantStorageConfiguration?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<TenantStorageConfiguration?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TenantStorageConfiguration>> GetAllForTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(TenantStorageConfiguration configuration, CancellationToken ct = default);
    Task UpdateAsync(TenantStorageConfiguration configuration, CancellationToken ct = default);
    Task DeleteAsync(TenantStorageConfiguration configuration, CancellationToken ct = default);
}

/// <summary>
/// Global storage options with multi-provider support.
/// </summary>
public class GlobalStorageOptions
{
    public const string SectionName = "Assets:Storage";

    /// <summary>
    /// Default provider type for the application.
    /// </summary>
    public StorageProviderType DefaultProviderType { get; set; } = StorageProviderType.S3Compatible;

    /// <summary>
    /// Whether tenants can configure their own storage.
    /// </summary>
    public bool AllowTenantStorage { get; set; } = true;

    /// <summary>
    /// S3-compatible storage settings (AWS S3, MinIO, etc.)
    /// </summary>
    public S3CompatibleConfiguration? S3Compatible { get; set; }

    /// <summary>
    /// Google Cloud Storage settings.
    /// </summary>
    public GoogleCloudStorageConfiguration? GoogleCloudStorage { get; set; }

    /// <summary>
    /// Azure Blob Storage settings.
    /// </summary>
    public AzureBlobStorageConfiguration? AzureBlobStorage { get; set; }

    /// <summary>
    /// Cloudflare R2 settings.
    /// </summary>
    public CloudflareR2Configuration? CloudflareR2 { get; set; }

    /// <summary>
    /// Backblaze B2 settings.
    /// </summary>
    public BackblazeB2Configuration? BackblazeB2 { get; set; }

    /// <summary>
    /// Local filesystem settings (development only).
    /// </summary>
    public LocalFileSystemConfiguration? LocalFileSystem { get; set; }

    /// <summary>
    /// Primary bucket name for assets.
    /// </summary>
    public string BucketName { get; set; } = "assets";

    /// <summary>
    /// Bucket name for transformed assets.
    /// </summary>
    public string TransformedBucketName { get; set; } = "assets-transformed";

    /// <summary>
    /// Bucket name for quarantined files.
    /// </summary>
    public string QuarantineBucketName { get; set; } = "assets-quarantine";

    /// <summary>
    /// CDN URL prefix for serving assets.
    /// </summary>
    public string? CdnUrlPrefix { get; set; }

    /// <summary>
    /// Presigned URL expiry in minutes.
    /// </summary>
    public int PresignedUrlExpiryMinutes { get; set; } = 60;

    /// <summary>
    /// Gets the active provider configuration based on DefaultProviderType.
    /// </summary>
    public StorageProviderConfiguration? GetActiveConfiguration()
    {
        return DefaultProviderType switch
        {
            StorageProviderType.S3Compatible => S3Compatible,
            StorageProviderType.GoogleCloudStorage => GoogleCloudStorage,
            StorageProviderType.AzureBlobStorage => AzureBlobStorage,
            StorageProviderType.CloudflareR2 => CloudflareR2,
            StorageProviderType.BackblazeB2 => BackblazeB2,
            StorageProviderType.LocalFileSystem => LocalFileSystem,
            _ => S3Compatible // Default fallback
        };
    }
}

/// <summary>
/// Default implementation of storage service factory.
/// </summary>
public class StorageServiceFactory : IStorageServiceFactory
{
    private readonly GlobalStorageOptions _globalOptions;
    private readonly ITenantStorageConfigurationRepository _tenantConfigRepo;
    private readonly IStorageConfigurationEncryption _encryption;
    private readonly ILogger<StorageServiceFactory> _logger;
    private readonly IStorageService _globalStorageService;

    public StorageServiceFactory(
        IOptions<GlobalStorageOptions> globalOptions,
        ITenantStorageConfigurationRepository tenantConfigRepo,
        IStorageConfigurationEncryption encryption,
        IStorageService globalStorageService,
        ILogger<StorageServiceFactory> logger)
    {
        _globalOptions = globalOptions.Value;
        _tenantConfigRepo = tenantConfigRepo;
        _encryption = encryption;
        _globalStorageService = globalStorageService;
        _logger = logger;
    }

    public IStorageService GetGlobalStorageService() => _globalStorageService;

    public async Task<IStorageService> GetStorageServiceAsync(Guid tenantId, CancellationToken ct = default)
    {
        if (!_globalOptions.AllowTenantStorage)
        {
            return _globalStorageService;
        }

        var tenantConfig = await _tenantConfigRepo.GetByTenantIdAsync(tenantId, ct).ConfigureAwait(false);

        if (tenantConfig == null || !tenantConfig.IsEnabled)
        {
            return _globalStorageService;
        }

        try
        {
            var providerConfig = _encryption.Decrypt(tenantConfig.EncryptedConfiguration, tenantConfig.ProviderType);
            return CreateFromConfiguration(providerConfig, tenantConfig.BucketName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create tenant storage service for {TenantId}, falling back to global",
                tenantId);
            return _globalStorageService;
        }
    }

    public IStorageService CreateFromConfiguration(StorageProviderConfiguration configuration, string bucketName)
    {
        return configuration switch
        {
            S3CompatibleConfiguration s3Config => CreateS3Service(s3Config, bucketName),
            GoogleCloudStorageConfiguration gcsConfig => CreateGcsService(gcsConfig, bucketName),
            AzureBlobStorageConfiguration azureConfig => CreateAzureService(azureConfig, bucketName),
            CloudflareR2Configuration r2Config => CreateR2Service(r2Config, bucketName),
            BackblazeB2Configuration b2Config => CreateB2Service(b2Config, bucketName),
            LocalFileSystemConfiguration localConfig => CreateLocalService(localConfig, bucketName),
            _ => throw new NotSupportedException($"Provider type {configuration.ProviderType} is not supported")
        };
    }

    public async Task<StorageTestResult> TestConfigurationAsync(
        StorageProviderConfiguration configuration,
        string bucketName,
        CancellationToken ct = default)
    {
        var startTime = SystemClock.UtcNow;

        try
        {
            var service = CreateFromConfiguration(configuration, bucketName);

            // Test bucket exists
            var testKey = $".test-{Guid.NewGuid():N}";
            var testContent = "test"u8.ToArray();

            // Test write
            bool canWrite = false;
            bool canRead = false;
            bool canDelete = false;

            try
            {
                using var stream = new MemoryStream(testContent);
                await service.UploadAsync(stream, testKey, "text/plain", false, ct).ConfigureAwait(false);
                canWrite = true;
            }
            catch
            {
                // Write failed
            }

            // Test read
            if (canWrite)
            {
                try
                {
                    var exists = await service.ExistsAsync(bucketName, testKey, ct).ConfigureAwait(false);
                    canRead = exists;
                }
                catch
                {
                    // Read failed
                }
            }

            // Test delete
            if (canWrite)
            {
                try
                {
                    await service.DeleteAsync(bucketName, testKey, ct).ConfigureAwait(false);
                    canDelete = true;
                }
                catch
                {
                    // Delete failed - still try to clean up
                }
            }

            var latency = SystemClock.UtcNow - startTime;

            return new StorageTestResult(
                Success: canWrite && canRead,
                CanRead: canRead,
                CanWrite: canWrite,
                CanDelete: canDelete,
                BucketExists: true,
                Latency: latency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in Operation");
            throw;
        }
    }

    private IStorageService CreateS3Service(S3CompatibleConfiguration config, string bucketName)
    {
        var hasServiceUrl = !string.IsNullOrWhiteSpace(config.ServiceUrl);
        var s3Config = new Amazon.S3.AmazonS3Config
        {
            ForcePathStyle = config.ForcePathStyle,
            UseHttp = config.UseHttp
        };

        if (hasServiceUrl)
        {
            s3Config.ServiceURL = config.ServiceUrl;
            s3Config.AuthenticationRegion = config.Region;
        }
        else
        {
            s3Config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(config.Region);
        }

        Amazon.S3.IAmazonS3 s3Client;
        if (!string.IsNullOrEmpty(config.SessionToken))
        {
            s3Client = new Amazon.S3.AmazonS3Client(
                config.AccessKeyId,
                config.SecretAccessKey,
                config.SessionToken,
                s3Config);
        }
        else
        {
            s3Client = new Amazon.S3.AmazonS3Client(
                config.AccessKeyId,
                config.SecretAccessKey,
                s3Config);
        }

        var options = Options.Create(new StorageOptions
        {
            BucketName = bucketName,
            ServiceUrl = config.ServiceUrl ?? "",
            AccessKey = config.AccessKeyId,
            SecretKey = config.SecretAccessKey,
            Region = config.Region,
            ForcePathStyle = config.ForcePathStyle
        });

        return new S3StorageService(s3Client, options);
    }

    private IStorageService CreateGcsService(GoogleCloudStorageConfiguration config, string bucketName)
    {
        throw new NotSupportedException("Google Cloud Storage requires the Google.Cloud.Storage.V1 provider package and runtime credentials. Use S3-compatible, Cloudflare R2, Backblaze B2, or LocalFileSystem until that provider package is installed.");
    }

    private IStorageService CreateAzureService(AzureBlobStorageConfiguration config, string bucketName)
    {
        throw new NotSupportedException("Azure Blob Storage requires the Azure.Storage.Blobs provider package and runtime credentials. Use S3-compatible, Cloudflare R2, Backblaze B2, or LocalFileSystem until that provider package is installed.");
    }

    private IStorageService CreateR2Service(CloudflareR2Configuration config, string bucketName)
    {
        // R2 is S3-compatible, convert to S3 config
        var s3Config = new S3CompatibleConfiguration
        {
            ServiceUrl = config.GetEndpointUrl(),
            AccessKeyId = config.AccessKeyId,
            SecretAccessKey = config.SecretAccessKey,
            Region = "auto",
            ForcePathStyle = true
        };
        return CreateS3Service(s3Config, bucketName);
    }

    private IStorageService CreateB2Service(BackblazeB2Configuration config, string bucketName)
    {
        // B2 has S3-compatible API
        var s3Config = new S3CompatibleConfiguration
        {
            ServiceUrl = $"https://{config.Endpoint}",
            AccessKeyId = config.ApplicationKeyId,
            SecretAccessKey = config.ApplicationKey,
            Region = config.Region,
            ForcePathStyle = true
        };
        return CreateS3Service(s3Config, bucketName);
    }

    private IStorageService CreateLocalService(LocalFileSystemConfiguration config, string bucketName)
    {
        return new LocalFileSystemStorageService(
            config,
            bucketName,
            _globalOptions.TransformedBucketName,
            _globalOptions.QuarantineBucketName);
    }
}

/// <summary>
/// Service for encrypting/decrypting storage configuration credentials.
/// </summary>
public interface IStorageConfigurationEncryption
{
    string Encrypt(StorageProviderConfiguration configuration);
    StorageProviderConfiguration Decrypt(string encryptedJson, StorageProviderType providerType);
}

/// <summary>
/// Default encryption implementation using data protection API.
/// </summary>
public class StorageConfigurationEncryption : IStorageConfigurationEncryption
{
    private readonly Microsoft.AspNetCore.DataProtection.IDataProtector _protector;

    public StorageConfigurationEncryption(
        Microsoft.AspNetCore.DataProtection.IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("GameGuild.Assets.Storage.Configuration");
    }

    public string Encrypt(StorageProviderConfiguration configuration)
    {
        var json = JsonSerializer.Serialize(configuration, configuration.GetType());
        // IDataProtector.Protect(string) returns string directly
        return _protector.Protect(json);
    }

    public StorageProviderConfiguration Decrypt(string encryptedJson, StorageProviderType providerType)
    {
        // IDataProtector.Unprotect(string) returns string directly
        var json = _protector.Unprotect(encryptedJson);

        return providerType switch
        {
            StorageProviderType.S3Compatible =>
                JsonSerializer.Deserialize<S3CompatibleConfiguration>(json)!,
            StorageProviderType.GoogleCloudStorage =>
                JsonSerializer.Deserialize<GoogleCloudStorageConfiguration>(json)!,
            StorageProviderType.AzureBlobStorage =>
                JsonSerializer.Deserialize<AzureBlobStorageConfiguration>(json)!,
            StorageProviderType.CloudflareR2 =>
                JsonSerializer.Deserialize<CloudflareR2Configuration>(json)!,
            StorageProviderType.BackblazeB2 =>
                JsonSerializer.Deserialize<BackblazeB2Configuration>(json)!,
            StorageProviderType.LocalFileSystem =>
                JsonSerializer.Deserialize<LocalFileSystemConfiguration>(json)!,
            _ => throw new NotSupportedException($"Provider type {providerType} is not supported")
        };
    }
}
