using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;
using GameGuild.SharedKernel.Enums;

namespace GameGuild.Modules.Programs.Queries;

/// <summary> Query to get published program by slug (public access) </summary>
public record GetPublishedProgramBySlugQuery(string Slug, bool IncludeContent = false) : IQuery<Program?>;
