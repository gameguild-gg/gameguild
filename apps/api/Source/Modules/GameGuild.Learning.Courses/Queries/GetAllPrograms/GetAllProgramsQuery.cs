using GameGuild.CQRS;


namespace GameGuild.Learning.Courses;

/// <summary> Query to get all programs with pagination and filtering </summary>
public sealed record GetAllProgramsQuery(
  int Skip = 0,
  int Take = 50,
  string? Search = null,
  ProgramCategory? Category = null,
  ProgramDifficulty? Difficulty = null,
  ContentStatus? Status = null,
  ContentVisibility? Visibility = null,
  EnrollmentStatus? EnrollmentStatus = null,
  string? CreatorId = null,
  bool IncludeArchived = false,
  string? SortBy = "CreatedAt",
  bool SortDescending = true
) : IQuery<IEnumerable<Program>>;
