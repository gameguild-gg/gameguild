using GameGuild.Modules.Experiments.Entities;

namespace GameGuild.Modules.Experiments.Repositories;

public interface IExperimentRepository
{
    Task<PricingExperiment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<PricingExperiment>> GetAllAsync(Guid? tenantId, ExperimentStatus? status, CancellationToken cancellationToken = default);
    Task CreateAsync(PricingExperiment experiment, CancellationToken cancellationToken = default);
    Task UpdateAsync(PricingExperiment experiment, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IVariantRepository
{
    Task<ExperimentVariant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ExperimentVariant>> GetByExperimentIdAsync(Guid experimentId, CancellationToken cancellationToken = default);
    Task CreateAsync(ExperimentVariant variant, CancellationToken cancellationToken = default);
    Task UpdateAsync(ExperimentVariant variant, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IAssignmentRepository
{
    Task<UserAssignment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserAssignment?> GetByUserAndExperimentAsync(Guid userId, Guid experimentId, CancellationToken cancellationToken = default);
    Task<List<UserAssignment>> GetByExperimentIdAsync(Guid experimentId, CancellationToken cancellationToken = default);
    Task CreateAsync(UserAssignment assignment, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserAssignment assignment, CancellationToken cancellationToken = default);
}
