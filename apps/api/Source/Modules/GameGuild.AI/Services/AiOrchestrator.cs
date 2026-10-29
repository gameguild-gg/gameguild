using GameGuild;
using GameGuild.Identity.Tenants;
using GameGuild.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace GameGuild.AI;

internal sealed class AiOrchestrator(
    AiAdapterRegistry adapterRegistry,
    AiBackendCatalog backendCatalog,
    IRequestContextAccessor requestContextAccessor,
    ITenantSettingsRepository tenantSettingsRepository,
    IResourceQuotaEnforcer quotaEnforcer,
    IAiConversationHistoryRepository historyRepository,
    IOptions<AiOptions> aiOptions,
    ILogger<AiOrchestrator> logger,
    IAiExecutionBillingRecorder? billingRecorder = null) : IAiOrchestrator
{
    private readonly IAiExecutionBillingRecorder _billingRecorder = billingRecorder ?? new NoOpAiExecutionBillingRecorder();
    private static readonly HashSet<string> AllowedMessageRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "user",
        "assistant"
    };

    private readonly AiOptions _aiOptions = aiOptions.Value;

    public async Task<Result<AiCompletionResponse>> ChatAsync(AiChatRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Messages is null || request.Messages.Count == 0)
        {
            return Result.Failure<AiCompletionResponse>(Error.Validation("AI.MessagesRequired", "At least one chat message is required."));
        }

        if (request.Messages.Any(static message => string.IsNullOrWhiteSpace(message.Content)))
        {
            return Result.Failure<AiCompletionResponse>(Error.Validation("AI.MessageContentRequired", "Chat message content cannot be empty."));
        }

        if (request.Messages.Any(static message => string.IsNullOrWhiteSpace(message.Role) || !AllowedMessageRoles.Contains(message.Role)))
        {
            return Result.Failure<AiCompletionResponse>(Error.Validation("AI.InvalidMessageRole", "Only 'user' and 'assistant' roles are supported."));
        }

        var resolvedResult = await ResolveRequestAsync(
            requestContextAccessor.CurrentTenantId,
            request.Backend,
            request.Model,
            request.SystemPrompt,
            request.Messages,
            request.Temperature,
            request.MaxTokens,
            AiRequestKind.Chat,
            cancellationToken).ConfigureAwait(false);

        if (resolvedResult.IsFailure)
        {
            return Result.Failure<AiCompletionResponse>(resolvedResult.Error);
        }

        return await ExecuteAsync(resolvedResult.Value, requestContextAccessor.CurrentUserId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<AiCompletionResponse>> GenerateAsync(AiGenerateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return Result.Failure<AiCompletionResponse>(Error.Validation("AI.PromptRequired", "A prompt is required."));
        }

        var messages = new[]
        {
            new AiChatMessage("user", request.Prompt.Trim())
        };

        var resolvedResult = await ResolveRequestAsync(
            requestContextAccessor.CurrentTenantId,
            request.Backend,
            request.Model,
            request.SystemPrompt,
            messages,
            request.Temperature,
            request.MaxTokens,
            AiRequestKind.Generate,
            cancellationToken).ConfigureAwait(false);

        if (resolvedResult.IsFailure)
        {
            return Result.Failure<AiCompletionResponse>(resolvedResult.Error);
        }

        return await ExecuteAsync(resolvedResult.Value, requestContextAccessor.CurrentUserId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<AiResolvedModelDto>> DescribeGenerateAsync(
        AiExecutionActor actor,
        AiGenerateRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);
        var messages = new[] { new AiChatMessage("user", string.IsNullOrWhiteSpace(request.Prompt) ? "preview" : request.Prompt.Trim()) };
        var resolved = await ResolveRequestAsync(
            actor.TenantId,
            request.Backend,
            request.Model,
            request.SystemPrompt,
            messages,
            request.Temperature,
            request.MaxTokens,
            AiRequestKind.Generate,
            cancellationToken).ConfigureAwait(false);
        return resolved.IsFailure
            ? Result.Failure<AiResolvedModelDto>(resolved.Error)
            : Result.Success(new AiResolvedModelDto(
                resolved.Value.Backend,
                resolved.Value.Adapter,
                resolved.Value.Model,
                resolved.Value.MaxTokens ?? 2048));
    }

    public async Task<Result<AiCompletionResponse>> GenerateForActorAsync(
        AiExecutionActor actor,
        AiGenerateRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return Result.Failure<AiCompletionResponse>(Error.Validation("AI.PromptRequired", "A prompt is required."));
        }

        var resolved = await ResolveRequestAsync(
            actor.TenantId,
            request.Backend,
            request.Model,
            request.SystemPrompt,
            [new AiChatMessage("user", request.Prompt.Trim())],
            request.Temperature,
            request.MaxTokens,
            AiRequestKind.Generate,
            cancellationToken).ConfigureAwait(false);
        return resolved.IsFailure
            ? Result.Failure<AiCompletionResponse>(resolved.Error)
            : await ExecuteAsync(resolved.Value, actor.UserId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result<AiCompletionResponse>> GenerateForActorStreamingAsync(
        AiExecutionActor actor,
        AiGenerateRequest request,
        Func<string, CancellationToken, ValueTask> onDelta,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(onDelta);
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return Result.Failure<AiCompletionResponse>(Error.Validation("AI.PromptRequired", "A prompt is required."));
        }

        var resolved = await ResolveRequestAsync(
            actor.TenantId,
            request.Backend,
            request.Model,
            request.SystemPrompt,
            [new AiChatMessage("user", request.Prompt.Trim())],
            request.Temperature,
            request.MaxTokens,
            AiRequestKind.Generate,
            cancellationToken).ConfigureAwait(false);
        return resolved.IsFailure
            ? Result.Failure<AiCompletionResponse>(resolved.Error)
            : await ExecuteAsync(resolved.Value, actor.UserId, cancellationToken, onDelta).ConfigureAwait(false);
    }

    public async Task<Result<AiCompletionResponse>> GenerateForActorStreamingWithReservedQuotaAsync(
        AiExecutionActor actor,
        AiGenerateRequest request,
        Func<string, CancellationToken, ValueTask> onDelta,
        CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(onDelta);
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return Result.Failure<AiCompletionResponse>(Error.Validation("AI.PromptRequired", "A prompt is required."));
        }

        var resolved = await ResolveRequestAsync(
            actor.TenantId,
            request.Backend,
            request.Model,
            request.SystemPrompt,
            [new AiChatMessage("user", request.Prompt.Trim())],
            request.Temperature,
            request.MaxTokens,
            AiRequestKind.Generate,
            cancellationToken).ConfigureAwait(false);
        return resolved.IsFailure
            ? Result.Failure<AiCompletionResponse>(resolved.Error)
            : await ExecuteAsync(
                resolved.Value,
                actor.UserId,
                cancellationToken,
                onDelta,
                enforceQuota: false).ConfigureAwait(false);
    }

    private static void ValidateActor(AiExecutionActor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        if (actor.TenantId == Guid.Empty)
        {
            throw new ArgumentException("A tenant is required for an AI execution.", nameof(actor));
        }

        if (actor.UserId == Guid.Empty)
        {
            throw new ArgumentException("A user is required for an AI execution.", nameof(actor));
        }
    }

    private async Task<Result<AiResolvedRequest>> ResolveRequestAsync(
        Guid? tenantIdOverride,
        string? requestedBackend,
        string? requestedModel,
        string? systemPrompt,
        IReadOnlyList<AiChatMessage> messages,
        double? temperature,
        int? maxTokens,
        AiRequestKind requestKind,
        CancellationToken cancellationToken)
    {
        if (!_aiOptions.Enabled)
        {
            return Result.Failure<AiResolvedRequest>(Error.Forbidden("AI.Disabled", "AI functionality is disabled for this environment."));
        }

        if (!tenantIdOverride.HasValue)
        {
            return Result.Failure<AiResolvedRequest>(Error.Forbidden("AI.TenantContextRequired", "AI requests require an active tenant context."));
        }

        var tenantId = tenantIdOverride.Value;
        var tenantSettings = await tenantSettingsRepository.GetByTenantIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var integrationSettings = tenantSettings is null
            ? TenantIntegrationSettingsSerializer.Empty()
            : TenantIntegrationSettingsSerializer.Deserialize(tenantSettings.IntegrationSettingsJson);
        var tenantConfiguration = ParseTenantConfiguration(integrationSettings);

        if (!tenantConfiguration.Enabled)
        {
            return Result.Failure<AiResolvedRequest>(Error.Forbidden("AI.TenantDisabled", "AI functionality is disabled for this tenant."));
        }

        var backendResult = ResolveBackend(requestedBackend, tenantConfiguration);
        if (backendResult.IsFailure)
        {
            return Result.Failure<AiResolvedRequest>(backendResult.Error);
        }

        var backend = backendResult.Value;

        if (tenantConfiguration.Backends.TryGetValue(backend, out var tenantBackendConfiguration)
            && !tenantBackendConfiguration.Enabled)
        {
            return Result.Failure<AiResolvedRequest>(Error.Forbidden(
                "AI.BackendDisabled",
                $"Backend '{backend}' is disabled for this tenant."));
        }

        var model = ResolveModel(requestedModel, requestKind, backend, tenantConfiguration);
        var apiKey = ResolveApiKey(backend, tenantConfiguration);
        var platformResolution = backendCatalog.Resolve(backend, model, apiKey);
        if (platformResolution.IsFailure)
        {
            return Result.Failure<AiResolvedRequest>(platformResolution.Error);
        }

        var resolvedBackend = platformResolution.Value;

        return Result.Success(new AiResolvedRequest(
            tenantId,
            requestKind,
            resolvedBackend.Backend,
            resolvedBackend.Adapter,
            resolvedBackend.Model,
            string.IsNullOrWhiteSpace(systemPrompt) ? null : systemPrompt.Trim(),
            messages,
            temperature,
            maxTokens,
            resolvedBackend.ApiKey,
            resolvedBackend.BaseUrl,
            tenantConfiguration.Moderation,
            tenantConfiguration.History));
    }

    private async Task<Result<AiCompletionResponse>> ExecuteAsync(
        AiResolvedRequest resolvedRequest,
        Guid? actorId,
        CancellationToken cancellationToken,
        Func<string, CancellationToken, ValueTask>? onDelta = null,
        bool enforceQuota = true)
    {
        var inputModerationResult = ModerateInput(resolvedRequest);
        if (inputModerationResult.IsFailure)
        {
            await RecordBillingAsync(resolvedRequest, actorId, null, "ModerationBlocked", inputModerationResult.Error.Code, inputModerationResult.Error.Description, cancellationToken).ConfigureAwait(false);
            await RecordHistoryAsync(resolvedRequest, actorId, null, "ModerationBlocked", inputModerationResult.Error.Code, inputModerationResult.Error.Description, cancellationToken).ConfigureAwait(false);
            return Result.Failure<AiCompletionResponse>(inputModerationResult.Error);
        }

        if (enforceQuota)
        {
            var requestQuotaResult = await ConsumeQuotaAsync(resolvedRequest.TenantId, ResourceUsageType.AiRequests, 1, "AI request quota exceeded.", cancellationToken).ConfigureAwait(false);
            if (requestQuotaResult.IsFailure)
            {
                await RecordBillingAsync(resolvedRequest, actorId, null, "Failed", requestQuotaResult.Error.Code, requestQuotaResult.Error.Description, cancellationToken).ConfigureAwait(false);
                return Result.Failure<AiCompletionResponse>(requestQuotaResult.Error);
            }
        }

        if (!adapterRegistry.TryGet(resolvedRequest.Adapter, out var adapter))
        {
            if (enforceQuota)
            {
                await quotaEnforcer.DecrementUsageAsync(resolvedRequest.TenantId, ResourceUsageType.AiRequests, 1, actorId, "AI.AdapterNotRegistered", cancellationToken).ConfigureAwait(false);
            }

            await RecordBillingAsync(resolvedRequest, actorId, null, "Failed", "AI.AdapterNotRegistered", $"Adapter '{resolvedRequest.Adapter}' is not registered.", cancellationToken).ConfigureAwait(false);
            return Result.Failure<AiCompletionResponse>(Error.Problem(
                "AI.AdapterNotRegistered",
                $"Adapter '{resolvedRequest.Adapter}' is not registered."));
        }

        var canStreamBeforeModeration = onDelta is not null && !resolvedRequest.Moderation.Enabled;
        var executionResult = canStreamBeforeModeration
            ? await adapter.CompleteStreamingAsync(resolvedRequest, onDelta!, cancellationToken).ConfigureAwait(false)
            : await adapter.CompleteAsync(resolvedRequest, cancellationToken).ConfigureAwait(false);
        if (executionResult.IsFailure)
        {
            if (enforceQuota)
            {
                await quotaEnforcer.DecrementUsageAsync(resolvedRequest.TenantId, ResourceUsageType.AiRequests, 1, actorId, "AI.AdapterExecutionFailed", cancellationToken).ConfigureAwait(false);
            }

            await RecordBillingAsync(resolvedRequest, actorId, null, "Failed", executionResult.Error.Code, executionResult.Error.Description, cancellationToken).ConfigureAwait(false);
            await RecordHistoryAsync(resolvedRequest, actorId, null, "Failed", executionResult.Error.Code, executionResult.Error.Description, cancellationToken).ConfigureAwait(false);
            return Result.Failure<AiCompletionResponse>(executionResult.Error);
        }

        var providerResult = executionResult.Value;

        var totalTokens = providerResult.TotalTokens ?? ((providerResult.InputTokens ?? 0) + (providerResult.OutputTokens ?? 0));
        if (enforceQuota && totalTokens > 0)
        {
            var tokenQuotaResult = await ConsumeQuotaAsync(resolvedRequest.TenantId, ResourceUsageType.AiTokens, totalTokens, "AI token quota exceeded.", cancellationToken).ConfigureAwait(false);
            if (tokenQuotaResult.IsFailure)
            {
                await RecordBillingAsync(resolvedRequest, actorId, providerResult, "Failed", tokenQuotaResult.Error.Code, tokenQuotaResult.Error.Description, cancellationToken).ConfigureAwait(false);
                await RecordHistoryAsync(resolvedRequest, actorId, providerResult, "Failed", tokenQuotaResult.Error.Code, tokenQuotaResult.Error.Description, cancellationToken).ConfigureAwait(false);
                return Result.Failure<AiCompletionResponse>(tokenQuotaResult.Error);
            }
        }

        var outputModerationResult = ModerateOutput(resolvedRequest, providerResult);
        if (outputModerationResult.IsFailure)
        {
            await RecordBillingAsync(resolvedRequest, actorId, providerResult, "ModerationBlocked", outputModerationResult.Error.Code, outputModerationResult.Error.Description, cancellationToken).ConfigureAwait(false);
            await RecordHistoryAsync(resolvedRequest, actorId, providerResult, "ModerationBlocked", outputModerationResult.Error.Code, outputModerationResult.Error.Description, cancellationToken).ConfigureAwait(false);
            return Result.Failure<AiCompletionResponse>(outputModerationResult.Error);
        }

        // Tenant output moderation must complete before any text can leave the
        // orchestrator. In that mode the approved response is emitted once.
        if (onDelta is not null && !canStreamBeforeModeration)
        {
            await onDelta(providerResult.Text, cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation(
            "Completed AI request for tenant {TenantId} using backend {Backend}, adapter {Adapter}, and model {Model}",
            resolvedRequest.TenantId,
            resolvedRequest.Backend,
            resolvedRequest.Adapter,
            providerResult.Model);

        await RecordBillingAsync(resolvedRequest, actorId, providerResult, "Completed", null, null, cancellationToken).ConfigureAwait(false);
        await RecordHistoryAsync(resolvedRequest, actorId, providerResult, "Completed", null, null, cancellationToken).ConfigureAwait(false);

        return Result.Success(new AiCompletionResponse(
            resolvedRequest.Backend,
            resolvedRequest.Adapter,
            providerResult.Model,
            providerResult.Text,
            providerResult.FinishReason,
            new AiUsageDto(providerResult.InputTokens, providerResult.OutputTokens, providerResult.TotalTokens)));
    }

    private async Task RecordBillingAsync(
        AiResolvedRequest resolvedRequest,
        Guid? actorId,
        AiAdapterExecutionResult? providerResult,
        string outcome,
        string? outcomeCode,
        string? outcomeReason,
        CancellationToken cancellationToken)
    {
        var record = new AiExecutionBillingRecord(
            resolvedRequest.TenantId,
            actorId,
            resolvedRequest.Adapter,
            providerResult?.Model ?? resolvedRequest.Model,
            providerResult?.InputTokens,
            providerResult?.OutputTokens,
            providerResult?.TotalTokens,
            outcome,
            outcomeCode,
            outcomeReason,
            SystemClock.UtcNow);
        await _billingRecorder.RecordExecutionAsync(record, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<bool>> ConsumeQuotaAsync(Guid tenantId, ResourceUsageType type, long amount, string message, CancellationToken cancellationToken)
    {
        var (success, currentUsage, hardLimit) = await quotaEnforcer
            .TryAtomicConsumeAsync(tenantId, type, amount, cancellationToken)
            .ConfigureAwait(false);

        if (success)
        {
            return Result.Success(true);
        }

        return Result.Failure<bool>(Error.Problem(
            type == ResourceUsageType.AiRequests ? "AI.RequestQuotaExceeded" : "AI.TokenQuotaExceeded",
            hardLimit.HasValue
                ? $"{message} Current usage: {currentUsage}, limit: {hardLimit.Value}."
                : message));
    }

    private static Result<bool> ModerateInput(AiResolvedRequest resolvedRequest)
    {
        if (!resolvedRequest.Moderation.Enabled)
        {
            return Result.Success(true);
        }

        var requestText = BuildRequestText(resolvedRequest.Messages, resolvedRequest.SystemPrompt);
        if (resolvedRequest.Moderation.MaxPromptCharacters.HasValue
            && requestText.Length > resolvedRequest.Moderation.MaxPromptCharacters.Value)
        {
            return Result.Failure<bool>(Error.Validation(
                "AI.PromptTooLong",
                $"Prompt length exceeds the configured tenant moderation limit of {resolvedRequest.Moderation.MaxPromptCharacters.Value} characters."));
        }

        if (TryFindBlockedTerm(requestText, resolvedRequest.Moderation.BlockedTerms, out var blockedTerm))
        {
            return Result.Failure<bool>(Error.Validation(
                "AI.ModerationBlockedTerm",
                $"Prompt contains a blocked term for this tenant: '{blockedTerm}'."));
        }

        return Result.Success(true);
    }

    private static Result<bool> ModerateOutput(AiResolvedRequest resolvedRequest, AiAdapterExecutionResult providerResult)
    {
        if (!resolvedRequest.Moderation.Enabled)
        {
            return Result.Success(true);
        }

        if (resolvedRequest.Moderation.MaxResponseCharacters.HasValue
            && providerResult.Text.Length > resolvedRequest.Moderation.MaxResponseCharacters.Value)
        {
            return Result.Failure<bool>(Error.Validation(
                "AI.ResponseTooLong",
                $"Generated response exceeds the configured tenant moderation limit of {resolvedRequest.Moderation.MaxResponseCharacters.Value} characters."));
        }

        if (TryFindBlockedTerm(providerResult.Text, resolvedRequest.Moderation.BlockedTerms, out var blockedTerm))
        {
            return Result.Failure<bool>(Error.Validation(
                "AI.ModerationBlockedTerm",
                $"Generated response contains a blocked term for this tenant: '{blockedTerm}'."));
        }

        return Result.Success(true);
    }

    private async Task RecordHistoryAsync(
        AiResolvedRequest resolvedRequest,
        Guid? actorId,
        AiAdapterExecutionResult? providerResult,
        string outcome,
        string? outcomeCode,
        string? outcomeReason,
        CancellationToken cancellationToken)
    {
        if (!resolvedRequest.History.Enabled)
        {
            return;
        }

        var entry = new AiConversationLog
        {
            TenantId = resolvedRequest.TenantId,
            UserId = actorId,
            RequestKind = resolvedRequest.RequestKind.ToString(),
            Provider = resolvedRequest.Adapter,
            Model = providerResult?.Model ?? resolvedRequest.Model,
            RequestText = BuildRequestText(resolvedRequest.Messages),
            SystemPrompt = resolvedRequest.SystemPrompt,
            ResponseText = providerResult?.Text,
            Outcome = outcome,
            OutcomeCode = outcomeCode,
            OutcomeReason = outcomeReason,
            FinishReason = providerResult?.FinishReason,
            InputTokens = providerResult?.InputTokens,
            OutputTokens = providerResult?.OutputTokens,
            TotalTokens = providerResult?.TotalTokens,
            OccurredAt = SystemClock.UtcNow,
        };

        await historyRepository.AddAsync(entry, cancellationToken).ConfigureAwait(false);
    }

    private static string BuildRequestText(IReadOnlyList<AiChatMessage> messages, string? systemPrompt = null)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            parts.Add($"system: {systemPrompt.Trim()}");
        }

        parts.AddRange(messages
            .Where(static message => !string.IsNullOrWhiteSpace(message.Content))
            .Select(message => $"{message.Role}: {message.Content.Trim()}"));

        return string.Join("\n", parts);
    }

    private static bool TryFindBlockedTerm(string text, IReadOnlyList<string> blockedTerms, out string blockedTerm)
    {
        foreach (var term in blockedTerms)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                continue;
            }

            if (text.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                blockedTerm = term;
                return true;
            }
        }

        blockedTerm = string.Empty;
        return false;
    }

    private Result<string> ResolveBackend(string? requestedBackend, AiTenantConfiguration tenantConfiguration)
    {
        if (!string.IsNullOrWhiteSpace(requestedBackend))
        {
            if (!tenantConfiguration.AllowRequestOverride)
            {
                return Result.Failure<string>(Error.Forbidden(
                    "AI.BackendOverrideForbidden",
                    "This tenant does not allow backend overrides per request."));
            }

            return Result.Success(requestedBackend.Trim());
        }

        if (!string.IsNullOrWhiteSpace(tenantConfiguration.DefaultBackend))
        {
            return Result.Success(tenantConfiguration.DefaultBackend.Trim());
        }

        if (!string.IsNullOrWhiteSpace(_aiOptions.DefaultBackend))
        {
            return Result.Success(_aiOptions.DefaultBackend.Trim());
        }

        return Result.Failure<string>(Error.Problem(
            "AI.BackendNotConfigured",
            "No default AI backend is configured for this tenant or environment."));
    }

    private static string? ResolveModel(
        string? requestedModel,
        AiRequestKind requestKind,
        string backend,
        AiTenantConfiguration tenantConfiguration)
    {
        if (!string.IsNullOrWhiteSpace(requestedModel))
        {
            return requestedModel;
        }

        var tenantDefaultModel = requestKind switch
        {
            AiRequestKind.Chat when !string.IsNullOrWhiteSpace(tenantConfiguration.DefaultChatModel) => tenantConfiguration.DefaultChatModel,
            AiRequestKind.Generate when !string.IsNullOrWhiteSpace(tenantConfiguration.DefaultGenerateModel) => tenantConfiguration.DefaultGenerateModel,
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(tenantDefaultModel))
        {
            return tenantDefaultModel;
        }

        if (tenantConfiguration.Backends.TryGetValue(backend, out var tenantBackendConfiguration)
            && !string.IsNullOrWhiteSpace(tenantBackendConfiguration.DefaultModel))
        {
            return tenantBackendConfiguration.DefaultModel;
        }

        return null;
    }

    private static string? ResolveApiKey(string backend, AiTenantConfiguration tenantConfiguration)
    {
        if (tenantConfiguration.ApiKeys.TryGetValue(backend, out var tenantApiKey)
            && !string.IsNullOrWhiteSpace(tenantApiKey))
        {
            return tenantApiKey;
        }

        return null;
    }

    private AiTenantConfiguration ParseTenantConfiguration(TenantIntegrationSettingsDto integrationSettings)
    {
        var apiKeySettings = integrationSettings.ApiKeys ?? new Dictionary<string, string>();
        var externalServices = integrationSettings.ExternalServices ?? new Dictionary<string, object?>();

        var apiKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        const string backendKeyPrefix = "ai:backend:";
        foreach (var apiKeySetting in apiKeySettings)
        {
            if (apiKeySetting.Key.StartsWith(backendKeyPrefix, StringComparison.OrdinalIgnoreCase)
                && apiKeySetting.Key.Length > backendKeyPrefix.Length
                && !string.IsNullOrWhiteSpace(apiKeySetting.Value))
            {
                apiKeys[apiKeySetting.Key[backendKeyPrefix.Length..]] = apiKeySetting.Value;
            }
        }

        if (!externalServices.TryGetValue("ai", out var rawAiSettings))
        {
            return new AiTenantConfiguration(
                Enabled: true,
                AllowRequestOverride: _aiOptions.AllowTenantOverrides,
                DefaultBackend: null,
                DefaultChatModel: null,
                DefaultGenerateModel: null,
                Backends: new Dictionary<string, AiTenantBackendConfiguration>(StringComparer.OrdinalIgnoreCase),
                ApiKeys: apiKeys,
                Moderation: new AiModerationConfiguration(false, [], null, null),
                History: new AiHistoryConfiguration(true));
        }

        var aiSettingsElement = AiContractJson.ToJsonElement(rawAiSettings);
        if (!aiSettingsElement.HasValue || aiSettingsElement.Value.ValueKind != JsonValueKind.Object)
        {
            return new AiTenantConfiguration(
                Enabled: true,
                AllowRequestOverride: _aiOptions.AllowTenantOverrides,
                DefaultBackend: null,
                DefaultChatModel: null,
                DefaultGenerateModel: null,
                Backends: new Dictionary<string, AiTenantBackendConfiguration>(StringComparer.OrdinalIgnoreCase),
                ApiKeys: apiKeys,
                Moderation: new AiModerationConfiguration(false, [], null, null),
                History: new AiHistoryConfiguration(true));
        }

        var aiElement = aiSettingsElement.Value;
        var backends = new Dictionary<string, AiTenantBackendConfiguration>(StringComparer.OrdinalIgnoreCase);

        if (aiElement.TryGetProperty("backends", out var backendSettingsElement)
            && backendSettingsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var backendProperty in backendSettingsElement.EnumerateObject())
            {
                if (backendProperty.Value.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var enabled = !AiJsonHelpers.TryGetBoolean(backendProperty.Value, "enabled", out var configuredEnabled) || configuredEnabled;
                AiJsonHelpers.TryGetString(backendProperty.Value, "defaultModel", out var defaultModel);
                backends[backendProperty.Name] = new AiTenantBackendConfiguration(enabled, defaultModel);
            }
        }

        var tenantEnabled = !AiJsonHelpers.TryGetBoolean(aiElement, "enabled", out var configuredTenantEnabled) || configuredTenantEnabled;
        var allowRequestOverride = AiJsonHelpers.TryGetBoolean(aiElement, "allowRequestOverride", out var configuredAllowOverride)
            ? configuredAllowOverride
            : _aiOptions.AllowTenantOverrides;

        AiJsonHelpers.TryGetString(aiElement, "defaultBackend", out var defaultBackend);
        AiJsonHelpers.TryGetString(aiElement, "defaultChatModel", out var defaultChatModel);
        AiJsonHelpers.TryGetString(aiElement, "defaultGenerateModel", out var defaultGenerateModel);

        var moderationSettings = aiElement.TryGetProperty("moderation", out var moderationElement)
            && moderationElement.ValueKind == JsonValueKind.Object
            ? moderationElement
            : default;
        var historySettings = aiElement.TryGetProperty("history", out var historyElement)
            && historyElement.ValueKind == JsonValueKind.Object
            ? historyElement
            : default;

        var moderationEnabled = moderationSettings.ValueKind == JsonValueKind.Object
            && AiJsonHelpers.TryGetBoolean(moderationSettings, "enabled", out var configuredModerationEnabled)
            && configuredModerationEnabled;
        var blockedTerms = moderationSettings.ValueKind == JsonValueKind.Object
            ? AiJsonHelpers.TryGetStringArray(moderationSettings, "blockedTerms")
            : [];
        var maxPromptCharacters = moderationSettings.ValueKind == JsonValueKind.Object
            ? AiJsonHelpers.TryGetInt(moderationSettings, "maxPromptCharacters")
            : null;
        var maxResponseCharacters = moderationSettings.ValueKind == JsonValueKind.Object
            ? AiJsonHelpers.TryGetInt(moderationSettings, "maxResponseCharacters")
            : null;
        var historyEnabled = historySettings.ValueKind != JsonValueKind.Object
            || !AiJsonHelpers.TryGetBoolean(historySettings, "enabled", out var configuredHistoryEnabled)
            || configuredHistoryEnabled;

        return new AiTenantConfiguration(
            tenantEnabled,
            allowRequestOverride,
            defaultBackend,
            defaultChatModel,
            defaultGenerateModel,
            backends,
            apiKeys,
            new AiModerationConfiguration(moderationEnabled, blockedTerms, maxPromptCharacters, maxResponseCharacters),
            new AiHistoryConfiguration(historyEnabled));
    }
}
