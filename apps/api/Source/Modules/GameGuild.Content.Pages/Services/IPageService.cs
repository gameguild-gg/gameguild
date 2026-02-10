namespace GameGuild.Content.Pages;

/// <summary>Service for CRUD operations on <see cref="Page"/> and <see cref="PageSection"/>.</summary>
public interface IPageService
{
    // ── Pages ──
    Task<Page?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Page?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<IReadOnlyList<Page>> GetPagesAsync(PageType? type = null, PageStatus? status = null, string? locale = null, Guid? parentId = null, int skip = 0, int take = 50, CancellationToken ct = default);
    Task<Page> CreateAsync(CreatePageDto dto, CancellationToken ct = default);
    Task<Page?> UpdateAsync(Guid id, UpdatePageDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Page?> PublishAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<Page?> UnpublishAsync(Guid id, CancellationToken ct = default);

    // ── Sections ──
    Task<IReadOnlyList<PageSection>> GetSectionsAsync(Guid pageId, CancellationToken ct = default);
    Task<PageSection?> GetSectionByIdAsync(Guid sectionId, CancellationToken ct = default);
    Task<PageSection> CreateSectionAsync(Guid pageId, CreatePageSectionDto dto, CancellationToken ct = default);
    Task<PageSection?> UpdateSectionAsync(Guid sectionId, UpdatePageSectionDto dto, CancellationToken ct = default);
    Task<bool> DeleteSectionAsync(Guid sectionId, CancellationToken ct = default);
    Task ReorderSectionsAsync(Guid pageId, IReadOnlyList<Guid> orderedIds, CancellationToken ct = default);
}
