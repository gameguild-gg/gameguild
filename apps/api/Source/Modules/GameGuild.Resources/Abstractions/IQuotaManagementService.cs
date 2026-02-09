namespace GameGuild.Resources;

/// <summary>
///     Sub-service handling quota CRUD and basic usage reads.
///     Implements <see cref="IResourceQuotaReader"/> and <see cref="IResourceQuotaWriter"/>.
/// </summary>
public interface IQuotaManagementService : IResourceQuotaReader, IResourceQuotaWriter;
