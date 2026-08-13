
using GameGuild.CQRS;
using GameGuild.Resources;


namespace GameGuild.Learning.Courses;

// Commands for Program management using CQRS pattern
// All commands implement ICommand pattern for GameGuild.CQRS handling

// ===== CRUD COMMANDS =====

/// <summary> Command to create a new program </summary>
[RequiresQuota(ResourceUsageType.Programs, Source = "CreateProgram")]
public sealed record CreateProgramCommand(
  string Title,
  string Description,
  string? Summary = null,
  string? Thumbnail = null,
  string? VideoShowcaseUrl = null,
  float? EstimatedHours = null,
  ProgramCategory Category = ProgramCategory.Other,
  ProgramDifficulty Difficulty = ProgramDifficulty.Beginner,
  EnrollmentStatus EnrollmentStatus = EnrollmentStatus.Open,
  int? MaxEnrollments = null,
  DateTime? EnrollmentDeadline = null,
  decimal PassingScore = 60m,
  string? CreatorId = null
) : ICommand<Program>;
