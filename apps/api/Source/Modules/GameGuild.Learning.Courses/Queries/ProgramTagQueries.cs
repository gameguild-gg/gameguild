using GameGuild.CQRS;
using GameGuild.Tags;

namespace GameGuild.Learning.Courses;

// ===== PROGRAM TAG QUERIES =====

/// <summary>
/// Get all tags for a specific program
/// </summary>
public record GetProgramTagsQuery(Guid ProgramId) : IQuery<IEnumerable<ProgramTagDto>>;

/// <summary>
/// Get all programs tagged with a specific tag
/// </summary>
public record GetProgramsByTagQuery(
    Guid TagId,
    int Skip = 0,
    int Take = 20) : IQuery<PagedResult<Program>>;

/// <summary>
/// Get programs by skill with minimum proficiency level
/// </summary>
public record GetProgramsBySkillQuery(
    Guid SkillTagId,
    SkillProficiencyLevel MinProficiency = SkillProficiencyLevel.Beginner,
    int Skip = 0,
    int Take = 20) : IQuery<PagedResult<ProgramWithSkillDto>>;

/// <summary>
/// Get programs that teach multiple skills
/// </summary>
public record GetProgramsBySkillsQuery(
    IEnumerable<Guid> SkillTagIds,
    bool RequireAll = false,
    int Skip = 0,
    int Take = 20) : IQuery<PagedResult<Program>>;

/// <summary>
/// Get primary skill for a program
/// </summary>
public record GetProgramPrimarySkillQuery(Guid ProgramId) : IQuery<ProgramTagDto?>;

/// <summary>
/// Search programs by tag name
/// </summary>
public record SearchProgramsByTagNameQuery(
    string TagName,
    int Skip = 0,
    int Take = 20) : IQuery<PagedResult<Program>>;

// ===== DTOs =====

public record ProgramWithSkillDto(
    Program Program,
    ProgramTagDto SkillTag);

public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Skip,
    int Take)
{
    public int PageCount => (int)Math.Ceiling((double)TotalCount / Take);
    public bool HasMore => Skip + Take < TotalCount;
}
