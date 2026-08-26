using System.Security.Cryptography;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace GameGuild.Compliance.KYC;

public sealed class ComplianceRawObjectStoreOptions
{
    public const string SectionName = "Compliance:EvidenceObjectStore";

    public bool Enabled { get; set; }
    public string BucketName { get; set; } = string.Empty;
    public string KmsKeyId { get; set; } = string.Empty;
    public string Prefix { get; set; } = "compliance-evidence";
}

public sealed record ComplianceRawObjectReference(string Reference, string PayloadHash);

public interface IComplianceRawObjectStore
{
    ValueTask<ComplianceRawObjectReference> PutAsync(
        string provider,
        string environment,
        string providerEventId,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken);
}

public sealed class S3ComplianceRawObjectStore : IComplianceRawObjectStore
{
    private readonly IAmazonS3 _s3;
    private readonly ComplianceRawObjectStoreOptions _options;

    public S3ComplianceRawObjectStore(
        IAmazonS3 s3,
        IOptions<ComplianceRawObjectStoreOptions> options)
    {
        _s3 = s3 ?? throw new ArgumentNullException(nameof(s3));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
    }

    public async ValueTask<ComplianceRawObjectReference> PutAsync(
        string provider,
        string environment,
        string providerEventId,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(environment);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerEventId);
        if (payload.IsEmpty) throw new ArgumentException("Compliance evidence payload cannot be empty.", nameof(payload));

        var payloadHash = Convert.ToHexStringLower(SHA256.HashData(payload.Span));
        var key = string.Join('/',
            _options.Prefix.Trim('/'),
            Uri.EscapeDataString(provider.Trim().ToLowerInvariant()),
            Uri.EscapeDataString(environment.Trim().ToLowerInvariant()),
            payloadHash[..2],
            payloadHash + ".bin");
        using var stream = new MemoryStream(payload.ToArray(), writable: false);
        var response = await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.BucketName.Trim(),
            Key = key,
            InputStream = stream,
            AutoCloseStream = false,
            ContentType = "application/octet-stream",
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AWSKMS,
            ServerSideEncryptionKeyManagementServiceKeyId = _options.KmsKeyId.Trim(),
            Metadata =
            {
                ["payload-sha256"] = payloadHash,
                ["provider-event-hash"] = Convert.ToHexStringLower(
                    SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(providerEventId)))
            }
        }, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(response.ETag))
            throw new ComplianceRawObjectStoreUnavailableException(
                "Encrypted compliance evidence storage did not return an object identity.");
        return new ComplianceRawObjectReference($"s3://{_options.BucketName.Trim()}/{key}", payloadHash);
    }

    private void EnsureConfigured()
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.BucketName) ||
            string.IsNullOrWhiteSpace(_options.KmsKeyId) || string.IsNullOrWhiteSpace(_options.Prefix))
            throw new ComplianceRawObjectStoreUnavailableException(
                "Compliance evidence intake is disabled until encrypted object storage is configured.");
    }
}

public sealed class UnavailableComplianceRawObjectStore : IComplianceRawObjectStore
{
    public ValueTask<ComplianceRawObjectReference> PutAsync(
        string provider,
        string environment,
        string providerEventId,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken) =>
        ValueTask.FromException<ComplianceRawObjectReference>(
            new ComplianceRawObjectStoreUnavailableException(
                "Compliance evidence intake is disabled until encrypted object storage is configured."));
}

public sealed class ComplianceRawObjectStoreUnavailableException(string message) : InvalidOperationException(message);
