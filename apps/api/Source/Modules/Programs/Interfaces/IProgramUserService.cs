namespace GameGuild.Modules.Programs;

/// <summary>
/// Interface for program user enrollment and progress services
/// </summary>
public interface IProgramUserService {
  Task<ProgramUser> EnrollUserAsync(int userProductId, int programId);

  Task<ProgramUser?> GetProgramUserAsync(int id);

  Task<ProgramUser?> GetProgramUserAsync(int userId, int programId);

  Task<IEnumerable<ProgramUser>> GetUserProgramsAsync(int userId);

  Task<ProgramUser> UpdateProgressAsync(int programUserId, decimal completionPercentage);

  Task<ProgramUser> UpdateGradeAsync(int programUserId, decimal finalGrade);

  Task<bool> CompleteeProgramAsync(int programUserId);

  Task<IEnumerable<ProgramUser>> GetProgramEnrollmentsAsync(int programId);

  Task<bool> UnenrollUserAsync(int programUserId);
}
