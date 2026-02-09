using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Command to publish a program </summary>
public sealed record PublishProgramCommand(Guid Id) : ICommand<Program>;
