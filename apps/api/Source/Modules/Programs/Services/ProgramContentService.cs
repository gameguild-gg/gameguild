using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Programs.Interfaces;
using GameGuild.Modules.Programs.Models;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Programs.Services;

/// <summary>
/// Service implementation for ProgramContent management with full DAC permission support
/// Handles CRUD operations, hierarchical content structure, and content ordering
/// </summary>
public class ProgramContentService(ApplicationDbContext context) : IProgramContentService {
  public async Task<ProgramContent> CreateContentAsync(ProgramContent content) {
    // Set creation timestamp
    content.CreatedAt = DateTime.UtcNow;
    content.UpdatedAt = DateTime.UtcNow;

    // If no sort order is specified, put it at the end
    if (content.SortOrder == 0) {
      var maxOrder = await context.ProgramContents
                                  .Where(pc => pc.ProgramId == content.ProgramId && pc.ParentId == content.ParentId && !pc.IsDeleted)
                                  .MaxAsync(pc => (int?)pc.SortOrder) ??
                     0;
      content.SortOrder = maxOrder + 1;
    }

    context.ProgramContents.Add(content);
    await context.SaveChangesAsync();

    return content;
  }

  public async Task<ProgramContent?> GetContentByIdAsync(Guid id) {
    return await context.ProgramContents.Include(pc => pc.Program)
                        .Include(pc => pc.Parent)
                        .Include(pc => pc.Children.Where(c => !c.IsDeleted))
                        .Where(pc => !pc.IsDeleted)
                        .FirstOrDefaultAsync(pc => pc.Id == id);
  }

  public async Task<IEnumerable<ProgramContent>> GetContentByProgramAsync(Guid programId) {
    return await context.ProgramContents.Include(pc => pc.Parent)
                        .Include(pc => pc.Children.Where(c => !c.IsDeleted))
                        .Where(pc => pc.ProgramId == programId && !pc.IsDeleted)
                        .OrderBy(pc => pc.SortOrder)
                        .ToListAsync();
  }

  public async Task<IEnumerable<ProgramContent>> GetContentByParentAsync(Guid parentId) {
    return await context.ProgramContents.Include(pc => pc.Children.Where(c => !c.IsDeleted))
                        .Where(pc => pc.ParentId == parentId && !pc.IsDeleted)
                        .OrderBy(pc => pc.SortOrder)
                        .ToListAsync();
  }

  public async Task<IEnumerable<ProgramContent>> GetTopLevelContentAsync(Guid programId) {
    return await context.ProgramContents.Include(pc => pc.Children.Where(c => !c.IsDeleted))
                        .Where(pc => pc.ProgramId == programId && pc.ParentId == null && !pc.IsDeleted)
                        .OrderBy(pc => pc.SortOrder)
                        .ToListAsync();
  }

  public async Task<ProgramContent> UpdateContentAsync(ProgramContent content) {
    var existingContent =
      await context.ProgramContents.FirstOrDefaultAsync(pc => pc.Id == content.Id && !pc.IsDeleted);

    if (existingContent == null) throw new InvalidOperationException($"ProgramContent with ID {content.Id} not found or has been deleted");

    // Update properties
    existingContent.Title = content.Title;
    existingContent.Description = content.Description;
    existingContent.Type = content.Type;
    existingContent.Body = content.Body;
    existingContent.SortOrder = content.SortOrder;
    existingContent.IsRequired = content.IsRequired;
    existingContent.GradingMethod = content.GradingMethod;
    existingContent.MaxPoints = content.MaxPoints;
    existingContent.EstimatedMinutes = content.EstimatedMinutes;
    existingContent.Visibility = content.Visibility;
    existingContent.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync();

    return existingContent;
  }

  public async Task<bool> DeleteContentAsync(Guid id) {
    var content = await context.ProgramContents.Include(pc => pc.Children)
                               .FirstOrDefaultAsync(pc => pc.Id == id && !pc.IsDeleted);

    if (content == null) return false;

    // Soft delete the content and all its children
    content.DeletedAt = DateTime.UtcNow;

    foreach (var child in content.Children.Where(c => !c.IsDeleted)) child.DeletedAt = DateTime.UtcNow;

    await context.SaveChangesAsync();

    return true;
  }

  public async Task<bool> ReorderContentAsync(Guid programId, List<(Guid contentId, int sortOrder)> newOrder) {
    // Get all content items to reorder
    var contentIds = newOrder.Select(x => x.contentId).ToList();
    var contentItems = await context.ProgramContents
                                    .Where(pc => contentIds.Contains(pc.Id) && pc.ProgramId == programId && !pc.IsDeleted)
                                    .ToListAsync();

    if (contentItems.Count != newOrder.Count) return false; // Some content items not found

    // Update sort orders
    foreach (var (contentId, sortOrder) in newOrder) {
      var content = contentItems.First(c => c.Id == contentId);
      content.SortOrder = sortOrder;
      content.UpdatedAt = DateTime.UtcNow;
    }

    await context.SaveChangesAsync();

    return true;
  }

  public async Task<IEnumerable<ProgramContent>> GetRequiredContentAsync(Guid programId) {
    return await context.ProgramContents.Where(pc => pc.ProgramId == programId && pc.IsRequired && !pc.IsDeleted)
                        .OrderBy(pc => pc.SortOrder)
                        .ToListAsync();
  }

  public async Task<IEnumerable<ProgramContent>> GetContentByTypeAsync(Guid programId, ProgramContentType type) {
    return await context.ProgramContents.Where(pc => pc.ProgramId == programId && pc.Type == type && !pc.IsDeleted)
                        .OrderBy(pc => pc.SortOrder)
                        .ToListAsync();
  }

  public async Task<IEnumerable<ProgramContent>> GetContentByVisibilityAsync(Guid programId, Visibility visibility) {
    return await context.ProgramContents
                        .Where(pc => pc.ProgramId == programId && pc.Visibility == visibility && !pc.IsDeleted)
                        .OrderBy(pc => pc.SortOrder)
                        .ToListAsync();
  }

  public async Task<bool> MoveContentAsync(Guid contentId, Guid? newParentId, int newSortOrder) {
    var content = await context.ProgramContents.FirstOrDefaultAsync(pc => pc.Id == contentId && !pc.IsDeleted);

    if (content == null) return false;

    // Update parent and sort order
    content.ParentId = newParentId;
    content.SortOrder = newSortOrder;
    content.UpdatedAt = DateTime.UtcNow;

    await context.SaveChangesAsync();

    return true;
  }

  public async Task<int> GetContentCountAsync(Guid programId) { return await context.ProgramContents.CountAsync(pc => pc.ProgramId == programId && !pc.IsDeleted); }

  public async Task<int> GetRequiredContentCountAsync(Guid programId) { return await context.ProgramContents.CountAsync(pc => pc.ProgramId == programId && pc.IsRequired && !pc.IsDeleted); }

  public async Task<IEnumerable<ProgramContent>> SearchContentAsync(Guid programId, string searchTerm) {
    return await context.ProgramContents
                        .Where(pc => pc.ProgramId == programId &&
                                     !pc.IsDeleted &&
                                     (pc.Title.Contains(searchTerm) || pc.Description.Contains(searchTerm))
                        )
                        .OrderBy(pc => pc.SortOrder)
                        .ToListAsync();
  }
}
