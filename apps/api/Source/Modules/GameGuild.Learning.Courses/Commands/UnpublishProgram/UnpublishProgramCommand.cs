using GameGuild.CQRS;

namespace GameGuild.Programs;

/// <summary> Command to unpublish a program </summary>
public record UnpublishProgramCommand(Guid Id) : ICommand<Program>;
