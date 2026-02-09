using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Command to unpublish a program </summary>
public sealed record UnpublishProgramCommand(Guid Id) : ICommand<Program>;
