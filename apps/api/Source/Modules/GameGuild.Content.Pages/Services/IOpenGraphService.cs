namespace GameGuild.Content.Pages;

/// <summary>Resolves OpenGraph / SEO metadata by slug, looking up pages then content resources.</summary>
public interface IOpenGraphService
{
    Task<OpenGraphMetadataDto?> ResolveAsync(string slug, CancellationToken ct = default);
}
