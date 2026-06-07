using GameGuild.CQRS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.GameJams;

public sealed record JamDto(
    Guid Id,
    string Name,
    string Slug,
    string? Theme,
    string? Description,
    DateTime StartDate,
    DateTime EndDate,
    DateTime? VotingEndDate,
    int? MaxParticipants,
    int ParticipantCount,
    JamStatus Status,
    Guid CreatedBy);

public sealed record JamSubmissionDto(Guid Id, Guid JamId, Guid ProjectVersionId, Guid UserId, string? SubmissionNotes);

public sealed record JamCriteriaDto(Guid Id, Guid JamId, string Name, string? Description, decimal Weight, int MaxScore);

public sealed record JamScoreDto(Guid Id, Guid SubmissionId, Guid CriteriaId, Guid JudgeUserId, int Score, string? Feedback);

public sealed record CreateJamRequest(
    string Name,
    string Slug,
    DateTime StartDate,
    DateTime EndDate,
    Guid CreatedBy,
    string? Theme = null,
    string? Description = null,
    string? Rules = null,
    string? SubmissionCriteria = null,
    DateTime? VotingEndDate = null,
    int? MaxParticipants = null);

public sealed record CreateJamCommand(CreateJamRequest Request) : ICommand<JamDto>;

public sealed record GetJamQuery(Guid JamId) : IQuery<JamDto?>;

public sealed record GetJamsQuery(JamStatus? Status = null, int Skip = 0, int Take = 50) : IQuery<IReadOnlyList<JamDto>>;

public sealed record SetJamStatusCommand(Guid JamId, JamStatus Status) : ICommand<JamDto?>;

public sealed record SubmitJamEntryCommand(Guid JamId, Guid ProjectVersionId, Guid UserId, string? Notes) : ICommand<JamSubmissionDto>;

public sealed record AddJamCriteriaCommand(Guid JamId, string Name, string? Description, decimal Weight, int MaxScore) : ICommand<JamCriteriaDto>;

public sealed record ScoreJamSubmissionCommand(Guid SubmissionId, Guid CriteriaId, Guid JudgeUserId, int Score, string? Feedback) : ICommand<JamScoreDto>;

public sealed record SubmitJamEntryRequest(Guid ProjectVersionId, Guid UserId, string? Notes = null);

public sealed record AddJamCriteriaRequest(string Name, string? Description = null, decimal Weight = 1m, int MaxScore = 5);

public sealed record ScoreJamSubmissionRequest(Guid CriteriaId, Guid JudgeUserId, int Score, string? Feedback = null);

public interface IGameJamRepository
{
    Task<Jam?> GetJamAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Jam>> ListJamsAsync(JamStatus? status, int skip, int take, CancellationToken cancellationToken = default);

    Task AddJamAsync(Jam jam, CancellationToken cancellationToken = default);

    Task UpdateJamAsync(Jam jam, CancellationToken cancellationToken = default);

    Task<JamSubmission?> GetSubmissionAsync(Guid submissionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JamSubmission>> GetSubmissionsAsync(Guid jamId, CancellationToken cancellationToken = default);

    Task AddSubmissionAsync(JamSubmission submission, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JamJudgingCriteria>> GetCriteriaAsync(Guid jamId, CancellationToken cancellationToken = default);

    Task AddCriteriaAsync(JamJudgingCriteria criteria, CancellationToken cancellationToken = default);

    Task AddScoreAsync(JamScore score, CancellationToken cancellationToken = default);
}

public sealed class GameJamRepository(IApplicationDbContext context) : IGameJamRepository
{
    public Task<Jam?> GetJamAsync(Guid id, CancellationToken cancellationToken = default)
        => context.Set<Jam>().FirstOrDefaultAsync(jam => jam.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Jam>> ListJamsAsync(JamStatus? status, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = context.Set<Jam>().AsQueryable();
        if (status.HasValue)
        {
            query = query.Where(jam => jam.Status == status.Value);
        }

        return await query
            .OrderByDescending(jam => jam.StartDate)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddJamAsync(Jam jam, CancellationToken cancellationToken = default)
    {
        context.Set<Jam>().Add(jam);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateJamAsync(Jam jam, CancellationToken cancellationToken = default)
    {
        context.Set<Jam>().Update(jam);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<JamSubmission?> GetSubmissionAsync(Guid submissionId, CancellationToken cancellationToken = default)
        => context.Set<JamSubmission>().FirstOrDefaultAsync(submission => submission.Id == submissionId, cancellationToken);

    public async Task<IReadOnlyList<JamSubmission>> GetSubmissionsAsync(Guid jamId, CancellationToken cancellationToken = default)
        => await context.Set<JamSubmission>()
            .Where(submission => submission.JamId == jamId)
            .OrderBy(submission => submission.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddSubmissionAsync(JamSubmission submission, CancellationToken cancellationToken = default)
    {
        context.Set<JamSubmission>().Add(submission);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<JamJudgingCriteria>> GetCriteriaAsync(Guid jamId, CancellationToken cancellationToken = default)
        => await context.Set<JamJudgingCriteria>()
            .Where(criteria => criteria.JamId == jamId)
            .OrderBy(criteria => criteria.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddCriteriaAsync(JamJudgingCriteria criteria, CancellationToken cancellationToken = default)
    {
        context.Set<JamJudgingCriteria>().Add(criteria);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddScoreAsync(JamScore score, CancellationToken cancellationToken = default)
    {
        context.Set<JamScore>().Add(score);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

public interface IGameJamService
{
    Task<JamDto> CreateAsync(CreateJamCommand command, CancellationToken cancellationToken = default);

    Task<JamDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JamDto>> ListAsync(GetJamsQuery query, CancellationToken cancellationToken = default);

    Task<JamDto?> SetStatusAsync(Guid id, JamStatus status, CancellationToken cancellationToken = default);

    Task<JamSubmissionDto> SubmitAsync(SubmitJamEntryCommand command, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JamSubmissionDto>> GetSubmissionsAsync(Guid jamId, CancellationToken cancellationToken = default);

    Task<JamCriteriaDto> AddCriteriaAsync(AddJamCriteriaCommand command, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JamCriteriaDto>> GetCriteriaAsync(Guid jamId, CancellationToken cancellationToken = default);

    Task<JamScoreDto> ScoreAsync(ScoreJamSubmissionCommand command, CancellationToken cancellationToken = default);
}

public sealed class GameJamService(IGameJamRepository repository) : IGameJamService
{
    public async Task<JamDto> CreateAsync(CreateJamCommand command, CancellationToken cancellationToken = default)
    {
        var request = command.Request;
        var jam = new Jam
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Slug = request.Slug.Trim(),
            Theme = request.Theme,
            Description = request.Description,
            Rules = request.Rules,
            SubmissionCriteria = request.SubmissionCriteria,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            VotingEndDate = request.VotingEndDate,
            MaxParticipants = request.MaxParticipants,
            CreatedBy = request.CreatedBy,
            Status = request.StartDate <= SystemClock.UtcNow ? JamStatus.Active : JamStatus.Upcoming
        };

        await repository.AddJamAsync(jam, cancellationToken).ConfigureAwait(false);
        return ToDto(jam);
    }

    public async Task<JamDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var jam = await repository.GetJamAsync(id, cancellationToken).ConfigureAwait(false);
        return jam is null ? null : ToDto(jam);
    }

    public async Task<IReadOnlyList<JamDto>> ListAsync(GetJamsQuery query, CancellationToken cancellationToken = default)
        => (await repository.ListJamsAsync(query.Status, query.Skip, query.Take, cancellationToken).ConfigureAwait(false))
            .Select(ToDto)
            .ToList();

    public async Task<JamDto?> SetStatusAsync(Guid id, JamStatus status, CancellationToken cancellationToken = default)
    {
        var jam = await repository.GetJamAsync(id, cancellationToken).ConfigureAwait(false);
        if (jam is null)
        {
            return null;
        }

        jam.Status = status;
        jam.UpdatedAt = SystemClock.UtcNow;
        await repository.UpdateJamAsync(jam, cancellationToken).ConfigureAwait(false);
        return ToDto(jam);
    }

    public async Task<JamSubmissionDto> SubmitAsync(SubmitJamEntryCommand command, CancellationToken cancellationToken = default)
    {
        var jam = await repository.GetJamAsync(command.JamId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Jam {command.JamId} was not found.");

        if (jam.MaxParticipants.HasValue && jam.ParticipantCount >= jam.MaxParticipants.Value)
        {
            throw new InvalidOperationException("The jam has reached its participant limit.");
        }

        var submission = new JamSubmission
        {
            Id = Guid.NewGuid(),
            JamId = command.JamId,
            ProjectVersionId = command.ProjectVersionId,
            UserId = command.UserId,
            SubmissionNotes = command.Notes
        };

        jam.ParticipantCount++;
        jam.UpdatedAt = SystemClock.UtcNow;
        await repository.AddSubmissionAsync(submission, cancellationToken).ConfigureAwait(false);
        await repository.UpdateJamAsync(jam, cancellationToken).ConfigureAwait(false);
        return ToDto(submission);
    }

    public async Task<IReadOnlyList<JamSubmissionDto>> GetSubmissionsAsync(Guid jamId, CancellationToken cancellationToken = default)
        => (await repository.GetSubmissionsAsync(jamId, cancellationToken).ConfigureAwait(false)).Select(ToDto).ToList();

    public async Task<JamCriteriaDto> AddCriteriaAsync(AddJamCriteriaCommand command, CancellationToken cancellationToken = default)
    {
        var criteria = new JamJudgingCriteria
        {
            Id = Guid.NewGuid(),
            JamId = command.JamId,
            Name = command.Name.Trim(),
            Description = command.Description,
            Weight = command.Weight <= 0 ? 1m : command.Weight,
            MaxScore = Math.Max(1, command.MaxScore)
        };

        await repository.AddCriteriaAsync(criteria, cancellationToken).ConfigureAwait(false);
        return ToDto(criteria);
    }

    public async Task<IReadOnlyList<JamCriteriaDto>> GetCriteriaAsync(Guid jamId, CancellationToken cancellationToken = default)
        => (await repository.GetCriteriaAsync(jamId, cancellationToken).ConfigureAwait(false)).Select(ToDto).ToList();

    public async Task<JamScoreDto> ScoreAsync(ScoreJamSubmissionCommand command, CancellationToken cancellationToken = default)
    {
        var submission = await repository.GetSubmissionAsync(command.SubmissionId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Submission {command.SubmissionId} was not found.");

        var criteria = (await repository.GetCriteriaAsync(submission.JamId, cancellationToken).ConfigureAwait(false))
            .FirstOrDefault(item => item.Id == command.CriteriaId)
            ?? throw new InvalidOperationException($"Criteria {command.CriteriaId} was not found.");

        var score = new JamScore
        {
            Id = Guid.NewGuid(),
            SubmissionId = command.SubmissionId,
            CriteriaId = command.CriteriaId,
            JudgeUserId = command.JudgeUserId,
            Score = Math.Clamp(command.Score, 0, criteria.MaxScore),
            Feedback = command.Feedback
        };

        await repository.AddScoreAsync(score, cancellationToken).ConfigureAwait(false);
        return ToDto(score);
    }

    private static JamDto ToDto(Jam jam)
        => new(jam.Id, jam.Name, jam.Slug, jam.Theme, jam.Description, jam.StartDate, jam.EndDate, jam.VotingEndDate, jam.MaxParticipants, jam.ParticipantCount, jam.Status, jam.CreatedBy);

    private static JamSubmissionDto ToDto(JamSubmission submission)
        => new(submission.Id, submission.JamId, submission.ProjectVersionId, submission.UserId, submission.SubmissionNotes);

    private static JamCriteriaDto ToDto(JamJudgingCriteria criteria)
        => new(criteria.Id, criteria.JamId, criteria.Name, criteria.Description, criteria.Weight, criteria.MaxScore);

    private static JamScoreDto ToDto(JamScore score)
        => new(score.Id, score.SubmissionId, score.CriteriaId, score.JudgeUserId, score.Score, score.Feedback);
}

public sealed class CreateJamCommandHandler(IGameJamService service) : ICommandHandler<CreateJamCommand, JamDto>
{
    public Task<JamDto> Handle(CreateJamCommand request, CancellationToken cancellationToken) => service.CreateAsync(request, cancellationToken);
}

public sealed class GetJamQueryHandler(IGameJamService service) : IQueryHandler<GetJamQuery, JamDto?>
{
    public Task<JamDto?> Handle(GetJamQuery request, CancellationToken cancellationToken) => service.GetAsync(request.JamId, cancellationToken);
}

public sealed class GetJamsQueryHandler(IGameJamService service) : IQueryHandler<GetJamsQuery, IReadOnlyList<JamDto>>
{
    public Task<IReadOnlyList<JamDto>> Handle(GetJamsQuery request, CancellationToken cancellationToken) => service.ListAsync(request, cancellationToken);
}

public sealed class SetJamStatusCommandHandler(IGameJamService service) : ICommandHandler<SetJamStatusCommand, JamDto?>
{
    public Task<JamDto?> Handle(SetJamStatusCommand request, CancellationToken cancellationToken) => service.SetStatusAsync(request.JamId, request.Status, cancellationToken);
}

public sealed class SubmitJamEntryCommandHandler(IGameJamService service) : ICommandHandler<SubmitJamEntryCommand, JamSubmissionDto>
{
    public Task<JamSubmissionDto> Handle(SubmitJamEntryCommand request, CancellationToken cancellationToken) => service.SubmitAsync(request, cancellationToken);
}

public sealed class AddJamCriteriaCommandHandler(IGameJamService service) : ICommandHandler<AddJamCriteriaCommand, JamCriteriaDto>
{
    public Task<JamCriteriaDto> Handle(AddJamCriteriaCommand request, CancellationToken cancellationToken) => service.AddCriteriaAsync(request, cancellationToken);
}

public sealed class ScoreJamSubmissionCommandHandler(IGameJamService service) : ICommandHandler<ScoreJamSubmissionCommand, JamScoreDto>
{
    public Task<JamScoreDto> Handle(ScoreJamSubmissionCommand request, CancellationToken cancellationToken) => service.ScoreAsync(request, cancellationToken);
}

[ApiController]
[Route("api/game-jams")]
public sealed class GameJamsController(ISender sender, IGameJamService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<JamDto>> List([FromQuery] JamStatus? status, [FromQuery] int skip, [FromQuery] int take, CancellationToken cancellationToken)
        => sender.Send(new GetJamsQuery(status, skip, take <= 0 ? 50 : take), cancellationToken);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var jam = await sender.Send(new GetJamQuery(id), cancellationToken).ConfigureAwait(false);
        return jam is null ? NotFound() : Ok(jam);
    }

    [HttpPost]
    public Task<JamDto> Create(CreateJamRequest request, CancellationToken cancellationToken)
        => sender.Send(new CreateJamCommand(request), cancellationToken);

    [HttpPost("{id:guid}/status/{status}")]
    public async Task<IActionResult> SetStatus(Guid id, JamStatus status, CancellationToken cancellationToken)
    {
        var jam = await sender.Send(new SetJamStatusCommand(id, status), cancellationToken).ConfigureAwait(false);
        return jam is null ? NotFound() : Ok(jam);
    }

    [HttpGet("{id:guid}/submissions")]
    public Task<IReadOnlyList<JamSubmissionDto>> GetSubmissions(Guid id, CancellationToken cancellationToken)
        => service.GetSubmissionsAsync(id, cancellationToken);

    [HttpPost("{id:guid}/submissions")]
    public Task<JamSubmissionDto> Submit(Guid id, SubmitJamEntryRequest request, CancellationToken cancellationToken)
        => sender.Send(new SubmitJamEntryCommand(id, request.ProjectVersionId, request.UserId, request.Notes), cancellationToken);

    [HttpGet("{id:guid}/criteria")]
    public Task<IReadOnlyList<JamCriteriaDto>> GetCriteria(Guid id, CancellationToken cancellationToken)
        => service.GetCriteriaAsync(id, cancellationToken);

    [HttpPost("{id:guid}/criteria")]
    public Task<JamCriteriaDto> AddCriteria(Guid id, AddJamCriteriaRequest request, CancellationToken cancellationToken)
        => sender.Send(new AddJamCriteriaCommand(id, request.Name, request.Description, request.Weight, request.MaxScore), cancellationToken);

    [HttpPost("submissions/{submissionId:guid}/scores")]
    public Task<JamScoreDto> Score(Guid submissionId, ScoreJamSubmissionRequest request, CancellationToken cancellationToken)
        => sender.Send(new ScoreJamSubmissionCommand(submissionId, request.CriteriaId, request.JudgeUserId, request.Score, request.Feedback), cancellationToken);
}

public sealed class GameJamsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new JamConfiguration());
        modelBuilder.ApplyConfiguration(new JamSubmissionConfiguration());
        modelBuilder.ApplyConfiguration(new JamJudgingCriteriaConfiguration());
        modelBuilder.ApplyConfiguration(new JamScoreConfiguration());
    }
}

public sealed class JamConfiguration : IEntityTypeConfiguration<Jam>
{
    public void Configure(EntityTypeBuilder<Jam> builder)
    {
        builder.ToTable("game_jams");
        builder.HasKey(jam => jam.Id);
        builder.Property(jam => jam.Name).HasMaxLength(255).IsRequired();
        builder.Property(jam => jam.Slug).HasMaxLength(255).IsRequired();
        builder.Property(jam => jam.Theme).HasMaxLength(500);
        builder.Property(jam => jam.Status).HasConversion<string>().HasMaxLength(40);
        builder.HasIndex(jam => jam.Slug).IsUnique();
        builder.HasIndex(jam => jam.Status);
    }
}

public sealed class JamSubmissionConfiguration : IEntityTypeConfiguration<JamSubmission>
{
    public void Configure(EntityTypeBuilder<JamSubmission> builder)
    {
        builder.ToTable("game_jam_submissions");
        builder.HasKey(submission => submission.Id);
        builder.HasIndex(submission => new { submission.JamId, submission.UserId }).IsUnique();
        builder.HasIndex(submission => submission.ProjectVersionId);
    }
}

public sealed class JamJudgingCriteriaConfiguration : IEntityTypeConfiguration<JamJudgingCriteria>
{
    public void Configure(EntityTypeBuilder<JamJudgingCriteria> builder)
    {
        builder.ToTable("game_jam_judging_criteria");
        builder.HasKey(criteria => criteria.Id);
        builder.Property(criteria => criteria.Name).HasMaxLength(100).IsRequired();
        builder.Property(criteria => criteria.Weight).HasPrecision(8, 2);
        builder.HasIndex(criteria => criteria.JamId);
    }
}

public sealed class JamScoreConfiguration : IEntityTypeConfiguration<JamScore>
{
    public void Configure(EntityTypeBuilder<JamScore> builder)
    {
        builder.ToTable("game_jam_scores");
        builder.HasKey(score => score.Id);
        builder.HasIndex(score => new { score.SubmissionId, score.CriteriaId, score.JudgeUserId }).IsUnique();
    }
}

public static class GameJamsDependencyInjection
{
    public static IServiceCollection AddGameJamsModule(this IServiceCollection services)
    {
        services.AddScoped<IGameJamRepository, GameJamRepository>();
        services.AddScoped<IGameJamService, GameJamService>();
        services.AddScoped<ICommandHandler<CreateJamCommand, JamDto>, CreateJamCommandHandler>();
        services.AddScoped<IRequestHandler<CreateJamCommand, JamDto>>(sp => sp.GetRequiredService<ICommandHandler<CreateJamCommand, JamDto>>());
        services.AddScoped<IQueryHandler<GetJamQuery, JamDto?>, GetJamQueryHandler>();
        services.AddScoped<IRequestHandler<GetJamQuery, JamDto?>>(sp => sp.GetRequiredService<IQueryHandler<GetJamQuery, JamDto?>>());
        services.AddScoped<IQueryHandler<GetJamsQuery, IReadOnlyList<JamDto>>, GetJamsQueryHandler>();
        services.AddScoped<IRequestHandler<GetJamsQuery, IReadOnlyList<JamDto>>>(sp => sp.GetRequiredService<IQueryHandler<GetJamsQuery, IReadOnlyList<JamDto>>>());
        services.AddScoped<ICommandHandler<SetJamStatusCommand, JamDto?>, SetJamStatusCommandHandler>();
        services.AddScoped<IRequestHandler<SetJamStatusCommand, JamDto?>>(sp => sp.GetRequiredService<ICommandHandler<SetJamStatusCommand, JamDto?>>());
        services.AddScoped<ICommandHandler<SubmitJamEntryCommand, JamSubmissionDto>, SubmitJamEntryCommandHandler>();
        services.AddScoped<IRequestHandler<SubmitJamEntryCommand, JamSubmissionDto>>(sp => sp.GetRequiredService<ICommandHandler<SubmitJamEntryCommand, JamSubmissionDto>>());
        services.AddScoped<ICommandHandler<AddJamCriteriaCommand, JamCriteriaDto>, AddJamCriteriaCommandHandler>();
        services.AddScoped<IRequestHandler<AddJamCriteriaCommand, JamCriteriaDto>>(sp => sp.GetRequiredService<ICommandHandler<AddJamCriteriaCommand, JamCriteriaDto>>());
        services.AddScoped<ICommandHandler<ScoreJamSubmissionCommand, JamScoreDto>, ScoreJamSubmissionCommandHandler>();
        services.AddScoped<IRequestHandler<ScoreJamSubmissionCommand, JamScoreDto>>(sp => sp.GetRequiredService<ICommandHandler<ScoreJamSubmissionCommand, JamScoreDto>>());
        return services;
    }
}

public sealed class GameJamsModule : ModuleBase
{
    public override string Name => "GameJams";
    public override int Order => 170;

    public override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
        => services.AddGameJamsModule();

    public override IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints) => endpoints;
}
