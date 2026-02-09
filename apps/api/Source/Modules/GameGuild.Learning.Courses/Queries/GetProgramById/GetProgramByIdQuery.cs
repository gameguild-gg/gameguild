using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Query to get a program by ID </summary>
public sealed record GetProgramByIdQuery(Guid Id, bool IncludeContent = false, bool IncludeEnrollments = false, bool IncludeRatings = false) : IQuery<Program?>;
