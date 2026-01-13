using GameGuild.CQRS;

namespace GameGuild.Programs;

/// <summary> Command to archive a program </summary>
public record ArchiveProgramCommand(Guid Id) : ICommand<Program>;
