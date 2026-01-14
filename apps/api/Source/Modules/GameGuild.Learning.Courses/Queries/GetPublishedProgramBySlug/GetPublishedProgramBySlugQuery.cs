using GameGuild.CQRS;

namespace GameGuild.Learning.Courses;

/// <summary> Query to get published program by slug (public access) </summary>
public record GetPublishedProgramBySlugQuery(string Slug, bool IncludeContent = false) : IQuery<Program?>;
