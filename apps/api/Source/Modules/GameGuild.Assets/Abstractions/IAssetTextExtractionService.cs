namespace GameGuild.Assets;

/// <summary>
/// Extracts searchable text from stored asset content.
/// </summary>
public interface IAssetTextExtractionService
{
    /// <summary>
    /// Extracts text from the given asset reference.
    /// </summary>
    Task<ExtractedAssetTextResult> ExtractAsync(AssetReference reference, CancellationToken ct = default);
}

/// <summary>
/// Searchable text extracted from an asset.
/// </summary>
public sealed record ExtractedAssetTextResult(
    string Text,
    string MimeType,
    string Source,
    bool UsedOcr,
    bool IsTruncated,
    IReadOnlyList<string> Warnings);
