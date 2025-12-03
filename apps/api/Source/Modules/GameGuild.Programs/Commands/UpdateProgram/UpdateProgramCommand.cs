using GameGuild.Modules.Programs.DTOs;
using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;
using GameGuild.Modules.Programs.Models;

namespace GameGuild.Modules.Programs.Commands;

/// <summary> Command to update an existing program </summary>
public record UpdateProgramCommand(
  Guid Id,
  string? Title = null,
  string? Description = null,
  string? Summary = null,
  string? Thumbnail = null,
  string? VideoShowcaseUrl = null,
  float? EstimatedHours = null,
  ProgramCategory? Category = null,
  ProgramDifficulty? Difficulty = null,
  EnrollmentStatus? EnrollmentStatus = null,
  int? MaxEnrollments = null,
  DateTime? EnrollmentDeadline = null
) : ICommand<Program>;
