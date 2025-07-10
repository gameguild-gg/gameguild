using GameGuild.Common;


namespace GameGuild.Modules.Programs.Interfaces;

/// <summary>
/// Interface for program management services
/// </summary>
public interface IProgramService {
  Task<Models.Program> CreateProgramAsync(Models.Program program);

  Task<Models.Program?> GetProgramByIdAsync(int id);

  Task<IEnumerable<Models.Program>> GetProgramsByVisibilityAsync(Visibility visibility);

  Task<Models.Program> UpdateProgramAsync(Models.Program program);

  Task<bool> DeleteProgramAsync(int id);

  Task<IEnumerable<Models.Program>> GetProgramsByProductAsync(int productId);

  Task<bool> AddProgramToProductAsync(int productId, int programId, int sortOrder = 0);

  Task<bool> RemoveProgramFromProductAsync(int productId, int programId);
}
