using GameGuild.CQRS;

namespace GameGuild.Programs;

/// <summary> Command to restore a program from archive </summary>
public record RestoreProgramCommand(Guid Id) : ICommand<Program>;
