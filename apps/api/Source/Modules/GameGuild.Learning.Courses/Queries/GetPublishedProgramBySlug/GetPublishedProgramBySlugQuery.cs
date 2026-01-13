using GameGuild.CQRS;

using GameGuild.Enums;

namespace GameGuild.Programs;

/// <summary> Query to get published program by slug (public access) </summary>
public record GetPublishedProgramBySlugQuery(string Slug, bool IncludeContent = false) : IQuery<Program?>;
