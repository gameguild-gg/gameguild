using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;
using GameGuild.SharedKernel.Enums;

namespace GameGuild.Modules.Programs.Queries;

/// <summary> Query to get all programs with pagination and filtering </summary>
public record GetAllProgramsQuery(
  int Skip = 0,
  int Take = 50,
  string? Search = null,
  ProgramCategory? Category = null,
  ProgramDifficulty? Difficulty = null,
  ContentStatus? Status = null,
  AccessLevel? Visibility = null,
  Entities.EnrollmentStatus? EnrollmentStatus = null,
  string? CreatorId = null,
  bool IncludeArchived = false,
  string? SortBy = "CreatedAt",
  bool SortDescending = true
) : IQuery<IEnumerable<Program>>;
