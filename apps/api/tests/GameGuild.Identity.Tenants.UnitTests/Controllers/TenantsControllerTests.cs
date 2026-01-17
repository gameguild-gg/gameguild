using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Commerce.Payments;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Controllers;

public class TenantsControllerTests
{
    [Fact]
    public async Task CreateTenant_Should_Return_CreatedAtAction()
    {
        var sender = new StubSender();
        var tenantId = Guid.NewGuid();
        sender.Setup<CreateTenantCommand, Guid>(_ => tenantId);

        var controller = new TenantsController(sender);
        var result = await controller.CreateTenant(new CreateTenantRequest("Name", "slug", "admin@example.com"), CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task GetTenants_Should_Clamp_Page_And_Return_Ok()
    {
        var sender = new StubSender();
        var paged = new GameGuild.Models.PagedResult<Tenant>(new List<Tenant>(), 0, 0, 100);
        sender.Setup<GetTenantsPageQuery, GameGuild.Models.PagedResult<Tenant>>(_ => paged);

        var controller = new TenantsController(sender);
        var result = await controller.GetTenants(page: 0, pageSize: 200, status: "active", searchTerm: null, ct: CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPaymentHistory_Should_Return_Ok()
    {
        var sender = new StubSender();
        sender.Setup<GetPaymentHistoryQuery, IEnumerable<PaymentResult>>(_ => Array.Empty<PaymentResult>());

        var controller = new TenantsController(sender);
        var result = await controller.GetPaymentHistory(Guid.NewGuid(), null, null, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ValidateTenant_Should_Return_Ok()
    {
        var sender = new StubSender();
        sender.Setup<ValidateTenantCommand, TenantValidationResponse>(_ => new TenantValidationResponse { IsValid = true });

        var controller = new TenantsController(sender);
        var result = await controller.ValidateTenant(new ValidateTenantRequest("Name", "slug", "admin@example.com"), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Bulk_Endpoints_Should_Return_Expected_Results()
    {
        var sender = new StubSender();
        var controller = new TenantsController(sender);
        var payload = new { };

        (await controller.BulkCreateTenants(payload, CancellationToken.None)).Should().BeOfType<CreatedResult>();
        (await controller.BulkPartialUpdateTenants(payload, CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.BulkFullUpdateTenants(payload, CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.BulkDeleteTenants(payload, CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.BulkActivateTenants(payload, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.BulkDeactivateTenants(payload, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.BulkArchiveTenants(payload, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.BulkUndeleteTenants(payload, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.BulkPurgeTenants(payload, CancellationToken.None)).Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task CheckTenantExists_Should_Return_NotFound_When_Missing()
    {
        var sender = new StubSender();
        sender.Setup<GetTenantByIdQuery, Tenant?>(_ => null);

        var controller = new TenantsController(sender);
        var result = await controller.CheckTenantExistsById(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetTenantById_Should_Return_Ok_When_Found()
    {
        var sender = new StubSender();
        sender.Setup<GetTenantByIdQuery, Tenant?>(_ => new Tenant { Name = "Tenant", Slug = "tenant" });

        var controller = new TenantsController(sender);
        var result = await controller.GetTenantById(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_Endpoints_Should_Return_NoContent()
    {
        var sender = new StubSender();
        sender.Setup<UpdateTenantCommand, Unit>(_ => Unit.Value);
        sender.Setup<ArchiveTenantCommand, ArchiveTenantResponse>(_ => new ArchiveTenantResponse { Success = true });
        sender.Setup<ActivateTenantCommand, ActivateTenantResponse>(_ => new ActivateTenantResponse { Success = true });
        sender.Setup<DeactivateTenantCommand, Unit>(_ => Unit.Value);
        sender.Setup<RecoverTenantCommand, RecoverTenantResponse>(_ => new RecoverTenantResponse { Success = true });
        sender.Setup<DeleteTenantCommand, Unit>(_ => Unit.Value);
        sender.Setup<GetTenantAuditLogQuery, GameGuild.Models.PagedResult<TenantAuditLogEntry>>(_ => new GameGuild.Models.PagedResult<TenantAuditLogEntry>(new List<TenantAuditLogEntry>(), 0, 0, 10));

        var controller = new TenantsController(sender);

        (await controller.PatchTenantById(Guid.NewGuid(), new UpdateTenantRequest("Name", null), CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.UpdateTenantById(Guid.NewGuid(), new UpdateTenantRequest("Name", "Desc"), CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.DeleteTenantById(Guid.NewGuid(), new ArchiveRequest("reason"), CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.ActivateTenant(Guid.NewGuid(), CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.DeactivateTenant(Guid.NewGuid(), CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.ArchiveTenant(Guid.NewGuid(), new ArchiveRequest("reason"), CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.UndeleteTenant(Guid.NewGuid(), new RecoverRequest("reason"), CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.PurgeTenant(Guid.NewGuid(), CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.GetTenantAuditLog(Guid.NewGuid(), null, null, null, null, 0, 500, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
    }

    private sealed class StubSender : ISender
    {
        private readonly Dictionary<Type, Func<object, object?>> _handlers = new();

        public void Setup<TRequest, TResponse>(Func<TRequest, TResponse> handler)
        {
            _handlers[typeof(TRequest)] = request => handler((TRequest)request);
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (_handlers.TryGetValue(request.GetType(), out var handler))
            {
                return Task.FromResult((TResponse)handler(request)!);
            }

            return Task.FromResult(default(TResponse)!);
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            if (_handlers.TryGetValue(request.GetType(), out var handler))
            {
                return Task.FromResult(handler(request));
            }

            return Task.FromResult<object?>(null);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            return Task.CompletedTask;
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStream<TResponse> request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        {
            return Task.CompletedTask;
        }

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
