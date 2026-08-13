
using GameGuild.CQRS;


namespace GameGuild.Learning.Courses;

/// <summary> Command to update an existing program </summary>
public sealed record UpdateProgramCommand(
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
  DateTime? EnrollmentDeadline = null,
  decimal? PassingScore = null
) : ICommand<Program>;
