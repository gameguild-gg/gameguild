namespace GameGuild.Modules.Workflow.Automation;

/// <summary>
/// Represents a workflow trigger that starts an automation.
/// </summary>
public sealed class WorkflowTrigger
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
    public Dictionary<string, object> Configuration { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Represents an action to be executed in a workflow.
/// </summary>
public sealed class WorkflowAction
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public int Order { get; set; }
    public string? ConditionExpression { get; set; }
}

/// <summary>
/// Represents a complete workflow definition.
/// </summary>
public sealed class WorkflowDefinition
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public WorkflowTrigger Trigger { get; set; } = null!;
    public List<WorkflowAction> Actions { get; set; } = new();
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastExecutedAt { get; set; }
    public int ExecutionCount { get; set; }
}

/// <summary>
/// Represents a workflow execution instance.
/// </summary>
public sealed class WorkflowExecution
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public WorkflowExecutionStatus Status { get; set; }
    public Dictionary<string, object> TriggerData { get; set; } = new();
    public List<ActionExecutionResult> Results { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Status of a workflow execution.
/// </summary>
public enum WorkflowExecutionStatus
{
    Running,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Result of an action execution.
/// </summary>
public sealed class ActionExecutionResult
{
    public Guid ActionId { get; set; }
    public required string ActionName { get; set; }
    public bool Success { get; set; }
    public Dictionary<string, object> Output { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public DateTime ExecutedAt { get; set; }
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Represents a webhook configuration for external integrations.
/// </summary>
public sealed class WebhookConfiguration
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Url { get; set; }
    public required string Method { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? SigningSecret { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Represents an incoming webhook event.
/// </summary>
public sealed class WebhookEvent
{
    public Guid Id { get; set; }
    public Guid ConfigurationId { get; set; }
    public required string EventType { get; set; }
    public Dictionary<string, object> Payload { get; set; } = new();
    public bool IsProcessed { get; set; }
    public bool SignatureVerified { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

/// <summary>
/// Service interface for workflow automation operations.
/// </summary>
public interface IWorkflowAutomationService
{
    /// <summary>
    /// Creates a new workflow definition.
    /// </summary>
    Task<WorkflowDefinition> CreateWorkflowAsync(
        string name,
        string description,
        WorkflowTrigger trigger,
        List<WorkflowAction> actions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing workflow definition.
    /// </summary>
    Task<WorkflowDefinition> UpdateWorkflowAsync(
        Guid workflowId,
        string? name = null,
        string? description = null,
        WorkflowTrigger? trigger = null,
        List<WorkflowAction>? actions = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a workflow definition.
    /// </summary>
    Task DeleteWorkflowAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables a workflow.
    /// </summary>
    Task EnableWorkflowAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables a workflow.
    /// </summary>
    Task DisableWorkflowAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a workflow manually with provided trigger data.
    /// </summary>
    Task<WorkflowExecution> ExecuteWorkflowAsync(
        Guid workflowId,
        Dictionary<string, object> triggerData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets execution history for a workflow.
    /// </summary>
    Task<IReadOnlyList<WorkflowExecution>> GetExecutionHistoryAsync(
        Guid workflowId,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a webhook for external integrations.
    /// </summary>
    Task<WebhookConfiguration> RegisterWebhookAsync(
        string name,
        string url,
        string method,
        Dictionary<string, string> headers,
        string? signingSecret = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes an incoming webhook event.
    /// </summary>
    Task<WebhookEvent> ProcessWebhookAsync(
        Guid configurationId,
        string eventType,
        Dictionary<string, object> payload,
        string? signature = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a webhook signature for security.
    /// </summary>
    Task<bool> VerifyWebhookSignatureAsync(
        Guid configurationId,
        string payload,
        string signature,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries a failed workflow execution.
    /// </summary>
    Task<WorkflowExecution> RetryExecutionAsync(
        Guid executionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a running workflow execution.
    /// </summary>
    Task CancelExecutionAsync(
        Guid executionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all workflows matching criteria.
    /// </summary>
    Task<IReadOnlyList<WorkflowDefinition>> GetWorkflowsAsync(
        bool? isEnabled = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of workflow automation service with Zapier-style integrations.
/// </summary>
public sealed class WorkflowAutomationService : IWorkflowAutomationService
{
    private readonly ILogger<WorkflowAutomationService> _logger;
    private readonly Dictionary<Guid, WorkflowDefinition> _workflows = new();
    private readonly Dictionary<Guid, WebhookConfiguration> _webhooks = new();
    private readonly Dictionary<Guid, WorkflowExecution> _executions = new();

    public WorkflowAutomationService(ILogger<WorkflowAutomationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<WorkflowDefinition> CreateWorkflowAsync(
        string name,
        string description,
        WorkflowTrigger trigger,
        List<WorkflowAction> actions,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating workflow: {Name}", name);

        var workflow = new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Trigger = trigger,
            Actions = actions,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow
        };

        _workflows[workflow.Id] = workflow;
        return Task.FromResult(workflow);
    }

    public Task<WorkflowDefinition> UpdateWorkflowAsync(
        Guid workflowId,
        string? name = null,
        string? description = null,
        WorkflowTrigger? trigger = null,
        List<WorkflowAction>? actions = null,
        CancellationToken cancellationToken = default)
    {
        if (!_workflows.TryGetValue(workflowId, out var workflow))
        {
            throw new InvalidOperationException($"Workflow {workflowId} not found");
        }

        if (name != null) workflow.Name = name;
        if (description != null) workflow.Description = description;
        if (trigger != null) workflow.Trigger = trigger;
        if (actions != null) workflow.Actions = actions;

        _logger.LogInformation("Updated workflow: {WorkflowId}", workflowId);
        return Task.FromResult(workflow);
    }

    public Task DeleteWorkflowAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default)
    {
        _workflows.Remove(workflowId);
        _logger.LogInformation("Deleted workflow: {WorkflowId}", workflowId);
        return Task.CompletedTask;
    }

    public Task EnableWorkflowAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default)
    {
        if (_workflows.TryGetValue(workflowId, out var workflow))
        {
            workflow.IsEnabled = true;
            _logger.LogInformation("Enabled workflow: {WorkflowId}", workflowId);
        }

        return Task.CompletedTask;
    }

    public Task DisableWorkflowAsync(
        Guid workflowId,
        CancellationToken cancellationToken = default)
    {
        if (_workflows.TryGetValue(workflowId, out var workflow))
        {
            workflow.IsEnabled = false;
            _logger.LogInformation("Disabled workflow: {WorkflowId}", workflowId);
        }

        return Task.CompletedTask;
    }

    public async Task<WorkflowExecution> ExecuteWorkflowAsync(
        Guid workflowId,
        Dictionary<string, object> triggerData,
        CancellationToken cancellationToken = default)
    {
        if (!_workflows.TryGetValue(workflowId, out var workflow))
        {
            throw new InvalidOperationException($"Workflow {workflowId} not found");
        }

        _logger.LogInformation("Executing workflow: {WorkflowId}", workflowId);

        var execution = new WorkflowExecution
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflowId,
            Status = WorkflowExecutionStatus.Running,
            TriggerData = triggerData,
            StartedAt = DateTime.UtcNow
        };

        _executions[execution.Id] = execution;

        try
        {
            foreach (var action in workflow.Actions.OrderBy(a => a.Order))
            {
                if (!string.IsNullOrEmpty(action.ConditionExpression))
                {
                    var conditionMet = EvaluateCondition(action.ConditionExpression, triggerData);
                    if (!conditionMet)
                    {
                        _logger.LogInformation("Skipping action {ActionName} - condition not met", action.Name);
                        continue;
                    }
                }

                var result = await ExecuteActionAsync(action, triggerData, cancellationToken);
                execution.Results.Add(result);

                if (!result.Success)
                {
                    throw new InvalidOperationException($"Action {action.Name} failed: {result.ErrorMessage}");
                }
            }

            execution.Status = WorkflowExecutionStatus.Completed;
            execution.CompletedAt = DateTime.UtcNow;

            workflow.LastExecutedAt = DateTime.UtcNow;
            workflow.ExecutionCount++;

            _logger.LogInformation("Workflow execution completed: {ExecutionId}", execution.Id);
        }
        catch (Exception ex)
        {
            execution.Status = WorkflowExecutionStatus.Failed;
            execution.ErrorMessage = ex.Message;
            execution.CompletedAt = DateTime.UtcNow;

            _logger.LogError(ex, "Workflow execution failed: {ExecutionId}", execution.Id);
        }

        return execution;
    }

    public Task<IReadOnlyList<WorkflowExecution>> GetExecutionHistoryAsync(
        Guid workflowId,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var history = _executions.Values
            .Where(e => e.WorkflowId == workflowId)
            .OrderByDescending(e => e.StartedAt)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<WorkflowExecution>>(history);
    }

    public Task<WebhookConfiguration> RegisterWebhookAsync(
        string name,
        string url,
        string method,
        Dictionary<string, string> headers,
        string? signingSecret = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Registering webhook: {Name} -> {Url}", name, url);

        var webhook = new WebhookConfiguration
        {
            Id = Guid.NewGuid(),
            Name = name,
            Url = url,
            Method = method,
            Headers = headers,
            SigningSecret = signingSecret,
            IsActive = true
        };

        _webhooks[webhook.Id] = webhook;
        return Task.FromResult(webhook);
    }

    public async Task<WebhookEvent> ProcessWebhookAsync(
        Guid configurationId,
        string eventType,
        Dictionary<string, object> payload,
        string? signature = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing webhook event: {EventType}", eventType);

        var webhookEvent = new WebhookEvent
        {
            Id = Guid.NewGuid(),
            ConfigurationId = configurationId,
            EventType = eventType,
            Payload = payload,
            ReceivedAt = DateTime.UtcNow,
            SignatureVerified = false
        };

        if (signature != null)
        {
            var payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);
            webhookEvent.SignatureVerified = await VerifyWebhookSignatureAsync(
                configurationId, payloadJson, signature, cancellationToken);
        }

        webhookEvent.IsProcessed = true;
        webhookEvent.ProcessedAt = DateTime.UtcNow;

        return webhookEvent;
    }

    public Task<bool> VerifyWebhookSignatureAsync(
        Guid configurationId,
        string payload,
        string signature,
        CancellationToken cancellationToken = default)
    {
        if (!_webhooks.TryGetValue(configurationId, out var webhook))
        {
            return Task.FromResult(false);
        }

        if (string.IsNullOrEmpty(webhook.SigningSecret))
        {
            return Task.FromResult(true);
        }

        // Simulate HMAC-SHA256 signature verification
        var expectedSignature = ComputeHmacSha256(payload, webhook.SigningSecret);
        var isValid = signature.Equals(expectedSignature, StringComparison.OrdinalIgnoreCase);

        _logger.LogInformation("Webhook signature verification: {IsValid}", isValid);
        return Task.FromResult(isValid);
    }

    public async Task<WorkflowExecution> RetryExecutionAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        if (!_executions.TryGetValue(executionId, out var originalExecution))
        {
            throw new InvalidOperationException($"Execution {executionId} not found");
        }

        _logger.LogInformation("Retrying workflow execution: {ExecutionId}", executionId);

        return await ExecuteWorkflowAsync(
            originalExecution.WorkflowId,
            originalExecution.TriggerData,
            cancellationToken);
    }

    public Task CancelExecutionAsync(
        Guid executionId,
        CancellationToken cancellationToken = default)
    {
        if (_executions.TryGetValue(executionId, out var execution))
        {
            execution.Status = WorkflowExecutionStatus.Cancelled;
            execution.CompletedAt = DateTime.UtcNow;
            _logger.LogInformation("Cancelled workflow execution: {ExecutionId}", executionId);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<WorkflowDefinition>> GetWorkflowsAsync(
        bool? isEnabled = null,
        CancellationToken cancellationToken = default)
    {
        var query = _workflows.Values.AsEnumerable();

        if (isEnabled.HasValue)
        {
            query = query.Where(w => w.IsEnabled == isEnabled.Value);
        }

        var workflows = query.OrderBy(w => w.Name).ToList();
        return Task.FromResult<IReadOnlyList<WorkflowDefinition>>(workflows);
    }

    private async Task<ActionExecutionResult> ExecuteActionAsync(
        WorkflowAction action,
        Dictionary<string, object> context,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Executing action: {ActionName} ({ActionType})", action.Name, action.Type);

            // Simulate action execution
            await Task.Delay(100, cancellationToken);

            var output = new Dictionary<string, object>
            {
                ["status"] = "success",
                ["timestamp"] = DateTime.UtcNow,
                ["actionType"] = action.Type
            };

            return new ActionExecutionResult
            {
                ActionId = action.Id,
                ActionName = action.Name,
                Success = true,
                Output = output,
                ExecutedAt = startTime,
                Duration = DateTime.UtcNow - startTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Action execution failed: {ActionName}", action.Name);

            return new ActionExecutionResult
            {
                ActionId = action.Id,
                ActionName = action.Name,
                Success = false,
                ErrorMessage = ex.Message,
                ExecutedAt = startTime,
                Duration = DateTime.UtcNow - startTime
            };
        }
    }

    private bool EvaluateCondition(string expression, Dictionary<string, object> context)
    {
        // Simplified condition evaluation
        // In production, use a proper expression evaluator
        return true;
    }

    private string ComputeHmacSha256(string payload, string secret)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(
            System.Text.Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }
}
