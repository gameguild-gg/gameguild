using GameGuild.Database;
using GameGuild.Modules.Experiments.Entities;


namespace GameGuild.Modules.Experiments.Repositories;

public class ExperimentRepository : IExperimentRepository
{
    private readonly ApplicationDbContext _context;

    public ExperimentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PricingExperiment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<PricingExperiment>()
            .Include(e => e.Variants)
            .Include(e => e.UserAssignments)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<List<PricingExperiment>> GetAllAsync(Guid? tenantId, ExperimentStatus? status, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<PricingExperiment>()
            .Include(e => e.Variants)
            .AsQueryable();

        if (tenantId.HasValue)
            query = query.Where(e => e.TenantId == tenantId.Value);

        if (status.HasValue)
            query = query.Where(e => e.Status == status.Value);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(PricingExperiment experiment, CancellationToken cancellationToken = default)
    {
        await _context.Set<PricingExperiment>().AddAsync(experiment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PricingExperiment experiment, CancellationToken cancellationToken = default)
    {
        experiment.UpdatedAt = DateTime.UtcNow;
        _context.Set<PricingExperiment>().Update(experiment);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var experiment = await GetByIdAsync(id, cancellationToken);
        if (experiment != null)
        {
            _context.Set<PricingExperiment>().Remove(experiment);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

public class VariantRepository : IVariantRepository
{
    private readonly ApplicationDbContext _context;

    public VariantRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ExperimentVariant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ExperimentVariant>()
            .Include(v => v.Experiment)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task<List<ExperimentVariant>> GetByExperimentIdAsync(Guid experimentId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ExperimentVariant>()
            .Where(v => v.ExperimentId == experimentId)
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(ExperimentVariant variant, CancellationToken cancellationToken = default)
    {
        await _context.Set<ExperimentVariant>().AddAsync(variant, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ExperimentVariant variant, CancellationToken cancellationToken = default)
    {
        variant.UpdatedAt = DateTime.UtcNow;
        _context.Set<ExperimentVariant>().Update(variant);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var variant = await GetByIdAsync(id, cancellationToken);
        if (variant != null)
        {
            _context.Set<ExperimentVariant>().Remove(variant);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

public class AssignmentRepository : IAssignmentRepository
{
    private readonly ApplicationDbContext _context;

    public AssignmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserAssignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<UserAssignment>()
            .Include(a => a.Experiment)
            .Include(a => a.Variant)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<UserAssignment?> GetByUserAndExperimentAsync(Guid userId, Guid experimentId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<UserAssignment>()
            .Include(a => a.Variant)
            .FirstOrDefaultAsync(a => a.UserId == userId && a.ExperimentId == experimentId, cancellationToken);
    }

    public async Task<List<UserAssignment>> GetByExperimentIdAsync(Guid experimentId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<UserAssignment>()
            .Where(a => a.ExperimentId == experimentId)
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(UserAssignment assignment, CancellationToken cancellationToken = default)
    {
        await _context.Set<UserAssignment>().AddAsync(assignment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserAssignment assignment, CancellationToken cancellationToken = default)
    {
        assignment.UpdatedAt = DateTime.UtcNow;
        _context.Set<UserAssignment>().Update(assignment);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
