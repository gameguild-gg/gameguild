namespace GameGuild.Resources;

/// <summary>
///     Sub-service handling analytics, reporting, and background maintenance.
///     Implements <see cref="IResourceQuotaAnalytics"/> and <see cref="IResourceQuotaMaintenance"/>.
/// </summary>
public interface IQuotaMaintenanceService : IResourceQuotaAnalytics, IResourceQuotaMaintenance;
