using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Command to unenroll a user from a program </summary>
public record UnenrollUserCommand(Guid ProgramId, string UserId) : ICommand<bool>;
