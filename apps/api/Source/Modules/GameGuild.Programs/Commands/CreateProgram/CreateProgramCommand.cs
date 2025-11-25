using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;

namespace GameGuild.Modules.Programs.Commands;

/// <summary>
/// Commands for Program management using CQRS pattern
/// All commands implement ICommand pattern for GameGuild.CQRS handling
/// </summary>

// ===== CRUD COMMANDS =====

/// <summary> Command to create a new program </summary>
public record CreateProgramCommand(
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
  string? CreatorId = null
) : ICommand<Program>;
