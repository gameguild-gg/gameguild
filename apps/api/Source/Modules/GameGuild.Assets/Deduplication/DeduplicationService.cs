using System.Security.Cryptography;

namespace GameGuild.Assets.Deduplication;

/// <summary>
/// Service for content-based deduplication using hashing.
/// </summary>
public interface IDeduplicationService
{
    /// <summary>
    /// Computes the SHA-256 hash of content for deduplication.
    /// </summary>
    Task<string> ComputeContentHashAsync(Stream content, CancellationToken ct = default);

    /// <summary>
    /// Computes a perceptual hash for images (for near-duplicate detection).
    /// </summary>
    Task<string?> ComputePerceptualHashAsync(Stream content, string mimeType, CancellationToken ct = default);

    /// <summary>
    /// Checks if content with this hash already exists.
    /// </summary>
    Task<Guid?> FindExistingContentAsync(string contentHash, CancellationToken ct = default);
}

/// <summary>
/// Configuration for deduplication.
/// </summary>
public class DeduplicationOptions
{
    public const string SectionName = "Assets:Deduplication";

    /// <summary>
    /// Whether content deduplication is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whether perceptual hashing is enabled for images.
    /// </summary>
    public bool EnablePerceptualHashing { get; set; } = true;

    /// <summary>
    /// Hamming distance threshold for perceptual hash matching.
    /// </summary>
    public int PerceptualHashThreshold { get; set; } = 5;
}

/// <summary>
/// Implementation of content deduplication.
/// </summary>
public class DeduplicationService : IDeduplicationService
{
    private readonly IAssetContentRepository _contentRepository;
    private readonly DeduplicationOptions _options;

    public DeduplicationService(
        IAssetContentRepository contentRepository,
        Microsoft.Extensions.Options.IOptions<DeduplicationOptions> options)
    {
        _contentRepository = contentRepository;
        _options = options.Value;
    }

    public async Task<string> ComputeContentHashAsync(Stream content, CancellationToken ct = default)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(content, ct);
        content.Position = 0; // Reset stream position for subsequent reads
        
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public Task<string?> ComputePerceptualHashAsync(Stream content, string mimeType, CancellationToken ct = default)
    {
        if (!_options.EnablePerceptualHashing)
        {
            return Task.FromResult<string?>(null);
        }

        // Only compute perceptual hash for images
        if (!mimeType.StartsWith("image/"))
        {
            return Task.FromResult<string?>(null);
        }

        // TODO: Implement perceptual hashing using a library like ImageSharp
        // For now, return null (perceptual hashing not implemented)
        return Task.FromResult<string?>(null);
    }

    public async Task<Guid?> FindExistingContentAsync(string contentHash, CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            return null;
        }

        var existing = await _contentRepository.GetByContentHashAsync(contentHash, ct);
        return existing?.Id;
    }
}
