using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Command to archive a program </summary>
public sealed record ArchiveProgramCommand(Guid Id) : ICommand<Program>;
