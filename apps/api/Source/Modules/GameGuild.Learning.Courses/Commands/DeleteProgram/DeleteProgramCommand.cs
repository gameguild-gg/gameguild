using GameGuild.CQRS;

namespace GameGuild.Programs;

/// <summary> Command to delete a program (soft delete) </summary>
public record DeleteProgramCommand(Guid Id) : ICommand<bool>;
