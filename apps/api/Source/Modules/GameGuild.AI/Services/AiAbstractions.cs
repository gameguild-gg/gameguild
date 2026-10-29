using GameGuild;

namespace GameGuild.AI;

/// <summary>
///     Orchestrates backend selection, credential resolution, and execution of AI requests.
/// </summary>
public interface IAiOrchestrator
{
    Task<Result<AiCompletionResponse>> ChatAsync(AiChatRequest request, CancellationToken cancellationToken = default);

    Task<Result<AiCompletionResponse>> GenerateAsync(AiGenerateRequest request, CancellationToken cancellationToken = default);

    Task<Result<AiResolvedModelDto>> DescribeGenerateAsync(
        AiExecutionActor actor,
        AiGenerateRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AiCompletionResponse>> GenerateForActorAsync(
        AiExecutionActor actor,
        AiGenerateRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AiCompletionResponse>> GenerateForActorStreamingAsync(
        AiExecutionActor actor,
        AiGenerateRequest request,
        Func<string, CancellationToken, ValueTask> onDelta,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a streamed request whose request and token quota envelope was
    /// already reserved atomically by the owning workflow. Backend and adapter resolution,
    /// moderation, history, and actor binding remain enforced here.
    /// </summary>
    Task<Result<AiCompletionResponse>> GenerateForActorStreamingWithReservedQuotaAsync(
        AiExecutionActor actor,
        AiGenerateRequest request,
        Func<string, CancellationToken, ValueTask> onDelta,
        CancellationToken cancellationToken = default);
}

public interface IAiConversationHistoryReader
{
    Task<IReadOnlyList<AiConversationHistoryEntryDto>> GetRecentAsync(Guid tenantId, int take, CancellationToken cancellationToken = default);
}

/// <summary>
/// Exposes sanitized backend readiness without revealing credentials or adapter internals.
/// </summary>
public interface IAiBackendCatalog
{
    AiBackendReadinessDto GetReadiness(string? backend = null, string? model = null);

    IReadOnlyList<AiBackendStatusDto> GetStatus();
}

/// <summary>
/// Records terminal AI execution outcomes for product billing attribution.
/// The platform registers a <see cref="NoOpAiExecutionBillingRecorder"/> default;
/// product modules replace it with their own billing implementation.
/// </summary>
public interface IAiExecutionBillingRecorder
{
    ValueTask RecordExecutionAsync(AiExecutionBillingRecord record, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default billing recorder that discards execution records. Used when no
/// product billing domain is registered.
/// </summary>
public sealed class NoOpAiExecutionBillingRecorder : IAiExecutionBillingRecorder
{
    public ValueTask RecordExecutionAsync(AiExecutionBillingRecord record, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}

public interface IAiPromptTemplateService
{
    Task<Result<IReadOnlyList<AiPromptTemplateDto>>> ListAsync(
        Guid tenantId,
        string? category = null,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<Result<AiPromptTemplateDto>> GetAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);

    Task<Result<AiPromptTemplateDto>> CreateAsync(
        Guid tenantId,
        Guid? userId,
        CreateAiPromptTemplateRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<AiPromptTemplateDto>> UpdateAsync(
        Guid tenantId,
        Guid id,
        Guid? userId,
        UpdateAiPromptTemplateRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid tenantId, Guid id, Guid? userId, CancellationToken cancellationToken = default);

    Task<Result<AiPromptTemplateRenderResponse>> RenderAsync(
        Guid tenantId,
        Guid id,
        IReadOnlyDictionary<string, string?>? variables,
        CancellationToken cancellationToken = default);
}

internal interface IAiConversationHistoryRepository
    : IAiConversationHistoryReader
{
    Task AddAsync(AiConversationLog entry, CancellationToken cancellationToken = default);
}

internal enum AiCredentialPolicy
{
    Required,
    Optional,
    None,
}

internal sealed record AiAdapterDescriptor(
    string Key,
    bool RequiresExplicitBaseUrl,
    AiCredentialPolicy CredentialPolicy,
    string? DefaultBaseUrl,
    bool SupportsStreaming);

internal interface IAiAdapter
{
    string Key { get; }

    AiAdapterDescriptor Descriptor { get; }

    Task<Result<AiAdapterExecutionResult>> CompleteAsync(AiResolvedRequest request, CancellationToken cancellationToken = default);

    async Task<Result<AiAdapterExecutionResult>> CompleteStreamingAsync(
        AiResolvedRequest request,
        Func<string, CancellationToken, ValueTask> onDelta,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(onDelta);
        var completion = await CompleteAsync(request, cancellationToken).ConfigureAwait(false);
        if (completion.IsSuccess)
            await onDelta(completion.Value.Text, cancellationToken).ConfigureAwait(false);
        return completion;
    }
}
