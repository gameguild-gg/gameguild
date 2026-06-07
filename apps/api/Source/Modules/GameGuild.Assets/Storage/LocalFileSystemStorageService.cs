using System.Security.Cryptography;
using System.Text.Json;

namespace GameGuild.Assets.Storage;

public sealed class LocalFileSystemStorageService : IStorageService
{
    private const string MultipartDirectoryName = ".multipart";
    private readonly string _basePath;
    private readonly string _bucketName;
    private readonly string _transformedBucketName;
    private readonly string _quarantineBucketName;
    private readonly string? _serveUrlPrefix;

    public LocalFileSystemStorageService(
        LocalFileSystemConfiguration configuration,
        string bucketName,
        string transformedBucketName,
        string quarantineBucketName)
    {
        _basePath = Path.GetFullPath(configuration.BasePath);
        _bucketName = bucketName;
        _transformedBucketName = transformedBucketName;
        _quarantineBucketName = quarantineBucketName;
        _serveUrlPrefix = configuration.ServeUrlPrefix?.TrimEnd('/');

        Directory.CreateDirectory(_basePath);
    }

    public async Task<StorageUploadResult> UploadAsync(
        Stream content,
        string contentHash,
        string mimeType,
        bool isTransformed = false,
        CancellationToken ct = default)
    {
        var bucketName = isTransformed ? _transformedBucketName : _bucketName;
        var objectKey = GenerateObjectKey(contentHash, mimeType, isTransformed);
        var path = ResolveObjectPath(bucketName, objectKey);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using (var file = File.Create(path))
        {
            await content.CopyToAsync(file, ct).ConfigureAwait(false);
        }

        var metadata = new LocalStorageMetadata(mimeType, contentHash, SystemClock.UtcNow);
        await WriteMetadataAsync(path, metadata, ct).ConfigureAwait(false);

        return new StorageUploadResult(bucketName, objectKey, metadata.ETag, new FileInfo(path).Length);
    }

    public Task<Stream> DownloadAsync(string bucketName, string objectKey, CancellationToken ct = default)
    {
        var path = ResolveObjectPath(bucketName, objectKey);
        Stream stream = File.OpenRead(path);
        return Task.FromResult(stream);
    }

    public Task<string> GeneratePresignedUrlAsync(
        string bucketName,
        string objectKey,
        TimeSpan expiry,
        bool isDownload = true,
        CancellationToken ct = default)
    {
        _ = ResolveObjectPath(bucketName, objectKey);

        if (!string.IsNullOrWhiteSpace(_serveUrlPrefix))
        {
            return Task.FromResult($"{_serveUrlPrefix}/{Uri.EscapeDataString(bucketName)}/{Uri.EscapeDataString(objectKey)}");
        }

        var expires = DateTimeOffset.UtcNow.Add(expiry).ToUnixTimeSeconds();
        return Task.FromResult($"local://{bucketName}/{objectKey}?expires={expires}&download={isDownload.ToString().ToLowerInvariant()}");
    }

    public Task DeleteAsync(string bucketName, string objectKey, CancellationToken ct = default)
    {
        var path = ResolveObjectPath(bucketName, objectKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        var metadataPath = GetMetadataPath(path);
        if (File.Exists(metadataPath))
        {
            File.Delete(metadataPath);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string bucketName, string objectKey, CancellationToken ct = default)
    {
        var path = ResolveObjectPath(bucketName, objectKey);
        return Task.FromResult(File.Exists(path));
    }

    public async Task<StorageMetadata?> GetMetadataAsync(string bucketName, string objectKey, CancellationToken ct = default)
    {
        var path = ResolveObjectPath(bucketName, objectKey);
        if (!File.Exists(path))
        {
            return null;
        }

        var info = new FileInfo(path);
        var metadata = await ReadMetadataAsync(path, ct).ConfigureAwait(false);

        return new StorageMetadata(
            info.Length,
            metadata?.MimeType ?? "application/octet-stream",
            metadata?.ETag ?? ComputeFileETag(path),
            info.LastWriteTimeUtc);
    }

    public Task<string> InitiateMultipartUploadAsync(string mimeType, CancellationToken ct = default)
    {
        var uploadId = Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(GetMultipartDirectory(uploadId));
        return Task.FromResult(uploadId);
    }

    public async Task<string> UploadPartAsync(
        string uploadId,
        string objectKey,
        int partNumber,
        Stream content,
        CancellationToken ct = default)
    {
        if (partNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(partNumber), partNumber, "Part number must be greater than zero.");
        }

        var multipartDirectory = GetMultipartDirectory(uploadId);
        Directory.CreateDirectory(multipartDirectory);

        var partPath = Path.Combine(multipartDirectory, $"{partNumber:D8}.part");
        await using (var file = File.Create(partPath))
        {
            await content.CopyToAsync(file, ct).ConfigureAwait(false);
        }

        return ComputeFileETag(partPath);
    }

    public async Task<StorageUploadResult> CompleteMultipartUploadAsync(
        string uploadId,
        string objectKey,
        IReadOnlyList<string> partETags,
        CancellationToken ct = default)
    {
        var multipartDirectory = GetMultipartDirectory(uploadId);
        var destinationPath = ResolveObjectPath(_bucketName, objectKey);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        await using (var destination = File.Create(destinationPath))
        {
            foreach (var partPath in Directory.EnumerateFiles(multipartDirectory, "*.part").OrderBy(path => path))
            {
                await using var part = File.OpenRead(partPath);
                await part.CopyToAsync(destination, ct).ConfigureAwait(false);
            }
        }

        Directory.Delete(multipartDirectory, recursive: true);

        var etag = ComputeFileETag(destinationPath);
        await WriteMetadataAsync(
            destinationPath,
            new LocalStorageMetadata("application/octet-stream", etag, SystemClock.UtcNow),
            ct).ConfigureAwait(false);

        return new StorageUploadResult(_bucketName, objectKey, etag, new FileInfo(destinationPath).Length);
    }

    public Task AbortMultipartUploadAsync(string uploadId, string objectKey, CancellationToken ct = default)
    {
        var multipartDirectory = GetMultipartDirectory(uploadId);
        if (Directory.Exists(multipartDirectory))
        {
            Directory.Delete(multipartDirectory, recursive: true);
        }

        return Task.CompletedTask;
    }

    public async Task UploadToQuarantineAsync(
        Stream content,
        string objectKey,
        IDictionary<string, string> metadata,
        CancellationToken ct = default)
    {
        var path = ResolveObjectPath(_quarantineBucketName, objectKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using (var file = File.Create(path))
        {
            await content.CopyToAsync(file, ct).ConfigureAwait(false);
        }

        var localMetadata = new LocalStorageMetadata(
            metadata.TryGetValue("content-type", out var mimeType) ? mimeType : "application/octet-stream",
            ComputeFileETag(path),
            SystemClock.UtcNow);
        await WriteMetadataAsync(path, localMetadata, ct).ConfigureAwait(false);
    }

    private string ResolveObjectPath(string bucketName, string objectKey)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
        {
            throw new ArgumentException("Bucket name is required.", nameof(bucketName));
        }

        if (string.IsNullOrWhiteSpace(objectKey))
        {
            throw new ArgumentException("Object key is required.", nameof(objectKey));
        }

        var bucketRoot = Path.GetFullPath(Path.Combine(_basePath, bucketName));
        var relativeKey = objectKey
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
        var path = Path.GetFullPath(Path.Combine(bucketRoot, relativeKey));

        if (!path.StartsWith(bucketRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(path, bucketRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Object key resolves outside the configured storage root.");
        }

        return path;
    }

    private string GetMultipartDirectory(string uploadId)
    {
        if (string.IsNullOrWhiteSpace(uploadId))
        {
            throw new ArgumentException("Upload ID is required.", nameof(uploadId));
        }

        return Path.Combine(_basePath, MultipartDirectoryName, uploadId);
    }

    private static async Task WriteMetadataAsync(string path, LocalStorageMetadata metadata, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(metadata);
        await File.WriteAllTextAsync(GetMetadataPath(path), json, ct).ConfigureAwait(false);
    }

    private static async Task<LocalStorageMetadata?> ReadMetadataAsync(string path, CancellationToken ct)
    {
        var metadataPath = GetMetadataPath(path);
        if (!File.Exists(metadataPath))
        {
            return null;
        }

        var json = await File.ReadAllTextAsync(metadataPath, ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<LocalStorageMetadata>(json);
    }

    private static string GetMetadataPath(string path) => $"{path}.metadata.json";

    private static string ComputeFileETag(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string GenerateObjectKey(string contentHash, string mimeType, bool isTransformed)
    {
        if (contentHash.Length < 4)
        {
            throw new ArgumentException("Content hash must be at least four characters.", nameof(contentHash));
        }

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

    private sealed record LocalStorageMetadata(string MimeType, string ETag, DateTime UploadedAt);
}
