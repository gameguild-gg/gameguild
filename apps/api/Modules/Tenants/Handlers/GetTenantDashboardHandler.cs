using GameGuild.CQRS;

namespace GameGuild.Modules.Tenants;

/// <summary> Handler for GetTenantDashboardQuery </summary>
public class GetTenantDashboardHandler : IQueryHandler<GetTenantDashboardQuery, Result<TenantDashboardDto>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantMemberRepository _memberRepository;
    private readonly ITenantSettingsRepository _settingsRepository;
    private readonly IUsageTrackingService _usageTrackingService;

    public GetTenantDashboardHandler(
        ITenantRepository tenantRepository,
        ITenantMemberRepository memberRepository,
        ITenantSettingsRepository settingsRepository,
        IUsageTrackingService usageTrackingService)
    {
        _tenantRepository = tenantRepository;
        _memberRepository = memberRepository;
        _settingsRepository = settingsRepository;
        _usageTrackingService = usageTrackingService;
    }

    public async Task<Result<TenantDashboardDto>> Handle(GetTenantDashboardQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
            if (tenant == null)
            {
                return Result<TenantDashboardDto>.Failure("Tenant not found");
            }

            var members = await _memberRepository.GetByTenantIdAsync(request.TenantId, includeInactive: true, cancellationToken);
            var settings = await _settingsRepository.GetByTenantIdAsync(request.TenantId, cancellationToken);
            
            var fromDate = request.FromDate ?? DateTime.UtcNow.AddMonths(-1);
            var toDate = request.ToDate ?? DateTime.UtcNow;

            var newMembersThisMonth = members.Count(m => m.JoinedAt >= DateTime.UtcNow.AddMonths(-1));
            var usage = await _usageTrackingService.GetUsageAsync(request.TenantId, fromDate, toDate, cancellationToken);

            var dashboard = new TenantDashboardDto
            {
                TenantId = tenant.Id,
                Name = tenant.Name,
                IsActive = tenant.IsActive,
                IsArchived = tenant.IsArchived,
                CreatedAt = tenant.CreatedAt,
                LastActivityAt = tenant.UpdatedAt,
                
                TotalMembers = members.Count,
                ActiveMembers = members.Count(m => m.IsActive),
                NewMembersThisMonth = newMembersThisMonth,
                
                StorageUsed = usage?.StorageUsed ?? 0,
                StorageLimit = settings?.StorageQuota ?? 0,
                ApiCallsThisMonth = usage?.ApiCalls ?? 0,
                ApiCallsLimit = settings?.MaxUsers ?? 0, // This would need proper API limits tracking
                
                Settings = settings,
                RecentActivities = await GetRecentActivities(request.TenantId, cancellationToken)
            };

            return Result<TenantDashboardDto>.Success(dashboard);
        }
        catch (Exception ex)
        {
            return Result<TenantDashboardDto>.Failure($"Error retrieving tenant dashboard: {ex.Message}");
        }
    }

    private async Task<IEnumerable<RecentActivityDto>> GetRecentActivities(Guid tenantId, CancellationToken cancellationToken)
    {
        // This would integrate with an audit log or activity tracking system
        // For now, return empty collection
        await Task.CompletedTask;
        return Enumerable.Empty<RecentActivityDto>();
    }
}