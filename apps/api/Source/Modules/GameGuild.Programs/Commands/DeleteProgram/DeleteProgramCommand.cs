using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;

namespace GameGuild.Modules.Programs.Commands;

/// <summary> Command to delete a program (soft delete) </summary>
public record DeleteProgramCommand(Guid Id) : ICommand<bool>;
