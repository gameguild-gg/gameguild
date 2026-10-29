using GameGuild;

namespace GameGuild.AI;

internal sealed class OpenAiAdapter(OpenAiCompatibleProtocolClient client) : IAiAdapter
{
    public string Key => AiAdapterKeys.OpenAi;

    public AiAdapterDescriptor Descriptor { get; } = new(
        AiAdapterKeys.OpenAi,
        RequiresExplicitBaseUrl: false,
        AiCredentialPolicy.Required,
        "https://api.openai.com",
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
