using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;

namespace GameGuild.Modules.Programs.Commands;

/// <summary> Command to update enrollment status </summary>
public record UpdateEnrollmentStatusCommand(Guid ProgramId, EnrollmentStatus Status, int? MaxEnrollments = null, DateTime? EnrollmentDeadline = null) : ICommand<Program>;
