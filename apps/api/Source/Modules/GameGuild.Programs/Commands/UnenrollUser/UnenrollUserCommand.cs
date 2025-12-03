using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;

namespace GameGuild.Modules.Programs.Commands;

/// <summary> Command to unenroll a user from a program </summary>
public record UnenrollUserCommand(Guid ProgramId, string UserId) : ICommand<bool>;
