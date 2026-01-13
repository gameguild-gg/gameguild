using GameGuild.CQRS;

namespace GameGuild.Programs;

/// <summary> Command to publish a program </summary>
public record PublishProgramCommand(Guid Id) : ICommand<Program>;
