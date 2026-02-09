using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Query to get a program by slug </summary>
public sealed record GetProgramBySlugQuery(string Slug, bool IncludeContent = false, bool IncludeEnrollments = false, bool IncludeRatings = false) : IQuery<Program?>;
