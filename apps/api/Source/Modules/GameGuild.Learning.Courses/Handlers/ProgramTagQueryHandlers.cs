using GameGuild.Abstractions;
using GameGuild.CQRS;
using GameGuild.Tags;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Courses;

public class GetProgramTagsQueryHandler(
    IApplicationDbContext context)
    : IQueryHandler<GetProgramTagsQuery, IEnumerable<ProgramTagDto>>
{
    public async Task<IEnumerable<ProgramTagDto>> Handle(GetProgramTagsQuery request, CancellationToken cancellationToken)
    {
        var programTags = await context.Set<ProgramTag>()
            .Include(pt => pt.Tag)
            .Where(pt => pt.ProgramId == request.ProgramId)
            .OrderBy(pt => pt.DisplayOrder)
            .ThenByDescending(pt => pt.IsPrimary)
            .ToListAsync(cancellationToken);

        return programTags.Select(pt => pt.ToDto());
    }
}

public class GetProgramsByTagQueryHandler(
    IApplicationDbContext context)
    : IQueryHandler<GetProgramsByTagQuery, PagedResult<Program>>
{
    public async Task<PagedResult<Program>> Handle(GetProgramsByTagQuery request, CancellationToken cancellationToken)
    {
        var query = context.Set<Program>()
            .Where(p => p.DeletedAt == null)
            .Where(p => context.Set<ProgramTag>()
                .Any(pt => pt.ProgramId == p.Id && pt.TagId == request.TagId));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken);

        return new PagedResult<Program>(items, totalCount, request.Skip, request.Take);
    }
}

public class GetProgramsBySkillQueryHandler(
    IApplicationDbContext context)
    : IQueryHandler<GetProgramsBySkillQuery, PagedResult<ProgramWithSkillDto>>
{
    public async Task<PagedResult<ProgramWithSkillDto>> Handle(GetProgramsBySkillQuery request, CancellationToken cancellationToken)
    {
        var query = from pt in context.Set<ProgramTag>().Include(pt => pt.Tag)
                    join p in context.Set<Program>() on pt.ProgramId equals p.Id
                    where pt.TagId == request.SkillTagId
                          && pt.ProficiencyLevel >= request.MinProficiency
                          && p.DeletedAt == null
                    select new { Program = p, ProgramTag = pt };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.ProgramTag.ProficiencyLevel)
            .ThenByDescending(x => x.Program.CreatedAt)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProgramWithSkillDto>(
            items.Select(x => new ProgramWithSkillDto(x.Program, x.ProgramTag.ToDto())),
            totalCount,
            request.Skip,
            request.Take);
    }
}

public class GetProgramsBySkillsQueryHandler(
    IApplicationDbContext context)
    : IQueryHandler<GetProgramsBySkillsQuery, PagedResult<Program>>
{
    public async Task<PagedResult<Program>> Handle(GetProgramsBySkillsQuery request, CancellationToken cancellationToken)
    {
        var skillTagIds = request.SkillTagIds.ToList();

        IQueryable<Program> query;

        if (request.RequireAll)
        {
            // Programs that have ALL the requested skills
            query = context.Set<Program>()
                .Where(p => p.DeletedAt == null)
                .Where(p => skillTagIds.All(tagId =>
                    context.Set<ProgramTag>().Any(pt => pt.ProgramId == p.Id && pt.TagId == tagId)));
        }
        else
        {
            // Programs that have ANY of the requested skills
            query = context.Set<Program>()
                .Where(p => p.DeletedAt == null)
                .Where(p => skillTagIds.Any(tagId =>
                    context.Set<ProgramTag>().Any(pt => pt.ProgramId == p.Id && pt.TagId == tagId)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken);

        return new PagedResult<Program>(items, totalCount, request.Skip, request.Take);
    }
}

public class GetProgramPrimarySkillQueryHandler(
    IApplicationDbContext context)
    : IQueryHandler<GetProgramPrimarySkillQuery, ProgramTagDto?>
{
    public async Task<ProgramTagDto?> Handle(GetProgramPrimarySkillQuery request, CancellationToken cancellationToken)
    {
        var primaryTag = await context.Set<ProgramTag>()
            .Include(pt => pt.Tag)
            .Where(pt => pt.ProgramId == request.ProgramId && pt.IsPrimary)
            .FirstOrDefaultAsync(cancellationToken);

        return primaryTag?.ToDto();
    }
}

public class SearchProgramsByTagNameQueryHandler(
    IApplicationDbContext context)
    : IQueryHandler<SearchProgramsByTagNameQuery, PagedResult<Program>>
{
    public async Task<PagedResult<Program>> Handle(SearchProgramsByTagNameQuery request, CancellationToken cancellationToken)
    {
        var normalizedSearch = request.TagName.ToLowerInvariant();

        var matchingTagIds = await context.Set<Tag>()
            .Where(t => t.IsActive && t.Name.ToLower().Contains(normalizedSearch))
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        var query = context.Set<Program>()
            .Where(p => p.DeletedAt == null)
            .Where(p => context.Set<ProgramTag>()
                .Any(pt => pt.ProgramId == p.Id && matchingTagIds.Contains(pt.TagId)));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken);

        return new PagedResult<Program>(items, totalCount, request.Skip, request.Take);
    }
}
