using GameGuild.Common;


namespace GameGuild.Modules.Programs.Interfaces;

/// <summary>
/// Interface for program content management services
/// </summary>
public interface IProgramContentService {
  Task<Models.ProgramContent> CreateContentAsync(Models.ProgramContent content);

  Task<Models.ProgramContent?> GetContentByIdAsync(Guid id);

  Task<IEnumerable<Models.ProgramContent>> GetContentByProgramAsync(Guid programId);

  Task<IEnumerable<Models.ProgramContent>> GetContentByParentAsync(Guid parentId);

  Task<IEnumerable<Models.ProgramContent>> GetTopLevelContentAsync(Guid programId);

  Task<Models.ProgramContent> UpdateContentAsync(Models.ProgramContent content);

  Task<bool> DeleteContentAsync(Guid id);

  Task<bool> ReorderContentAsync(Guid programId, List<(Guid contentId, int sortOrder)> newOrder);

  Task<IEnumerable<Models.ProgramContent>> GetRequiredContentAsync(Guid programId);

  Task<IEnumerable<Models.ProgramContent>> GetContentByTypeAsync(
    Guid programId,
    ProgramContentType type
  );

  Task<IEnumerable<Models.ProgramContent>> GetContentByVisibilityAsync(
    Guid programId,
    Visibility visibility
  );

  Task<bool> MoveContentAsync(Guid contentId, Guid? newParentId, int newSortOrder);

  Task<int> GetContentCountAsync(Guid programId);

  Task<int> GetRequiredContentCountAsync(Guid programId);

  Task<IEnumerable<Models.ProgramContent>> SearchContentAsync(Guid programId, string searchTerm);
}
