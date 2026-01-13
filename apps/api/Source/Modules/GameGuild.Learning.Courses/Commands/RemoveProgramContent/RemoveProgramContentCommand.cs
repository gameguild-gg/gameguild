using GameGuild.CQRS;

namespace GameGuild.Programs;

/// <summary> Command to remove content from a program </summary>
public record RemoveProgramContentCommand(Guid ProgramId, Guid ContentId) : ICommand<bool>;
