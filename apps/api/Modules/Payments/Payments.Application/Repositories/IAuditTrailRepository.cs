using GameGuild.Modules.Payments.Payments.Domain.Entities;

namespace GameGuild.Modules.Payments.Payments.Application.Repositories;

public interface IAuditTrailRepository
{
    Task<List<AuditTrail>> GetByEntityAsync(string entityType, Guid entityId, int skip, int take, CancellationToken cancellationToken = default);
    Task AddAsync(AuditTrail auditTrail, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
