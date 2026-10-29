using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameGuild;
using Microsoft.Extensions.Logging;

namespace GameGuild.AI;

internal sealed class OpenAiCompatibleProtocolClient(
    IHttpClientFactory httpClientFactory,
    ILogger<OpenAiCompatibleProtocolClient> logger)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<Result<AiAdapterExecutionResult>> CompleteAsync(
        AiResolvedRequest request,
        AiAdapterDescriptor descriptor,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            string responseBody = string.Empty;

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                using var httpRequest = CreateRequest(request, CreatePayload(request, stream: false));
                using var response = await client.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
                responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    break;
                }

                if (attempt < 3 && IsTransient(response.StatusCode))
                {
                    logger.LogWarning(
                        "Transient AI adapter failure for backend {Backend} using {Adapter}. Attempt {Attempt}/3 returned {StatusCode}",
                        request.Backend,
                        descriptor.Key,
                        attempt,
                        response.StatusCode);
                    await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken).ConfigureAwait(false);
                    continue;
                }

                return Result.Failure<AiAdapterExecutionResult>(
                    AiAdapterErrorMapper.Map(descriptor.Key, response.StatusCode));
            }

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return Failure(descriptor, "EmptyResponse", "The AI backend returned an empty HTTP response.");
            }

            return ParseCompletion(request, descriptor, responseBody);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Failure(descriptor, "Timeout", "The AI backend did not respond in time.");
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "AI adapter {Adapter} returned malformed JSON for backend {Backend}",
                descriptor.Key,
                request.Backend);
            return Failure(descriptor, "MalformedResponse", "The AI backend returned malformed JSON.");
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "AI adapter {Adapter} request failed for tenant {TenantId} and backend {Backend}",
                descriptor.Key,
                request.TenantId,
                request.Backend);
            return Failure(descriptor, "RequestFailed", "Failed to execute the AI backend request.");
        }
    }

    public async Task<Result<AiAdapterExecutionResult>> CompleteStreamingAsync(
        AiResolvedRequest request,
        AiAdapterDescriptor descriptor,
        Func<string, CancellationToken, ValueTask> onDelta,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(onDelta);
        try
        {
            using var httpRequest = CreateRequest(request, CreatePayload(request, stream: true), "text/event-stream");
            var client = httpClientFactory.CreateClient();
            using var response = await client
                .SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure<AiAdapterExecutionResult>(
                    AiAdapterErrorMapper.Map(descriptor.Key, response.StatusCode));
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new StreamReader(stream);
            var text = new StringBuilder();
            var model = request.Model;
            string? finishReason = null;
            int? inputTokens = null;
            int? outputTokens = null;
            int? totalTokens = null;
            while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
            {
                if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var data = line[5..].Trim();
                if (data.Length == 0 || data == "[DONE]")
                {
                    continue;
                }

                using var document = JsonDocument.Parse(data);
                var root = document.RootElement;
                if (root.TryGetProperty("model", out var modelElement) && modelElement.ValueKind == JsonValueKind.String)
                {
                    model = modelElement.GetString() ?? model;
                }

                if (root.TryGetProperty("usage", out var usageElement) && usageElement.ValueKind == JsonValueKind.Object)
                {
                    inputTokens = AiJsonHelpers.TryGetInt(usageElement, "prompt_tokens");
                    outputTokens = AiJsonHelpers.TryGetInt(usageElement, "completion_tokens");
                    totalTokens = AiJsonHelpers.TryGetInt(usageElement, "total_tokens");
                }

                if (!root.TryGetProperty("choices", out var choices) ||
                    choices.ValueKind != JsonValueKind.Array ||
                    choices.GetArrayLength() == 0)
                {
                    continue;
                }

                var choice = choices[0];
                if (choice.TryGetProperty("finish_reason", out var finishElement) && finishElement.ValueKind == JsonValueKind.String)
                {
                    finishReason = finishElement.GetString();
                }

                if (!choice.TryGetProperty("delta", out var deltaElement) ||
                    !deltaElement.TryGetProperty("content", out var contentElement))
                {
                    continue;
                }

                var delta = ExtractContent(contentElement);
                if (string.IsNullOrEmpty(delta))
                {
                    continue;
                }

                text.Append(delta);
                await onDelta(delta, cancellationToken).ConfigureAwait(false);
            }

            return text.Length == 0
                ? Failure(descriptor, "EmptyResponse", "The AI backend returned an empty response.")
                : Result.Success(new AiAdapterExecutionResult(
                    model,
                    text.ToString(),
                    finishReason,
                    inputTokens,
                    outputTokens,
                    totalTokens));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Failure(descriptor, "Timeout", "The AI backend did not respond in time.");
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "AI adapter {Adapter} returned malformed streaming JSON for backend {Backend}",
                descriptor.Key,
                request.Backend);
            return Failure(descriptor, "MalformedResponse", "The AI backend returned malformed streaming JSON.");
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "AI adapter {Adapter} streaming request failed for tenant {TenantId} and backend {Backend}",
                descriptor.Key,
                request.TenantId,
                request.Backend);
            return Failure(descriptor, "RequestFailed", "Failed to execute the AI backend streaming request.");
        }
    }

    private static object CreatePayload(AiResolvedRequest request, bool stream)
    {
        var messages = new List<object>();
        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            messages.Add(new { role = "system", content = request.SystemPrompt });
        }

        messages.AddRange(request.Messages.Select(static message => new
        {
            role = message.Role.ToLowerInvariant(),
            content = message.Content,
        }));

        return new
        {
            model = request.Model,
            messages,
            temperature = request.Temperature,
            max_tokens = request.MaxTokens,
            stream,
            stream_options = stream ? new { include_usage = true } : null,
        };
    }

    private static HttpRequestMessage CreateRequest(
        AiResolvedRequest request,
        object payload,
        string accept = "application/json")
    {
        var baseUrl = request.BaseUrl.TrimEnd('/');
        var endpoint = baseUrl.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
            ? $"{baseUrl}/chat/completions"
            : $"{baseUrl}/v1/chat/completions";
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        if (!string.IsNullOrWhiteSpace(request.ApiKey))
        {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.ApiKey);
        }

        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(payload, SerializerOptions),
            Encoding.UTF8,
            "application/json");
        return httpRequest;
    }

    private static Result<AiAdapterExecutionResult> ParseCompletion(
        AiResolvedRequest request,
        AiAdapterDescriptor descriptor,
        string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;
        if (!root.TryGetProperty("choices", out var choices) ||
            choices.ValueKind != JsonValueKind.Array ||
            choices.GetArrayLength() == 0)
        {
            return Failure(descriptor, "MalformedResponse", "The AI backend returned no completion choices.");
        }

        var choice = choices[0];
        if (!choice.TryGetProperty("message", out var message))
        {
            return Failure(descriptor, "MalformedResponse", "The AI backend returned a choice without a message.");
        }

        var text = message.TryGetProperty("content", out var contentElement)
            ? ExtractContent(contentElement)
            : null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return Failure(descriptor, "EmptyResponse", "The AI backend returned an empty response.");
        }

        var finishReason = choice.TryGetProperty("finish_reason", out var finishReasonElement)
            ? finishReasonElement.GetString()
            : null;
        var model = root.TryGetProperty("model", out var modelElement)
            ? modelElement.GetString() ?? request.Model
            : request.Model;
        var usage = root.TryGetProperty("usage", out var usageElement) ? usageElement : default;
        return Result.Success(new AiAdapterExecutionResult(
            model,
            text,
            finishReason,
            AiJsonHelpers.TryGetInt(usage, "prompt_tokens"),
            AiJsonHelpers.TryGetInt(usage, "completion_tokens"),
            AiJsonHelpers.TryGetInt(usage, "total_tokens")));
    }

    private static string? ExtractContent(JsonElement contentElement) => contentElement.ValueKind switch
    {
        JsonValueKind.String => contentElement.GetString(),
        JsonValueKind.Array => AiJsonHelpers.ExtractTextFromParts(contentElement),
        _ => null,
    };

    private static Result<AiAdapterExecutionResult> Failure(
        AiAdapterDescriptor descriptor,
        string suffix,
        string message) =>
        Result.Failure<AiAdapterExecutionResult>(
            Error.Failure($"AI.{descriptor.Key}{suffix}", message));

    private static bool IsTransient(System.Net.HttpStatusCode statusCode) =>
        statusCode == System.Net.HttpStatusCode.TooManyRequests || (int)statusCode >= 500;
}
