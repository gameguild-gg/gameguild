using GameGuild;

namespace GameGuild.AI;

internal sealed class StandardAiAdapter(OpenAiCompatibleProtocolClient client) : IAiAdapter
{
    public string Key => AiAdapterKeys.StandardAi;

    public AiAdapterDescriptor Descriptor { get; } = new(
        AiAdapterKeys.StandardAi,
        RequiresExplicitBaseUrl: true,
        AiCredentialPolicy.Optional,
        DefaultBaseUrl: null,
        SupportsStreaming: true);

    public Task<Result<AiAdapterExecutionResult>> CompleteAsync(
        AiResolvedRequest request,
        CancellationToken cancellationToken = default) =>
        client.CompleteAsync(request, Descriptor, cancellationToken);

    public Task<Result<AiAdapterExecutionResult>> CompleteStreamingAsync(
        AiResolvedRequest request,
        Func<string, CancellationToken, ValueTask> onDelta,
        CancellationToken cancellationToken = default) =>
        client.CompleteStreamingAsync(request, Descriptor, onDelta, cancellationToken);
}
