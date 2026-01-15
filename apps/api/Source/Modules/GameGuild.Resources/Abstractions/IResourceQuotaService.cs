
namespace GameGuild.Resources;

/// <summary>
///     Unified service for managing resource quotas and usage tracking.
///     <para>
///     <b>ISP Note:</b> This interface composes the segregated interfaces for backward compatibility.
///     New code should prefer depending on the specific interface needed:
///     </para>
///     <list type="bullet">
///         <item><see cref="IResourceQuotaReader"/> - Read-only quota/usage queries</item>
///         <item><see cref="IResourceQuotaWriter"/> - Admin quota configuration</item>
///         <item><see cref="IResourceQuotaEnforcer"/> - Consumption and limit enforcement</item>
///         <item><see cref="IResourceQuotaAnalytics"/> - Reporting and analytics</item>
///         <item><see cref="IResourceQuotaMaintenance"/> - Background maintenance tasks</item>
///     </list>
/// </summary>
public interface IResourceQuotaService : 
    IResourceQuotaReader, 
    IResourceQuotaWriter, 
    IResourceQuotaEnforcer, 
    IResourceQuotaAnalytics, 
    IResourceQuotaMaintenance
{
    // All methods are inherited from the segregated interfaces.
    // This unified interface exists for:
    // 1. Backward compatibility with existing code
    // 2. Cases where a component genuinely needs all capabilities
    // 3. Simplified DI registration (single implementation, multiple interfaces)
}

