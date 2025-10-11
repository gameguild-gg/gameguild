using GameGuild.CQRS;
using GameGuild.Modules.Common.Configuration;
using GameGuild.Modules.Tenants;

namespace GameGuild.Modules.Tenants;

/// <summary>
/// Handler for registering a new tenant webhook.
/// </summary>
public class RegisterTenantWebhookHandler : IRequestHandler<RegisterTenantWebhookCommand, Result<TenantWebhook>>
{
    private readonly ITenantWebhookRepository _repository;

    public RegisterTenantWebhookHandler(ITenantWebhookRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<TenantWebhook>> Handle(RegisterTenantWebhookCommand request, CancellationToken cancellationToken)
    {
        var webhook = new TenantWebhook
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Url = request.Url,
            EventType = request.EventType,
            Secret = request.Secret ?? Guid.NewGuid().ToString("N"),
            IsActive = true,
            RetryCount = request.RetryCount,
            TimeoutSeconds = request.TimeoutSeconds,
            Headers = request.Headers,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _repository.CreateAsync(webhook, cancellationToken);
        return Result<TenantWebhook>.Success(result);
    }
}

/// <summary>
/// Handler for updating a tenant webhook.
/// </summary>
public class UpdateTenantWebhookHandler : IRequestHandler<UpdateTenantWebhookCommand, TenantWebhook>
{
    private readonly ITenantWebhookRepository _repository;

    public UpdateTenantWebhookHandler(ITenantWebhookRepository repository)
    {
        _repository = repository;
    }

    public async Task<TenantWebhook> Handle(UpdateTenantWebhookCommand request, CancellationToken cancellationToken)
    {
        var webhook = await _repository.GetByIdAsync(request.WebhookId, cancellationToken);
        if (webhook == null)
            throw new InvalidOperationException($"Webhook {request.WebhookId} not found");

        if (request.Name != null)
            webhook.Name = request.Name;

        if (request.Url != null)
            webhook.Url = request.Url;

        if (request.EventTypes != null)
            webhook.EventTypes = request.EventTypes.Select(e => e.ToString()).ToArray();

        if (request.Secret != null)
            webhook.Secret = request.Secret;

        if (request.IsActive.HasValue)
            webhook.IsActive = request.IsActive.Value;

        if (request.RetryPolicy.HasValue)
            webhook.RetryPolicy = request.RetryPolicy.Value;

        if (request.MaxRetries.HasValue)
            webhook.MaxRetries = request.MaxRetries.Value;

        if (request.TimeoutSeconds.HasValue)
            webhook.TimeoutSeconds = request.TimeoutSeconds.Value;

        if (request.Headers != null)
            webhook.Headers = request.Headers;

        webhook.ValidateWebhook();

        return await _repository.UpdateAsync(webhook, cancellationToken);
    }
}

/// <summary>
/// Handler for deleting a tenant webhook.
/// </summary>
public class DeleteTenantWebhookHandler : IRequestHandler<DeleteTenantWebhookCommand, bool>
{
    private readonly ITenantWebhookRepository _repository;

    public DeleteTenantWebhookHandler(ITenantWebhookRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(DeleteTenantWebhookCommand request, CancellationToken cancellationToken)
    {
        return await _repository.DeleteAsync(request.WebhookId, cancellationToken);
    }
}

/// <summary>
/// Handler for testing a tenant webhook.
/// </summary>
public class TestTenantWebhookHandler : IRequestHandler<TestTenantWebhookCommand, TenantWebhookDelivery>
{
    private readonly ITenantWebhookRepository _repository;
    private readonly ITenantWebhookService _webhookService;

    public TestTenantWebhookHandler(ITenantWebhookRepository repository, ITenantWebhookService webhookService)
    {
        _repository = repository;
        _webhookService = webhookService;
    }

    public async Task<TenantWebhookDelivery> Handle(TestTenantWebhookCommand request, CancellationToken cancellationToken)
    {
        var webhook = await _repository.GetByIdAsync(request.WebhookId, cancellationToken);
        if (webhook == null)
            throw new InvalidOperationException($"Webhook {request.WebhookId} not found");

        var testPayload = request.TestPayload ?? new
        {
            test = true,
            message = "This is a test webhook delivery",
            timestamp = DateTime.UtcNow
        };

        var payloadJson = System.Text.Json.JsonSerializer.Serialize(testPayload);

        return await _webhookService.DeliverWebhookAsync(webhook, request.EventType, payloadJson, cancellationToken);
    }
}

/// <summary>
/// Handler for retrying a failed webhook delivery.
/// </summary>
public class RetryFailedWebhookHandler : IRequestHandler<RetryFailedWebhookCommand, Result<TenantWebhookDelivery>>
{
    private readonly ITenantWebhookService _webhookService;

    public RetryFailedWebhookHandler(ITenantWebhookService webhookService)
    {
        _webhookService = webhookService;
    }

    public async Task<Result<TenantWebhookDelivery>> Handle(RetryFailedWebhookCommand request, CancellationToken cancellationToken)
    {
        var delivery = await _webhookService.RetryFailedDeliveryAsync(request.DeliveryId, cancellationToken);
        return Result<TenantWebhookDelivery>.Success(delivery);
    }
}

/// <summary>
/// Handler for enabling a tenant webhook.
/// </summary>
public class EnableTenantWebhookHandler : IRequestHandler<EnableTenantWebhookCommand, Result<TenantWebhook>>
{
    private readonly ITenantWebhookRepository _repository;

    public EnableTenantWebhookHandler(ITenantWebhookRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<TenantWebhook>> Handle(EnableTenantWebhookCommand request, CancellationToken cancellationToken)
    {
        var webhook = await _repository.GetByIdAsync(request.WebhookId, cancellationToken);
        if (webhook == null)
            return Result<TenantWebhook>.Failure($"Webhook {request.WebhookId} not found");

        webhook.IsActive = true;
        var updatedWebhook = await _repository.UpdateAsync(webhook, cancellationToken);
        return Result<TenantWebhook>.Success(updatedWebhook);
    }
}

/// <summary>
/// Handler for disabling a tenant webhook.
/// </summary>
public class DisableTenantWebhookHandler : IRequestHandler<DisableTenantWebhookCommand, Result<TenantWebhook>>
{
    private readonly ITenantWebhookRepository _repository;

    public DisableTenantWebhookHandler(ITenantWebhookRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<TenantWebhook>> Handle(DisableTenantWebhookCommand request, CancellationToken cancellationToken)
    {
        var webhook = await _repository.GetByIdAsync(request.WebhookId, cancellationToken);
        if (webhook == null)
            return Result<TenantWebhook>.Failure($"Webhook {request.WebhookId} not found");

        webhook.IsActive = false;
        var updatedWebhook = await _repository.UpdateAsync(webhook, cancellationToken);
        return Result<TenantWebhook>.Success(updatedWebhook);
    }
}

/// <summary>
/// Handler for getting tenant webhooks.
/// </summary>
public class GetTenantWebhooksHandler : IRequestHandler<GetTenantWebhooksQuery, Result<IEnumerable<TenantWebhook>>>
{
    private readonly ITenantWebhookRepository _repository;

    public GetTenantWebhooksHandler(ITenantWebhookRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IEnumerable<TenantWebhook>>> Handle(GetTenantWebhooksQuery request, CancellationToken cancellationToken)
    {
        var webhooks = await _repository.GetByTenantIdAsync(request.TenantId, request.IsActive, cancellationToken);
        return Result<IEnumerable<TenantWebhook>>.Success(webhooks);
    }
}

/// <summary>
/// Handler for getting webhook deliveries.
/// </summary>
public class GetWebhookDeliveriesHandler : IRequestHandler<GetWebhookDeliveriesQuery, Result<PagedResult<TenantWebhookDelivery>>>
{
    private readonly ITenantWebhookRepository _repository;

    public GetWebhookDeliveriesHandler(ITenantWebhookRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedResult<TenantWebhookDelivery>>> Handle(GetWebhookDeliveriesQuery request, CancellationToken cancellationToken)
    {
        var (deliveries, totalCount) = await _repository.GetDeliveriesAsync(
            request.WebhookId,
            request.Success,
            request.StartDate,
            request.EndDate,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var pagedResult = new PagedResult<TenantWebhookDelivery>
        {
            Items = deliveries,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        return Result<PagedResult<TenantWebhookDelivery>>.Success(pagedResult);
    }
}

/// <summary>
/// Handler for getting failed webhook deliveries.
/// </summary>
public class GetFailedWebhookDeliveriesHandler : IRequestHandler<GetFailedWebhookDeliveriesQuery, Result<PagedResult<TenantWebhookDelivery>>>
{
    private readonly ITenantWebhookRepository _repository;

    public GetFailedWebhookDeliveriesHandler(ITenantWebhookRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedResult<TenantWebhookDelivery>>> Handle(GetFailedWebhookDeliveriesQuery request, CancellationToken cancellationToken)
    {
        var (deliveries, totalCount) = await _repository.GetFailedDeliveriesAsync(
            request.TenantId,
            request.SinceDate,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var pagedResult = new PagedResult<TenantWebhookDelivery>
        {
            Items = deliveries,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        return Result<PagedResult<TenantWebhookDelivery>>.Success(pagedResult);
    }
}

/// <summary>
/// Paged result wrapper.
/// </summary>
public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
