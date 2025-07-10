namespace GameGuild.Modules.Programs.Interfaces;

/// <summary>
/// Interface for program user enrollment and progress services
/// </summary>
public interface IProgramUserService {
  Task<Models.ProgramUser> EnrollUserAsync(int userProductId, int programId);

  Task<Models.ProgramUser?> GetProgramUserAsync(int id);

  Task<Models.ProgramUser?> GetProgramUserAsync(int userId, int programId);

  Task<IEnumerable<Models.ProgramUser>> GetUserProgramsAsync(int userId);

  Task<Models.ProgramUser> UpdateProgressAsync(int programUserId, decimal completionPercentage);

  Task<Models.ProgramUser> UpdateGradeAsync(int programUserId, decimal finalGrade);

  Task<bool> CompleteeProgramAsync(int programUserId);

  Task<IEnumerable<Models.ProgramUser>> GetProgramEnrollmentsAsync(int programId);

  Task<bool> UnenrollUserAsync(int programUserId);
}
