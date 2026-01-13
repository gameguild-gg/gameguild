using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Command to enroll a user in a program </summary>
public record EnrollUserCommand(Guid ProgramId, string UserId, DateTime? EnrollmentDate = null) : ICommand<ProgramUser>;
