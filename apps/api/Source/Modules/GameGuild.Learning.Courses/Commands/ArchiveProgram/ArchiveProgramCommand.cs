using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Command to archive a program </summary>
public record ArchiveProgramCommand(Guid Id) : ICommand<Program>;
