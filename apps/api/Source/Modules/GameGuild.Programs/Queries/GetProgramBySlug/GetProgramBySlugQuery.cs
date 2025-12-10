using GameGuild.CQRS;
using GameGuild.Modules.Programs.Entities;
using GameGuild.SharedKernel.Enums;

namespace GameGuild.Modules.Programs.Queries;

/// <summary> Query to get a program by slug </summary>
public record GetProgramBySlugQuery(string Slug, bool IncludeContent = false, bool IncludeEnrollments = false, bool IncludeRatings = false) : IQuery<Program?>;
