using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Programs;

/// <summary>
/// Resolvers for ProgramContent navigation properties
/// </summary>
public class ProgramContentResolvers {
  /// <summary>
  /// Resolves the parent program for the content
  /// </summary>
  public async Task<Program?> GetProgramAsync(
    [Parent] ProgramContent content,
    [Service] ApplicationDbContext context
  ) {
    return await context.Programs.FirstOrDefaultAsync(p => p.Id == content.ProgramId);
  }

  /// <summary>
  /// Resolves the parent content for hierarchical structure
  /// </summary>
  public async Task<ProgramContent?> GetParentContentAsync(
    [Parent] ProgramContent content,
    [Service] ApplicationDbContext context
  ) {
    if (!content.ParentId.HasValue) return null;

    return await context.ProgramContents.FirstOrDefaultAsync(pc => pc.Id == content.ParentId.Value);
  }

  /// <summary>
  /// Resolves child contents for hierarchical structure
  /// </summary>
  public async Task<IEnumerable<ProgramContent>> GetChildContentsAsync(
    [Parent] ProgramContent content,
    [Service] ApplicationDbContext context
  ) {
    return await context.ProgramContents.Where(pc => pc.ParentId == content.Id)
                        .OrderBy(pc => pc.SortOrder)
                        .ToListAsync();
  }
}
