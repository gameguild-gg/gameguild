using GameGuild.Modules.Programs.DTOs;
using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;
using GameGuild.Modules.Programs.Models;

namespace GameGuild.Modules.Programs.Commands;

/// <summary> Command to update enrollment status </summary>
public record UpdateEnrollmentStatusCommand(Guid ProgramId, EnrollmentStatus Status, int? MaxEnrollments = null, DateTime? EnrollmentDeadline = null) : ICommand<Program>;
