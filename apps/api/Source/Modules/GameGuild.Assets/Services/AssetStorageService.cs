using Microsoft.Extensions.Options;

namespace GameGuild.Assets;

/// <summary>
/// Options for S3-compatible storage.
/// </summary>
public class AssetStorageOptions
{
    public const string SectionName = "Assets:Storage";

    public string BucketName { get; set; } = "assets";
    public string TransformedBucketName { get; set; } = string.Empty;
    public string QuarantineBucketName { get; set; } = string.Empty;
    public string ServiceUrl { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public bool ForcePathStyle { get; set; } = true; // For MinIO compatibility
    public int PresignedUrlExpiryMinutes { get; set; } = 60;

    public string GetTransformedBucketName() =>
        string.IsNullOrWhiteSpace(TransformedBucketName) ? BucketName : TransformedBucketName;

    public string GetQuarantineBucketName() =>
        string.IsNullOrWhiteSpace(QuarantineBucketName) ? BucketName : QuarantineBucketName;
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
        var bucketName = isTransformed ? _options.GetTransformedBucketName() : _options.BucketName;
        var objectKey = GenerateObjectKey(contentHash, mimeType, isTransformed);

        await EnsureBucketExistsAsync(bucketName, ct).ConfigureAwait(false);

        var request = new Amazon.S3.Model.PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            InputStream = content,
            ContentType = mimeType,
            AutoCloseStream = false
        };

        request.Metadata.Add("content-hash", contentHash);
        request.Metadata.Add("uploaded-at", SystemClock.UtcNow.ToString("O"));

        await _s3Client.PutObjectAsync(request, ct).ConfigureAwait(false);

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

        var response = await _s3Client.GetObjectAsync(request, ct).ConfigureAwait(false);
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

            await _s3Client.GetObjectMetadataAsync(request, ct).ConfigureAwait(false);
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

            var response = await _s3Client.GetObjectMetadataAsync(request, ct).ConfigureAwait(false);

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

        await _s3Client.DeleteObjectAsync(request, ct).ConfigureAwait(false);
    }

    public async Task<string> GeneratePresignedUrlAsync(
        string bucketName,
        string objectKey,
        TimeSpan expiry,
        bool isDownload = true,
        CancellationToken ct = default)
    {
        var useHttp = Uri.TryCreate(_options.ServiceUrl, UriKind.Absolute, out var serviceUri)
            && string.Equals(serviceUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);

        var request = new Amazon.S3.Model.GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            Expires = SystemClock.UtcNow.Add(expiry),
            Verb = isDownload ? Amazon.S3.HttpVerb.GET : Amazon.S3.HttpVerb.PUT,
            Protocol = useHttp ? Amazon.S3.Protocol.HTTP : Amazon.S3.Protocol.HTTPS
        };

        return await Task.FromResult(_s3Client.GetPreSignedURL(request));
    }

    public async Task<string> InitiateMultipartUploadAsync(
        string mimeType,
        CancellationToken ct = default)
    {
        var objectKey = $"multipart/{Guid.NewGuid()}";

        await EnsureBucketExistsAsync(_options.BucketName, ct).ConfigureAwait(false);

        var request = new Amazon.S3.Model.InitiateMultipartUploadRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            ContentType = mimeType
        };

        var response = await _s3Client.InitiateMultipartUploadAsync(request, ct).ConfigureAwait(false);
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

        var response = await _s3Client.UploadPartAsync(request, ct).ConfigureAwait(false);
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

        await _s3Client.CompleteMultipartUploadAsync(request, ct).ConfigureAwait(false);

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

        await _s3Client.AbortMultipartUploadAsync(request, ct).ConfigureAwait(false);
    }

    public async Task UploadToQuarantineAsync(
        Stream content,
        string objectKey,
        IDictionary<string, string> metadata,
        CancellationToken ct = default)
    {
        var quarantineBucket = _options.GetQuarantineBucketName();

        await EnsureBucketExistsAsync(quarantineBucket, ct).ConfigureAwait(false);

        var request = new Amazon.S3.Model.PutObjectRequest
        {
            BucketName = quarantineBucket,
            Key = objectKey,
            InputStream = content,
            ContentType = "application/octet-stream", // Don't trust original MIME type for quarantined files
            AutoCloseStream = false
        };

        // Add all provided metadata
        foreach (var (key, value) in metadata)
        {
            request.Metadata.Add(key, value);
        }

        request.Metadata.Add("quarantined-at", SystemClock.UtcNow.ToString("O"));

        await _s3Client.PutObjectAsync(request, ct).ConfigureAwait(false);
    }

    private async Task EnsureBucketExistsAsync(string bucketName, CancellationToken ct)
    {
        try
        {
            await _s3Client.PutBucketAsync(
                new Amazon.S3.Model.PutBucketRequest
                {
                    BucketName = bucketName,
                    UseClientRegion = true
                },
                ct).ConfigureAwait(false);
        }
        catch (Amazon.S3.AmazonS3Exception ex) when (
            string.Equals(ex.ErrorCode, "BucketAlreadyOwnedByYou", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(ex.ErrorCode, "BucketAlreadyExists", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(ex.ErrorCode, "AccessDenied", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(ex.ErrorCode, "Forbidden", StringComparison.OrdinalIgnoreCase) ||
            ex.StatusCode == System.Net.HttpStatusCode.Forbidden ||
            ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
        }
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
