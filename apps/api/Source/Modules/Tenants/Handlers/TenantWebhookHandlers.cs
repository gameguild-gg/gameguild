using GameGuild.CQRS;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Handler for registering a new tenant webhook.
/// </summary>
public class RegisterTenantWebhookHandler : IRequestHandler<RegisterTenantWebhookCommand, Result<TenantWebhook>> {
    private readonly ITenantWebhookRepository _repository;

    public RegisterTenantWebhookHandler(ITenantWebhookRepository repository) {
        _repository = repository;
    }

    public async Task<Result<TenantWebhook>> Handle(RegisterTenantWebhookCommand request, CancellationToken cancellationToken) {
        var webhook = new TenantWebhook(
            url: request.Url,
            tenantId: request.TenantId,
            eventType: request.EventType,
            secret: request.Secret ?? Guid.NewGuid().ToString("N"),
            retryCount: request.RetryCount,
            timeoutSeconds: request.TimeoutSeconds,
            headers: request.Headers
        );

        var result = await _repository.CreateAsync(webhook, cancellationToken);
        return Result<TenantWebhook>.Success(result);
    }
}

/// <summary>
/// Handler for updating a tenant webhook.
/// </summary>
public class UpdateTenantWebhookHandler : IRequestHandler<UpdateTenantWebhookCommand, Result<TenantWebhook>> {
    private readonly ITenantWebhookRepository _repository;

    public UpdateTenantWebhookHandler(ITenantWebhookRepository repository) {
        _repository = repository;
    }

    public async Task<Result<TenantWebhook>> Handle(UpdateTenantWebhookCommand request, CancellationToken cancellationToken) {
        var webhook = await _repository.GetByIdAsync(request.WebhookId, cancellationToken);
        if (webhook == null) {
            return Result.Failure<TenantWebhook>(Error.NotFound("Webhook", request.WebhookId));
        }

        if (request.Url != null) {
            webhook.UpdateUrl(request.Url);
        }

        if (request.Secret != null) {
            webhook.UpdateSecret(request.Secret);
        }

        if (request.IsActive.HasValue) {
            if (request.IsActive.Value) {
                webhook.Activate();
            }
            else {
                webhook.Deactivate();
            }
        }

        if (request.TimeoutSeconds.HasValue) {
            webhook.UpdateTimeoutSeconds(request.TimeoutSeconds.Value);
        }

        if (request.Headers != null) {
            webhook.UpdateHeaders(request.Headers);
        }

        var updatedWebhook = await _repository.UpdateAsync(webhook, cancellationToken);
        return Result.Success(updatedWebhook);
    }
}

/// <summary>
/// Handler for deleting a tenant webhook.
/// </summary>
public class DeleteTenantWebhookHandler : IRequestHandler<DeleteTenantWebhookCommand, Result<bool>> {
    private readonly ITenantWebhookRepository _repository;

    public DeleteTenantWebhookHandler(ITenantWebhookRepository repository) {
        _repository = repository;
    }

    public async Task<Result<bool>> Handle(DeleteTenantWebhookCommand request, CancellationToken cancellationToken) {
        var deleted = await _repository.DeleteAsync(request.WebhookId, cancellationToken);
        return Result<bool>.Success(deleted);
    }
}

/// <summary>
/// Handler for testing a tenant webhook.
/// </summary>
public class TestTenantWebhookHandler : IRequestHandler<TestTenantWebhookCommand, Result<TenantWebhookDelivery>> {
    private readonly ITenantWebhookRepository _repository;
    private readonly ITenantWebhookService _webhookService;

    public TestTenantWebhookHandler(ITenantWebhookRepository repository, ITenantWebhookService webhookService) {
        _repository = repository;
        _webhookService = webhookService;
    }

    public async Task<Result<TenantWebhookDelivery>> Handle(TestTenantWebhookCommand request, CancellationToken cancellationToken) {
        var webhook = await _repository.GetByIdAsync(request.WebhookId, cancellationToken);
        if (webhook == null)
            return Result<TenantWebhookDelivery>.Failure($"Webhook {request.WebhookId} not found");

        var testPayload = request.TestPayload ?? new {
            test = true,
            message = "This is a test webhook delivery",
            timestamp = DateTime.UtcNow
        };

        var payloadJson = System.Text.Json.JsonSerializer.Serialize(testPayload);

        // Use a test event type for testing
        var testEventType = TenantWebhookEventType.Test; // Assuming this exists, or use the first supported event type
        var delivery = await _webhookService.DeliverWebhookAsync(webhook, testEventType, payloadJson, cancellationToken);
        return Result<TenantWebhookDelivery>.Success(delivery);
    }
}

/// <summary>
/// Handler for retrying a failed webhook delivery.
/// </summary>
public class RetryFailedWebhookHandler : IRequestHandler<RetryFailedWebhookCommand, Result<TenantWebhookDelivery>> {
    private readonly ITenantWebhookService _webhookService;

    public RetryFailedWebhookHandler(ITenantWebhookService webhookService) {
        _webhookService = webhookService;
    }

    public async Task<Result<TenantWebhookDelivery>> Handle(RetryFailedWebhookCommand request, CancellationToken cancellationToken) {
        var delivery = await _webhookService.RetryFailedDeliveryAsync(request.DeliveryId, cancellationToken);
        return Result<TenantWebhookDelivery>.Success(delivery);
    }
}

/// <summary>
/// Handler for enabling a tenant webhook.
/// </summary>
public class EnableTenantWebhookHandler : IRequestHandler<EnableTenantWebhookCommand, Result<TenantWebhook>> {
    private readonly ITenantWebhookRepository _repository;

    public EnableTenantWebhookHandler(ITenantWebhookRepository repository) {
        _repository = repository;
    }

    public async Task<Result<TenantWebhook>> Handle(EnableTenantWebhookCommand request, CancellationToken cancellationToken) {
        var webhook = await _repository.GetByIdAsync(request.WebhookId, cancellationToken);
        if (webhook == null) {
            return Result.Failure<TenantWebhook>(Error.NotFound("Webhook", request.WebhookId));
        }

        webhook.Activate();
        var updatedWebhook = await _repository.UpdateAsync(webhook, cancellationToken);
        return Result.Success(updatedWebhook);
    }
}

/// <summary>
/// Handler for disabling a tenant webhook.
/// </summary>
public class DisableTenantWebhookHandler : IRequestHandler<DisableTenantWebhookCommand, Result<TenantWebhook>> {
    private readonly ITenantWebhookRepository _repository;

    public DisableTenantWebhookHandler(ITenantWebhookRepository repository) {
        _repository = repository;
    }

    public async Task<Result<TenantWebhook>> Handle(DisableTenantWebhookCommand request, CancellationToken cancellationToken) {
        var webhook = await _repository.GetByIdAsync(request.WebhookId, cancellationToken);
        if (webhook == null) {
            return Result.Failure<TenantWebhook>(Error.NotFound("Webhook", request.WebhookId));
        }

        webhook.Deactivate();
        var updatedWebhook = await _repository.UpdateAsync(webhook, cancellationToken);
        return Result.Success(updatedWebhook);
    }
}

/// <summary>
/// Handler for getting tenant webhooks.
/// </summary>
public class GetTenantWebhooksHandler : IRequestHandler<GetTenantWebhooksQuery, Result<IEnumerable<TenantWebhook>>> {
    private readonly ITenantWebhookRepository _repository;

    public GetTenantWebhooksHandler(ITenantWebhookRepository repository) {
        _repository = repository;
    }

    public async Task<Result<IEnumerable<TenantWebhook>>> Handle(GetTenantWebhooksQuery request, CancellationToken cancellationToken) {
        var webhooks = await _repository.GetByTenantIdAsync(request.TenantId, request.IsActive, cancellationToken);
        return Result<IEnumerable<TenantWebhook>>.Success(webhooks);
    }
}

/// <summary>
/// Handler for getting webhook deliveries.
/// </summary>
public class GetWebhookDeliveriesHandler : IRequestHandler<GetWebhookDeliveriesQuery, Result<PagedResult<TenantWebhookDelivery>>> {
    private readonly ITenantWebhookRepository _repository;

    public GetWebhookDeliveriesHandler(ITenantWebhookRepository repository) {
        _repository = repository;
    }

    public async Task<Result<PagedResult<TenantWebhookDelivery>>> Handle(GetWebhookDeliveriesQuery request, CancellationToken cancellationToken) {
        (IEnumerable<TenantWebhookDelivery> deliveries, int totalCount) = await _repository.GetDeliveriesAsync(
            request.WebhookId,
            request.Success,
            request.StartDate,
            request.EndDate,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var pagedResult = new PagedResult<TenantWebhookDelivery> {
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
public class GetFailedWebhookDeliveriesHandler : IRequestHandler<GetFailedWebhookDeliveriesQuery, Result<PagedResult<TenantWebhookDelivery>>> {
    private readonly ITenantWebhookRepository _repository;

    public GetFailedWebhookDeliveriesHandler(ITenantWebhookRepository repository) {
        _repository = repository;
    }

    public async Task<Result<PagedResult<TenantWebhookDelivery>>> Handle(GetFailedWebhookDeliveriesQuery request, CancellationToken cancellationToken) {
        (IEnumerable<TenantWebhookDelivery> deliveries, int totalCount) = await _repository.GetFailedDeliveriesAsync(
            request.TenantId,
            request.SinceDate,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var pagedResult = new PagedResult<TenantWebhookDelivery> {
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
public class PagedResult<T> {
    public IEnumerable<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
