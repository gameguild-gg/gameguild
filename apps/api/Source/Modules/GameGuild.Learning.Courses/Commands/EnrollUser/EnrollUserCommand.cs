using GameGuild.CQRS;

namespace GameGuild.Programs;

/// <summary> Command to enroll a user in a program </summary>
public record EnrollUserCommand(Guid ProgramId, string UserId, DateTime? EnrollmentDate = null) : ICommand<ProgramUser>;
