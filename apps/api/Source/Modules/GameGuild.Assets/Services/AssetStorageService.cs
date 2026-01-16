using Microsoft.Extensions.Options;

namespace GameGuild.Assets;

/// <summary>
/// Options for S3-compatible storage.
/// </summary>
public class AssetStorageOptions
{
    public const string SectionName = "Assets:Storage";

    public string BucketName { get; set; } = "assets";
    public string TransformedBucketName { get; set; } = "assets-transformed";
    public string ServiceUrl { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public bool ForcePathStyle { get; set; } = true; // For MinIO compatibility
    public int PresignedUrlExpiryMinutes { get; set; } = 60;
}

/// <summary>
/// Implementation of S3-compatible storage operations.
/// </summary>
public class AssetStorageService : IAssetStorageService
{
    private readonly Amazon.S3.IAmazonS3 _s3Client;
    private readonly AssetStorageOptions _options;

    public AssetStorageService(
        Amazon.S3.IAmazonS3 s3Client,
        IOptions<AssetStorageOptions> options)
    {
        _s3Client = s3Client;
        _options = options.Value;
    }

    public async Task<StorageUploadResult> UploadAsync(
        Stream content,
        string contentHash,
        string mimeType,
        bool isTransformed = false,
        CancellationToken ct = default)
    {
        var bucketName = isTransformed ? _options.TransformedBucketName : _options.BucketName;
        var objectKey = GenerateObjectKey(contentHash, mimeType, isTransformed);

        var request = new Amazon.S3.Model.PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            InputStream = content,
            ContentType = mimeType,
            AutoCloseStream = false,
            DisablePayloadSigning = true
        };

        request.Metadata.Add("content-hash", contentHash);
        request.Metadata.Add("uploaded-at", DateTime.UtcNow.ToString("O"));

        await _s3Client.PutObjectAsync(request, ct);

        return new StorageUploadResult(bucketName, objectKey);
    }

    public async Task<Stream> DownloadAsync(
        string bucketName,
        string objectKey,
        CancellationToken ct = default)
    {
        var request = new Amazon.S3.Model.GetObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey
        };

        var response = await _s3Client.GetObjectAsync(request, ct);
        return response.ResponseStream;
    }

    public async Task<bool> ExistsAsync(
        string bucketName,
        string objectKey,
        CancellationToken ct = default)
    {
        try
        {
            var request = new Amazon.S3.Model.GetObjectMetadataRequest
            {
                BucketName = bucketName,
                Key = objectKey
            };

            await _s3Client.GetObjectMetadataAsync(request, ct);
            return true;
        }
        catch (Amazon.S3.AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<StorageMetadata?> GetMetadataAsync(
        string bucketName,
        string objectKey,
        CancellationToken ct = default)
    {
        try
        {
            var request = new Amazon.S3.Model.GetObjectMetadataRequest
            {
                BucketName = bucketName,
                Key = objectKey
            };

            var response = await _s3Client.GetObjectMetadataAsync(request, ct);

            return new StorageMetadata(
                response.ContentLength,
                response.Headers.ContentType,
                response.ETag,
                response.LastModified);
        }
        catch (Amazon.S3.AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(
        string bucketName,
        string objectKey,
        CancellationToken ct = default)
    {
        var request = new Amazon.S3.Model.DeleteObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey
        };

        await _s3Client.DeleteObjectAsync(request, ct);
    }

    public async Task<string> GeneratePresignedUrlAsync(
        string bucketName,
        string objectKey,
        TimeSpan expiry,
        bool isDownload = true,
        CancellationToken ct = default)
    {
        var request = new Amazon.S3.Model.GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            Expires = DateTime.UtcNow.Add(expiry),
            Verb = isDownload ? Amazon.S3.HttpVerb.GET : Amazon.S3.HttpVerb.PUT
        };

        return await Task.FromResult(_s3Client.GetPreSignedURL(request));
    }

    public async Task<string> InitiateMultipartUploadAsync(
        string mimeType,
        CancellationToken ct = default)
    {
        var objectKey = $"multipart/{Guid.NewGuid()}";
        
        var request = new Amazon.S3.Model.InitiateMultipartUploadRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            ContentType = mimeType
        };

        var response = await _s3Client.InitiateMultipartUploadAsync(request, ct);
        return response.UploadId;
    }

    public async Task<string> UploadPartAsync(
        string uploadId,
        string objectKey,
        int partNumber,
        Stream content,
        CancellationToken ct = default)
    {
        var request = new Amazon.S3.Model.UploadPartRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            UploadId = uploadId,
            PartNumber = partNumber,
            InputStream = content
        };

        var response = await _s3Client.UploadPartAsync(request, ct);
        return response.ETag;
    }

    public async Task<StorageUploadResult> CompleteMultipartUploadAsync(
        string uploadId,
        string objectKey,
        IReadOnlyList<string> partETags,
        CancellationToken ct = default)
    {
        var request = new Amazon.S3.Model.CompleteMultipartUploadRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            UploadId = uploadId
        };

        for (int i = 0; i < partETags.Count; i++)
        {
            request.PartETags.Add(new Amazon.S3.Model.PartETag(i + 1, partETags[i]));
        }

        await _s3Client.CompleteMultipartUploadAsync(request, ct);

        return new StorageUploadResult(_options.BucketName, objectKey);
    }

    public async Task AbortMultipartUploadAsync(
        string uploadId,
        string objectKey,
        CancellationToken ct = default)
    {
        var request = new Amazon.S3.Model.AbortMultipartUploadRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            UploadId = uploadId
        };

        await _s3Client.AbortMultipartUploadAsync(request, ct);
    }

    public async Task UploadToQuarantineAsync(
        Stream content,
        string objectKey,
        IDictionary<string, string> metadata,
        CancellationToken ct = default)
    {
        // Quarantine bucket name follows convention: {bucket}-quarantine
        var quarantineBucket = $"{_options.BucketName}-quarantine";

        var request = new Amazon.S3.Model.PutObjectRequest
        {
            BucketName = quarantineBucket,
            Key = objectKey,
            InputStream = content,
            ContentType = "application/octet-stream", // Don't trust original MIME type for quarantined files
            AutoCloseStream = false,
            DisablePayloadSigning = true
        };

        // Add all provided metadata
        foreach (var (key, value) in metadata)
        {
            request.Metadata.Add(key, value);
        }

        request.Metadata.Add("quarantined-at", DateTime.UtcNow.ToString("O"));

        await _s3Client.PutObjectAsync(request, ct);
    }

    private static string GenerateObjectKey(string contentHash, string mimeType, bool isTransformed)
    {
        // Structure: {prefix}/{hash[0:2]}/{hash[2:4]}/{hash}.{ext}
        var prefix = isTransformed ? "transformed" : "content";
        var extension = GetExtensionFromMimeType(mimeType);
        
        return $"{prefix}/{contentHash[..2]}/{contentHash[2..4]}/{contentHash}.{extension}";
    }

    private static string GetExtensionFromMimeType(string mimeType)
    {
        return mimeType switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "image/gif" => "gif",
            "image/webp" => "webp",
            "image/svg+xml" => "svg",
            "video/mp4" => "mp4",
            "video/webm" => "webm",
            "audio/mpeg" => "mp3",
            "audio/wav" => "wav",
            "audio/ogg" => "ogg",
            "application/pdf" => "pdf",
            "application/zip" => "zip",
            "text/plain" => "txt",
            "application/json" => "json",
            _ => "bin"
        };
    }
}
