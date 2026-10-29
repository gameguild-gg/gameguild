using System.Text.Json;

namespace GameGuild.AI;

/// <summary>
///     Chat message payload for AI requests.
/// </summary>
public sealed record AiChatMessage(string Role, string Content);

/// <summary>
///     Canonical request shape for conversational completions.
/// </summary>
public sealed record AiChatRequest(
    string? Backend,
    string? Model,
    string? SystemPrompt,
    IReadOnlyList<AiChatMessage> Messages,
    double? Temperature,
    int? MaxTokens);

/// <summary>
///     Canonical request shape for single-prompt generation.
/// </summary>
public sealed record AiGenerateRequest(
    string? Backend,
    string? Model,
    string? SystemPrompt,
    string Prompt,
    double? Temperature,
    int? MaxTokens);

/// <summary>
/// Server-derived billing actor for an AI execution. This value is created from the
/// authenticated request context and is never accepted from a browser payload.
/// </summary>
public sealed record AiExecutionActor(Guid TenantId, Guid UserId);

public sealed record AiResolvedModelDto(string Backend, string Provider, string Model, int MaximumOutputTokens);

/// <summary>
/// Terminal billing attribution for one AI execution, emitted after the request
/// reached a terminal outcome. Product modules record these through
/// <see cref="IAiExecutionBillingRecorder"/>; the platform registers a no-op
/// default so AI execution works without any product billing domain.
/// </summary>
public sealed record AiExecutionBillingRecord(
    Guid TenantId,
    Guid? ActorId,
    string Provider,
    string Model,
    int? InputTokens,
    int? OutputTokens,
    int? TotalTokens,
    string Outcome,
    string? OutcomeCode,
    string? OutcomeReason,
    DateTimeOffset OccurredAt);

/// <summary>
///     Normalized token usage information returned by a provider.
/// </summary>
public sealed record AiUsageDto(int? InputTokens, int? OutputTokens, int? TotalTokens);

/// <summary>
///     Canonical completion response shape exposed by the GameGuild API.
/// </summary>
public sealed record AiCompletionResponse(
    string Backend,
    string Provider,
    string Model,
    string Text,
    string? FinishReason,
    AiUsageDto Usage);

public enum AiGeneratedContentKind
{
    Email = 0,
    Report = 1,
    ListingDescription = 2
}

public sealed record AiGeneratedContentRequest(
    AiGeneratedContentKind Kind,
    string Subject,
    string Context,
    string? Audience,
    string? Tone,
    string? Backend,
    string? Model,
    int? MaxTokens);

public sealed record AiGeneratedContentDraftRequest(
    string Subject,
    string Context,
    string? Audience,
    string? Tone,
    string? Backend,
    string? Model,
    int? MaxTokens);

public sealed record AiBackendStatusDto(
    string Backend,
    string? Adapter,
    bool Configured,
    bool Ready,
    string? ReadinessCode,
    string? DefaultModel,
    string? BaseUrl,
    bool CredentialsConfigured,
    string? CredentialPolicy);

public sealed record AiStatusResponse(
    bool Enabled,
    string? DefaultBackend,
    bool AllowTenantOverrides,
    IReadOnlyList<AiBackendStatusDto> Backends);

public sealed record AiBackendReadinessDto(
    bool Available,
    string? Code = null,
    string? Message = null,
    string? Backend = null,
    string? Adapter = null,
    string? Model = null);

public sealed record AiQuotaStatusDto(
    string ResourceType,
    long CurrentUsage,
    long? SoftLimit,
    long? HardLimit,
    long Remaining,
    double UsagePercent,
    string Period,
    bool IsActive,
    DateTime? LastReset,
    DateTime? NextReset);

public sealed record AiQuotaStatusResponse(
    Guid TenantId,
    IReadOnlyList<AiQuotaStatusDto> Quotas,
    DateTime GeneratedAtUtc);

/// <summary>
///     Conversation history entry returned by the AI history endpoint.
/// </summary>
public sealed record AiConversationHistoryEntryDto(
    Guid Id,
    Guid? UserId,
    string RequestKind,
    string Provider,
    string Model,
    string RequestText,
    string? SystemPrompt,
    string? ResponseText,
    string Outcome,
    string? OutcomeCode,
    string? OutcomeReason,
    string? FinishReason,
    AiUsageDto Usage,
    DateTime OccurredAt);

public sealed record AiPromptTemplateDto(
    Guid Id,
    Guid? TenantId,
    string Key,
    string Name,
    string? Description,
    string Category,
    string? SystemPrompt,
    string Prompt,
    bool IsActive,
    bool IsSystemTemplate,
    Guid? CreatedByUserId,
    Guid? UpdatedByUserId,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateAiPromptTemplateRequest(
    string Key,
    string Name,
    string Prompt,
    string? Description = null,
    string? Category = null,
    string? SystemPrompt = null,
    bool? IsActive = null);

public sealed record UpdateAiPromptTemplateRequest(
    string? Name = null,
    string? Prompt = null,
    string? Description = null,
    string? Category = null,
    string? SystemPrompt = null,
    bool? IsActive = null);

public sealed record AiPromptTemplateRenderRequest(
    IReadOnlyDictionary<string, string?>? Variables = null);

public sealed record AiPromptTemplateRenderResponse(
    Guid TemplateId,
    string Key,
    string? SystemPrompt,
    string Prompt,
    IReadOnlyDictionary<string, string?> Variables);

public sealed record AiPromptTemplateGenerateRequest(
    IReadOnlyDictionary<string, string?>? Variables = null,
    string? Backend = null,
    string? Model = null,
    double? Temperature = null,
    int? MaxTokens = null);

internal enum AiRequestKind
{
    Chat,
    Generate
}

internal sealed record AiResolvedRequest(
    Guid TenantId,
    AiRequestKind RequestKind,
    string Backend,
    string Adapter,
    string Model,
    string? SystemPrompt,
    IReadOnlyList<AiChatMessage> Messages,
    double? Temperature,
    int? MaxTokens,
    string? ApiKey,
    string BaseUrl,
    AiModerationConfiguration Moderation,
    AiHistoryConfiguration History);

internal sealed record AiAdapterExecutionResult(
    string Model,
    string Text,
    string? FinishReason,
    int? InputTokens,
    int? OutputTokens,
    int? TotalTokens);

internal sealed record AiTenantBackendConfiguration(bool Enabled, string? DefaultModel);

internal sealed record AiModerationConfiguration(
    bool Enabled,
    IReadOnlyList<string> BlockedTerms,
    int? MaxPromptCharacters,
    int? MaxResponseCharacters);

internal sealed record AiHistoryConfiguration(bool Enabled);

internal sealed record AiTenantConfiguration(
    bool Enabled,
    bool AllowRequestOverride,
    string? DefaultBackend,
    string? DefaultChatModel,
    string? DefaultGenerateModel,
    IReadOnlyDictionary<string, AiTenantBackendConfiguration> Backends,
    IReadOnlyDictionary<string, string> ApiKeys,
    AiModerationConfiguration Moderation,
    AiHistoryConfiguration History);

internal static class AiContractJson
{
    public static JsonElement? ToJsonElement(object? value)
    {
        if (value is null)
            return null;

        if (value is JsonElement element)
            return element;

        return JsonSerializer.SerializeToElement(value, value.GetType());
    }
}
