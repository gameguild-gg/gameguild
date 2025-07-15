using GameGuild.Common;


namespace GameGuild.Modules.Programs;

/// <summary>
/// Interface for program content management services
/// </summary>
public interface IProgramContentService {
  Task<ProgramContent> CreateContentAsync(ProgramContent content);

  Task<ProgramContent?> GetContentByIdAsync(Guid id);

  Task<IEnumerable<ProgramContent>> GetContentByProgramAsync(Guid programId);

  Task<IEnumerable<ProgramContent>> GetContentByParentAsync(Guid parentId);

  Task<IEnumerable<ProgramContent>> GetTopLevelContentAsync(Guid programId);

  Task<ProgramContent> UpdateContentAsync(ProgramContent content);

  Task<bool> DeleteContentAsync(Guid id);

  Task<bool> ReorderContentAsync(Guid programId, List<(Guid contentId, int sortOrder)> newOrder);

  Task<IEnumerable<ProgramContent>> GetRequiredContentAsync(Guid programId);

  Task<IEnumerable<ProgramContent>> GetContentByTypeAsync(
    Guid programId,
    Common.ProgramContentType type
  );

  Task<IEnumerable<ProgramContent>> GetContentByVisibilityAsync(
    Guid programId,
    Visibility visibility
  );

  Task<bool> MoveContentAsync(Guid contentId, Guid? newParentId, int newSortOrder);

  Task<int> GetContentCountAsync(Guid programId);

  Task<int> GetRequiredContentCountAsync(Guid programId);

  Task<IEnumerable<ProgramContent>> SearchContentAsync(Guid programId, string searchTerm);
}
