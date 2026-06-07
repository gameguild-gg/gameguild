using GameGuild.CQRS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.Tags;

public sealed record TagDto(Guid Id, string Name, string? Description, TagType Type, string? Color, string? Icon, bool IsActive, Guid? TenantId);

public sealed record TagRelationshipDto(Guid Id, Guid SourceId, Guid TargetId, TagRelationshipType Type, decimal? Weight, string? Metadata);

public sealed record TagProficiencyDto(Guid Id, string Name, string? Description, TagType Type, SkillProficiencyLevel ProficiencyLevel, string? Color, string? Icon, bool IsActive);

public sealed record CreateTagRequest(string Name, TagType Type, string? Description = null, string? Color = null, string? Icon = null, Guid? TenantId = null);

public sealed record UpdateTagRequest(string? Name = null, string? Description = null, string? Color = null, string? Icon = null, bool? IsActive = null);

public sealed record CreateTagRelationshipRequest(Guid SourceId, Guid TargetId, TagRelationshipType Type, decimal? Weight = null, string? Metadata = null);

public sealed record CreateTagProficiencyRequest(string Name, TagType Type, SkillProficiencyLevel ProficiencyLevel, string? Description = null, string? Color = null, string? Icon = null);

public sealed record CreateTagCommand(CreateTagRequest Request) : ICommand<TagDto>;

public sealed record UpdateTagCommand(Guid Id, UpdateTagRequest Request) : ICommand<TagDto?>;

public sealed record GetTagQuery(Guid Id) : IQuery<TagDto?>;

public sealed record SearchTagsQuery(string? Search = null, TagType? Type = null, Guid? TenantId = null, bool IncludeInactive = false, int Skip = 0, int Take = 50) : IQuery<IReadOnlyList<TagDto>>;

public sealed record CreateTagRelationshipCommand(CreateTagRelationshipRequest Request) : ICommand<TagRelationshipDto>;

public sealed record GetTagRelationshipsQuery(Guid TagId) : IQuery<IReadOnlyList<TagRelationshipDto>>;

public sealed record CreateTagProficiencyCommand(CreateTagProficiencyRequest Request) : ICommand<TagProficiencyDto>;

public sealed record SearchTagProficienciesQuery(TagType? Type = null, SkillProficiencyLevel? Level = null, bool IncludeInactive = false) : IQuery<IReadOnlyList<TagProficiencyDto>>;

public interface ITagsRepository
{
    Task<Tag?> GetTagAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Tag>> SearchTagsAsync(string? search, TagType? type, Guid? tenantId, bool includeInactive, int skip, int take, CancellationToken cancellationToken = default);

    Task AddTagAsync(Tag tag, CancellationToken cancellationToken = default);

    Task UpdateTagAsync(Tag tag, CancellationToken cancellationToken = default);

    Task AddRelationshipAsync(TagRelationship relationship, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TagRelationship>> GetRelationshipsAsync(Guid tagId, CancellationToken cancellationToken = default);

    Task AddProficiencyAsync(TagProficiency proficiency, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TagProficiency>> SearchProficienciesAsync(TagType? type, SkillProficiencyLevel? level, bool includeInactive, CancellationToken cancellationToken = default);
}

public sealed class TagsRepository(IApplicationDbContext context) : ITagsRepository
{
    public Task<Tag?> GetTagAsync(Guid id, CancellationToken cancellationToken = default)
        => context.Set<Tag>().FirstOrDefaultAsync(tag => tag.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Tag>> SearchTagsAsync(string? search, TagType? type, Guid? tenantId, bool includeInactive, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = context.Set<Tag>().AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(tag => tag.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(tag => tag.Name.Contains(search) || (tag.Description != null && tag.Description.Contains(search)));
        }

        if (type.HasValue)
        {
            query = query.Where(tag => tag.Type == type.Value);
        }

        if (tenantId.HasValue)
        {
            query = query.Where(tag => tag.TenantId == tenantId.Value);
        }

        return await query.OrderBy(tag => tag.Name)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddTagAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        context.Set<Tag>().Add(tag);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateTagAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        context.Set<Tag>().Update(tag);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddRelationshipAsync(TagRelationship relationship, CancellationToken cancellationToken = default)
    {
        context.Set<TagRelationship>().Add(relationship);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TagRelationship>> GetRelationshipsAsync(Guid tagId, CancellationToken cancellationToken = default)
        => await context.Set<TagRelationship>()
            .Where(relationship => relationship.SourceId == tagId || relationship.TargetId == tagId)
            .OrderBy(relationship => relationship.Type)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddProficiencyAsync(TagProficiency proficiency, CancellationToken cancellationToken = default)
    {
        context.Set<TagProficiency>().Add(proficiency);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TagProficiency>> SearchProficienciesAsync(TagType? type, SkillProficiencyLevel? level, bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = context.Set<TagProficiency>().AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(proficiency => proficiency.IsActive);
        }

        if (type.HasValue)
        {
            query = query.Where(proficiency => proficiency.Type == type.Value);
        }

        if (level.HasValue)
        {
            query = query.Where(proficiency => proficiency.ProficiencyLevel == level.Value);
        }

        return await query.OrderBy(proficiency => proficiency.Type).ThenBy(proficiency => proficiency.ProficiencyLevel)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}

public interface ITagsService
{
    Task<TagDto> CreateTagAsync(CreateTagRequest request, CancellationToken cancellationToken = default);

    Task<TagDto?> UpdateTagAsync(Guid id, UpdateTagRequest request, CancellationToken cancellationToken = default);

    Task<TagDto?> GetTagAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TagDto>> SearchTagsAsync(SearchTagsQuery query, CancellationToken cancellationToken = default);

    Task<TagRelationshipDto> CreateRelationshipAsync(CreateTagRelationshipRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TagRelationshipDto>> GetRelationshipsAsync(Guid tagId, CancellationToken cancellationToken = default);

    Task<TagProficiencyDto> CreateProficiencyAsync(CreateTagProficiencyRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TagProficiencyDto>> SearchProficienciesAsync(SearchTagProficienciesQuery query, CancellationToken cancellationToken = default);
}

public sealed class TagsService(ITagsRepository repository) : ITagsService
{
    public async Task<TagDto> CreateTagAsync(CreateTagRequest request, CancellationToken cancellationToken = default)
    {
        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description,
            Type = request.Type,
            Color = request.Color,
            Icon = request.Icon,
            TenantId = request.TenantId,
            IsActive = true
        };

        await repository.AddTagAsync(tag, cancellationToken).ConfigureAwait(false);
        return ToDto(tag);
    }

    public async Task<TagDto?> UpdateTagAsync(Guid id, UpdateTagRequest request, CancellationToken cancellationToken = default)
    {
        var tag = await repository.GetTagAsync(id, cancellationToken).ConfigureAwait(false);
        if (tag is null)
        {
            return null;
        }

        tag.Name = request.Name?.Trim() ?? tag.Name;
        tag.Description = request.Description ?? tag.Description;
        tag.Color = request.Color ?? tag.Color;
        tag.Icon = request.Icon ?? tag.Icon;
        tag.IsActive = request.IsActive ?? tag.IsActive;
        tag.UpdatedAt = SystemClock.UtcNow;

        await repository.UpdateTagAsync(tag, cancellationToken).ConfigureAwait(false);
        return ToDto(tag);
    }

    public async Task<TagDto?> GetTagAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tag = await repository.GetTagAsync(id, cancellationToken).ConfigureAwait(false);
        return tag is null ? null : ToDto(tag);
    }

    public async Task<IReadOnlyList<TagDto>> SearchTagsAsync(SearchTagsQuery query, CancellationToken cancellationToken = default)
        => (await repository.SearchTagsAsync(query.Search, query.Type, query.TenantId, query.IncludeInactive, query.Skip, query.Take, cancellationToken)
                .ConfigureAwait(false))
            .Select(ToDto)
            .ToList();

    public async Task<TagRelationshipDto> CreateRelationshipAsync(CreateTagRelationshipRequest request, CancellationToken cancellationToken = default)
    {
        if (request.SourceId == request.TargetId)
        {
            throw new InvalidOperationException("A tag cannot relate to itself.");
        }

        var relationship = new TagRelationship
        {
            Id = Guid.NewGuid(),
            SourceId = request.SourceId,
            TargetId = request.TargetId,
            Type = request.Type,
            Weight = request.Weight,
            Metadata = request.Metadata
        };

        await repository.AddRelationshipAsync(relationship, cancellationToken).ConfigureAwait(false);
        return ToDto(relationship);
    }

    public async Task<IReadOnlyList<TagRelationshipDto>> GetRelationshipsAsync(Guid tagId, CancellationToken cancellationToken = default)
        => (await repository.GetRelationshipsAsync(tagId, cancellationToken).ConfigureAwait(false)).Select(ToDto).ToList();

    public async Task<TagProficiencyDto> CreateProficiencyAsync(CreateTagProficiencyRequest request, CancellationToken cancellationToken = default)
    {
        var proficiency = new TagProficiency
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description,
            Type = request.Type,
            ProficiencyLevel = request.ProficiencyLevel,
            Color = request.Color,
            Icon = request.Icon,
            IsActive = true
        };

        await repository.AddProficiencyAsync(proficiency, cancellationToken).ConfigureAwait(false);
        return ToDto(proficiency);
    }

    public async Task<IReadOnlyList<TagProficiencyDto>> SearchProficienciesAsync(SearchTagProficienciesQuery query, CancellationToken cancellationToken = default)
        => (await repository.SearchProficienciesAsync(query.Type, query.Level, query.IncludeInactive, cancellationToken)
                .ConfigureAwait(false))
            .Select(ToDto)
            .ToList();

    private static TagDto ToDto(Tag tag)
        => new(tag.Id, tag.Name, tag.Description, tag.Type, tag.Color, tag.Icon, tag.IsActive, tag.TenantId);

    private static TagRelationshipDto ToDto(TagRelationship relationship)
        => new(relationship.Id, relationship.SourceId, relationship.TargetId, relationship.Type, relationship.Weight, relationship.Metadata);

    private static TagProficiencyDto ToDto(TagProficiency proficiency)
        => new(proficiency.Id, proficiency.Name, proficiency.Description, proficiency.Type, proficiency.ProficiencyLevel, proficiency.Color, proficiency.Icon, proficiency.IsActive);
}

public sealed class CreateTagCommandHandler(ITagsService service) : ICommandHandler<CreateTagCommand, TagDto>
{
    public Task<TagDto> Handle(CreateTagCommand request, CancellationToken cancellationToken) => service.CreateTagAsync(request.Request, cancellationToken);
}

public sealed class UpdateTagCommandHandler(ITagsService service) : ICommandHandler<UpdateTagCommand, TagDto?>
{
    public Task<TagDto?> Handle(UpdateTagCommand request, CancellationToken cancellationToken) => service.UpdateTagAsync(request.Id, request.Request, cancellationToken);
}

public sealed class GetTagQueryHandler(ITagsService service) : IQueryHandler<GetTagQuery, TagDto?>
{
    public Task<TagDto?> Handle(GetTagQuery request, CancellationToken cancellationToken) => service.GetTagAsync(request.Id, cancellationToken);
}

public sealed class SearchTagsQueryHandler(ITagsService service) : IQueryHandler<SearchTagsQuery, IReadOnlyList<TagDto>>
{
    public Task<IReadOnlyList<TagDto>> Handle(SearchTagsQuery request, CancellationToken cancellationToken) => service.SearchTagsAsync(request, cancellationToken);
}

public sealed class CreateTagRelationshipCommandHandler(ITagsService service) : ICommandHandler<CreateTagRelationshipCommand, TagRelationshipDto>
{
    public Task<TagRelationshipDto> Handle(CreateTagRelationshipCommand request, CancellationToken cancellationToken) => service.CreateRelationshipAsync(request.Request, cancellationToken);
}

public sealed class GetTagRelationshipsQueryHandler(ITagsService service) : IQueryHandler<GetTagRelationshipsQuery, IReadOnlyList<TagRelationshipDto>>
{
    public Task<IReadOnlyList<TagRelationshipDto>> Handle(GetTagRelationshipsQuery request, CancellationToken cancellationToken) => service.GetRelationshipsAsync(request.TagId, cancellationToken);
}

public sealed class CreateTagProficiencyCommandHandler(ITagsService service) : ICommandHandler<CreateTagProficiencyCommand, TagProficiencyDto>
{
    public Task<TagProficiencyDto> Handle(CreateTagProficiencyCommand request, CancellationToken cancellationToken) => service.CreateProficiencyAsync(request.Request, cancellationToken);
}

public sealed class SearchTagProficienciesQueryHandler(ITagsService service) : IQueryHandler<SearchTagProficienciesQuery, IReadOnlyList<TagProficiencyDto>>
{
    public Task<IReadOnlyList<TagProficiencyDto>> Handle(SearchTagProficienciesQuery request, CancellationToken cancellationToken) => service.SearchProficienciesAsync(request, cancellationToken);
}

[ApiController]
[Route("api/tags")]
public sealed class TagsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<TagDto>> Search(
        [FromQuery] string? search,
        [FromQuery] TagType? type,
        [FromQuery] Guid? tenantId,
        [FromQuery] bool includeInactive,
        [FromQuery] int skip,
        [FromQuery] int take,
        CancellationToken cancellationToken)
        => sender.Send(new SearchTagsQuery(search, type, tenantId, includeInactive, skip, take <= 0 ? 50 : take), cancellationToken);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var tag = await sender.Send(new GetTagQuery(id), cancellationToken).ConfigureAwait(false);
        return tag is null ? NotFound() : Ok(tag);
    }

    [HttpPost]
    public Task<TagDto> Create(CreateTagRequest request, CancellationToken cancellationToken)
        => sender.Send(new CreateTagCommand(request), cancellationToken);

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateTagRequest request, CancellationToken cancellationToken)
    {
        var tag = await sender.Send(new UpdateTagCommand(id, request), cancellationToken).ConfigureAwait(false);
        return tag is null ? NotFound() : Ok(tag);
    }

    [HttpGet("{id:guid}/relationships")]
    public Task<IReadOnlyList<TagRelationshipDto>> GetRelationships(Guid id, CancellationToken cancellationToken)
        => sender.Send(new GetTagRelationshipsQuery(id), cancellationToken);

    [HttpPost("relationships")]
    public Task<TagRelationshipDto> CreateRelationship(CreateTagRelationshipRequest request, CancellationToken cancellationToken)
        => sender.Send(new CreateTagRelationshipCommand(request), cancellationToken);

    [HttpGet("proficiencies")]
    public Task<IReadOnlyList<TagProficiencyDto>> SearchProficiencies(
        [FromQuery] TagType? type,
        [FromQuery] SkillProficiencyLevel? level,
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
        => sender.Send(new SearchTagProficienciesQuery(type, level, includeInactive), cancellationToken);

    [HttpPost("proficiencies")]
    public Task<TagProficiencyDto> CreateProficiency(CreateTagProficiencyRequest request, CancellationToken cancellationToken)
        => sender.Send(new CreateTagProficiencyCommand(request), cancellationToken);
}

public sealed class TagsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TagsModelConfiguration).Assembly);
    }
}

public static class TagsDependencyInjection
{
    public static IServiceCollection AddTagsModule(this IServiceCollection services)
    {
        services.AddScoped<ITagsRepository, TagsRepository>();
        services.AddScoped<ITagsService, TagsService>();
        services.AddScoped<ICommandHandler<CreateTagCommand, TagDto>, CreateTagCommandHandler>();
        services.AddScoped<IRequestHandler<CreateTagCommand, TagDto>>(sp => sp.GetRequiredService<ICommandHandler<CreateTagCommand, TagDto>>());
        services.AddScoped<ICommandHandler<UpdateTagCommand, TagDto?>, UpdateTagCommandHandler>();
        services.AddScoped<IRequestHandler<UpdateTagCommand, TagDto?>>(sp => sp.GetRequiredService<ICommandHandler<UpdateTagCommand, TagDto?>>());
        services.AddScoped<IQueryHandler<GetTagQuery, TagDto?>, GetTagQueryHandler>();
        services.AddScoped<IRequestHandler<GetTagQuery, TagDto?>>(sp => sp.GetRequiredService<IQueryHandler<GetTagQuery, TagDto?>>());
        services.AddScoped<IQueryHandler<SearchTagsQuery, IReadOnlyList<TagDto>>, SearchTagsQueryHandler>();
        services.AddScoped<IRequestHandler<SearchTagsQuery, IReadOnlyList<TagDto>>>(sp => sp.GetRequiredService<IQueryHandler<SearchTagsQuery, IReadOnlyList<TagDto>>>());
        services.AddScoped<ICommandHandler<CreateTagRelationshipCommand, TagRelationshipDto>, CreateTagRelationshipCommandHandler>();
        services.AddScoped<IRequestHandler<CreateTagRelationshipCommand, TagRelationshipDto>>(sp => sp.GetRequiredService<ICommandHandler<CreateTagRelationshipCommand, TagRelationshipDto>>());
        services.AddScoped<IQueryHandler<GetTagRelationshipsQuery, IReadOnlyList<TagRelationshipDto>>, GetTagRelationshipsQueryHandler>();
        services.AddScoped<IRequestHandler<GetTagRelationshipsQuery, IReadOnlyList<TagRelationshipDto>>>(sp => sp.GetRequiredService<IQueryHandler<GetTagRelationshipsQuery, IReadOnlyList<TagRelationshipDto>>>());
        services.AddScoped<ICommandHandler<CreateTagProficiencyCommand, TagProficiencyDto>, CreateTagProficiencyCommandHandler>();
        services.AddScoped<IRequestHandler<CreateTagProficiencyCommand, TagProficiencyDto>>(sp => sp.GetRequiredService<ICommandHandler<CreateTagProficiencyCommand, TagProficiencyDto>>());
        services.AddScoped<IQueryHandler<SearchTagProficienciesQuery, IReadOnlyList<TagProficiencyDto>>, SearchTagProficienciesQueryHandler>();
        services.AddScoped<IRequestHandler<SearchTagProficienciesQuery, IReadOnlyList<TagProficiencyDto>>>(sp => sp.GetRequiredService<IQueryHandler<SearchTagProficienciesQuery, IReadOnlyList<TagProficiencyDto>>>());
        return services;
    }
}

public sealed class TagsModule : ModuleBase
{
    public override string Name => "Tags";
    public override int Order => 80;

    public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.AddTagsModule();

    public override IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints;
}
