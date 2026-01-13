using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Command to publish a program </summary>
public record PublishProgramCommand(Guid Id) : ICommand<Program>;
