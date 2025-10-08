using GameGuild.CQRS;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions.Commands;

namespace GameGuild.Modules.Permissions.Handlers;

/// <summary>
/// Handler for evaluating ABAC policies
/// </summary>
public class EvaluateAbacPolicyHandler : IRequestHandler<EvaluateAbacPolicyCommand, AbacEvaluationResult>
{
    private readonly IAbacPolicyService _abacService;
    private readonly ILogger<EvaluateAbacPolicyHandler> _logger;

    public EvaluateAbacPolicyHandler(
        IAbacPolicyService abacService,
        ILogger<EvaluateAbacPolicyHandler> logger)
    {
        _abacService = abacService ?? throw new ArgumentNullException(nameof(abacService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AbacEvaluationResult> Handle(EvaluateAbacPolicyCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Evaluating ABAC policies for User:{UserId}, Resource:{ResourceType}:{ResourceId}",
            request.UserId, request.ResourceType, request.ResourceId);

        var context = new AbacEvaluationContext
        {
            UserId = request.UserId,
            TenantId = request.TenantId,
            ResourceId = request.ResourceId,
            ResourceType = request.ResourceType,
            Permission = request.Permission,
            UserAttributes = request.UserAttributes,
            ResourceAttributes = request.ResourceAttributes,
            ContextAttributes = request.ContextAttributes
        };

        return await _abacService.EvaluatePoliciesAsync(context, cancellationToken);
    }
}

/// <summary>
/// Handler for creating ABAC policies
/// </summary>
public class CreateAbacPolicyHandler : IRequestHandler<CreateAbacPolicyCommand, AbacPolicy>
{
    private readonly IAbacPolicyService _abacService;
    private readonly ILogger<CreateAbacPolicyHandler> _logger;

    public CreateAbacPolicyHandler(
        IAbacPolicyService abacService,
        ILogger<CreateAbacPolicyHandler> logger)
    {
        _abacService = abacService ?? throw new ArgumentNullException(nameof(abacService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AbacPolicy> Handle(CreateAbacPolicyCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Validate expression
        var (isValid, errors) = await _abacService.ValidatePolicyExpressionAsync(
            request.AttributeExpression,
            request.ConditionExpression);

        if (!isValid)
        {
            throw new InvalidOperationException($"Invalid policy expression: {string.Join(", ", errors)}");
        }

        var policy = new AbacPolicy
        {
            Name = request.Name,
            Description = request.Description,
            TenantId = request.TenantId,
            ResourceType = request.ResourceType,
            Permission = request.Permission,
            Effect = request.Effect,
            AttributeExpression = request.AttributeExpression,
            ConditionExpression = request.ConditionExpression,
            Priority = request.Priority,
            IsActive = request.IsActive,
            ExpiresAt = request.ExpiresAt
        };

        return await _abacService.CreatePolicyAsync(policy, cancellationToken);
    }
}

/// <summary>
/// Handler for updating ABAC policies
/// </summary>
public class UpdateAbacPolicyHandler : IRequestHandler<UpdateAbacPolicyCommand, AbacPolicy>
{
    private readonly IAbacPolicyService _abacService;
    private readonly ILogger<UpdateAbacPolicyHandler> _logger;

    public UpdateAbacPolicyHandler(
        IAbacPolicyService abacService,
        ILogger<UpdateAbacPolicyHandler> logger)
    {
        _abacService = abacService ?? throw new ArgumentNullException(nameof(abacService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AbacPolicy> Handle(UpdateAbacPolicyCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var policy = await _abacService.GetPolicyByIdAsync(request.PolicyId, cancellationToken)
            ?? throw new InvalidOperationException($"Policy with ID {request.PolicyId} not found");

        // Update fields if provided
        if (request.Name != null) policy.Name = request.Name;
        if (request.Description != null) policy.Description = request.Description;
        if (request.ResourceType != null) policy.ResourceType = request.ResourceType;
        if (request.Permission.HasValue) policy.Permission = request.Permission.Value;
        if (request.Effect.HasValue) policy.Effect = request.Effect.Value;
        if (request.AttributeExpression != null) policy.AttributeExpression = request.AttributeExpression;
        if (request.ConditionExpression != null) policy.ConditionExpression = request.ConditionExpression;
        if (request.Priority.HasValue) policy.Priority = request.Priority.Value;
        if (request.IsActive.HasValue) policy.IsActive = request.IsActive.Value;
        if (request.ExpiresAt.HasValue) policy.ExpiresAt = request.ExpiresAt;

        // Validate if expression was updated
        if (request.AttributeExpression != null || request.ConditionExpression != null)
        {
            var (isValid, errors) = await _abacService.ValidatePolicyExpressionAsync(
                policy.AttributeExpression,
                policy.ConditionExpression);

            if (!isValid)
            {
                throw new InvalidOperationException($"Invalid policy expression: {string.Join(", ", errors)}");
            }
        }

        return await _abacService.UpdatePolicyAsync(policy, cancellationToken);
    }
}

/// <summary>
/// Handler for deleting ABAC policies
/// </summary>
public class DeleteAbacPolicyHandler : IRequestHandler<DeleteAbacPolicyCommand, bool>
{
    private readonly IAbacPolicyService _abacService;
    private readonly ILogger<DeleteAbacPolicyHandler> _logger;

    public DeleteAbacPolicyHandler(
        IAbacPolicyService abacService,
        ILogger<DeleteAbacPolicyHandler> logger)
    {
        _abacService = abacService ?? throw new ArgumentNullException(nameof(abacService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> Handle(DeleteAbacPolicyCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Deleting ABAC policy {PolicyId}", request.PolicyId);

        return await _abacService.DeletePolicyAsync(request.PolicyId, cancellationToken);
    }
}
