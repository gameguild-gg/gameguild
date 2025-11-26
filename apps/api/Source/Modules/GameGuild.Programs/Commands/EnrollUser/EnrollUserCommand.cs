using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;

namespace GameGuild.Modules.Programs.Commands;

/// <summary> Command to enroll a user in a program </summary>
public record EnrollUserCommand(Guid ProgramId, string UserId, DateTime? EnrollmentDate = null) : ICommand<ProgramUser>;
