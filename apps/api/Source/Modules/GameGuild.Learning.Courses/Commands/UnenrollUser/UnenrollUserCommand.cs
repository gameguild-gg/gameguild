using GameGuild.CQRS;

namespace GameGuild.Programs;

/// <summary> Command to unenroll a user from a program </summary>
public record UnenrollUserCommand(Guid ProgramId, string UserId) : ICommand<bool>;
