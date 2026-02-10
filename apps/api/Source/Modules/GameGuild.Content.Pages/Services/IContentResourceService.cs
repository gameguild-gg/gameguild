namespace GameGuild.Content.Pages;

/// <summary>Service for CRUD on <see cref="ContentResource"/>.</summary>
public interface IContentResourceService
{
    Task<ContentResource?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ContentResource?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<ContentResource>> ListAsync(
        ContentResourceType? type = null,
        ContentResourceStatus? status = null,
        string? locale = null,
        string? categorySlug = null,
        bool? isFeatured = null,
        string? search = null,
        int skip = 0,
        int take = 50,
        CancellationToken ct = default);
    Task<ContentResource> CreateAsync(CreateContentResourceDto dto, CancellationToken ct = default);
    Task<ContentResource?> UpdateAsync(Guid id, UpdateContentResourceDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<ContentResource?> PublishAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task IncrementViewCountAsync(Guid id, CancellationToken ct = default);
}
