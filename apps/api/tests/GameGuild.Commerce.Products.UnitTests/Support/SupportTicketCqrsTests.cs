using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Products.UnitTests.Support;

public sealed class SupportTicketCqrsTests
{
    [Fact]
    public async Task CreateSupportTicketCommand_ShouldPersistCustomerTicketAndOpeningMessage()
    {
        await using var db = CreateDbContext();
        var handler = new CreateSupportTicketCommandHandler(db);
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var reporterId = Guid.NewGuid();

        var result = await handler.Handle(
            new CreateSupportTicketCommand(
                tenantId,
                customerId,
                "Acme Properties",
                reporterId,
                "Morgan Support",
                "morgan@example.com",
                "Billing sync",
                "The account billing sync is not refreshing.",
                SupportTicketPriority.High,
                "billing"),
            CancellationToken.None);

        result.Id.Should().NotBeEmpty();
        result.TenantId.Should().Be(tenantId);
        result.CustomerId.Should().Be(customerId);
        result.CustomerName.Should().Be("Acme Properties");
        result.Status.Should().Be(SupportTicketStatus.Open);
        result.Priority.Should().Be(SupportTicketPriority.High);
        result.MessageCount.Should().Be(1);

        var stored = await db.Set<SupportTicket>()
            .Include(ticket => ticket.Messages)
            .SingleAsync(CancellationToken.None);

        stored.Subject.Should().Be("Billing sync");
        stored.Messages.Should().ContainSingle(message => message.Body == "The account billing sync is not refreshing.");
    }

    [Fact]
    public async Task SupportTicketWorkflow_ShouldListReplyResolveAndCloseTicket()
    {
        await using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var reporterId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        var createResult = await new CreateSupportTicketCommandHandler(db).Handle(
            new CreateSupportTicketCommand(
                tenantId,
                customerId,
                "Acme Properties",
                reporterId,
                "Morgan Support",
                "morgan@example.com",
                "Portal onboarding",
                "The onboarding checklist is blocked.",
                SupportTicketPriority.Normal,
                "onboarding"),
            CancellationToken.None);

        var listResult = await new GetSupportTicketsQueryHandler(db).Handle(
            new GetSupportTicketsQuery(tenantId, SupportTicketStatus.Open, null, "portal", 0, 10),
            CancellationToken.None);

        listResult.TotalCount.Should().Be(1);
        listResult.Items.Single().Id.Should().Be(createResult.Id);

        var replyResult = await new AddSupportTicketMessageCommandHandler(db).Handle(
            new AddSupportTicketMessageCommand(
                createResult.Id,
                tenantId,
                agentId,
                "Sasha Agent",
                "agent@example.com",
                SupportTicketMessageAuthorType.Agent,
                "I am checking the onboarding state now.",
                true),
            CancellationToken.None);

        replyResult.MessageCount.Should().Be(2);
        replyResult.Status.Should().Be(SupportTicketStatus.InProgress);
        replyResult.AssignedToUserId.Should().Be(agentId);

        var assigned = await new AssignSupportTicketCommandHandler(db).Handle(
            new AssignSupportTicketCommand(createResult.Id, tenantId, agentId, "Sasha Agent"),
            CancellationToken.None);

        assigned.AssignedToName.Should().Be("Sasha Agent");

        var byId = await new GetSupportTicketByIdQueryHandler(db).Handle(
            new GetSupportTicketByIdQuery(createResult.Id, tenantId),
            CancellationToken.None);

        byId.Should().NotBeNull();
        byId!.Id.Should().Be(createResult.Id);

        var hiddenByTenant = await new GetSupportTicketByIdQueryHandler(db).Handle(
            new GetSupportTicketByIdQuery(createResult.Id, Guid.NewGuid()),
            CancellationToken.None);

        hiddenByTenant.Should().BeNull();

        var resolved = await new ResolveSupportTicketCommandHandler(db).Handle(
            new ResolveSupportTicketCommand(createResult.Id, tenantId, agentId, "Sasha Agent", "Reset the onboarding task state."),
            CancellationToken.None);

        resolved.Status.Should().Be(SupportTicketStatus.Resolved);
        resolved.ResolutionSummary.Should().Be("Reset the onboarding task state.");

        var closed = await new CloseSupportTicketCommandHandler(db).Handle(
            new CloseSupportTicketCommand(createResult.Id, tenantId, agentId, "Sasha Agent", "Customer confirmed the fix."),
            CancellationToken.None);

        closed.Status.Should().Be(SupportTicketStatus.Closed);
        closed.ClosedAt.Should().NotBeNull();
        closed.LastMessagePreview.Should().Contain("Customer confirmed");
    }

    [Fact]
    public async Task SupportTicketHandlers_ShouldValidateMissingTicketAndCloseWithoutNotes()
    {
        await using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var agentId = Guid.NewGuid();

        var missing = () => new AssignSupportTicketCommandHandler(db).Handle(
            new AssignSupportTicketCommand(Guid.NewGuid(), tenantId, agentId, "Sasha Agent"),
            CancellationToken.None);

        await missing.Should().ThrowAsync<KeyNotFoundException>();

        var created = await new CreateSupportTicketCommandHandler(db).Handle(
            NewCreateCommand(tenantId, SupportTicketPriority.Low, body: "Needs a simple answer."),
            CancellationToken.None);

        var closed = await new CloseSupportTicketCommandHandler(db).Handle(
            new CloseSupportTicketCommand(created.Id, tenantId, agentId, "Sasha Agent"),
            CancellationToken.None);
        var closedAgain = await new CloseSupportTicketCommandHandler(db).Handle(
            new CloseSupportTicketCommand(created.Id, tenantId, agentId, "Sasha Agent"),
            CancellationToken.None);

        closed.Status.Should().Be(SupportTicketStatus.Closed);
        closedAgain.MessageCount.Should().Be(closed.MessageCount);
    }

    [Fact]
    public void SupportTicketEntity_ShouldValidateRequiredOpenFieldsAndMessageFields()
    {
        var valid = NewCreateCommand(Guid.NewGuid(), SupportTicketPriority.Urgent);
        var openFailures = new Action[]
        {
            () => SupportTicket.Open(Guid.Empty, valid.CustomerId, valid.CustomerName, valid.ReporterUserId, valid.ReporterName, valid.ReporterEmail, valid.Subject, valid.Body, valid.Priority, valid.Category),
            () => SupportTicket.Open(valid.TenantId, Guid.Empty, valid.CustomerName, valid.ReporterUserId, valid.ReporterName, valid.ReporterEmail, valid.Subject, valid.Body, valid.Priority, valid.Category),
            () => SupportTicket.Open(valid.TenantId, valid.CustomerId, valid.CustomerName, Guid.Empty, valid.ReporterName, valid.ReporterEmail, valid.Subject, valid.Body, valid.Priority, valid.Category),
            () => SupportTicket.Open(valid.TenantId, valid.CustomerId, " ", valid.ReporterUserId, valid.ReporterName, valid.ReporterEmail, valid.Subject, valid.Body, valid.Priority, valid.Category),
            () => SupportTicket.Open(valid.TenantId, valid.CustomerId, valid.CustomerName, valid.ReporterUserId, " ", valid.ReporterEmail, valid.Subject, valid.Body, valid.Priority, valid.Category),
            () => SupportTicket.Open(valid.TenantId, valid.CustomerId, valid.CustomerName, valid.ReporterUserId, valid.ReporterName, valid.ReporterEmail, " ", valid.Body, valid.Priority, valid.Category),
            () => SupportTicket.Open(valid.TenantId, valid.CustomerId, valid.CustomerName, valid.ReporterUserId, valid.ReporterName, valid.ReporterEmail, valid.Subject, " ", valid.Priority, valid.Category)
        };

        foreach (var failure in openFailures)
        {
            failure.Should().Throw<ArgumentException>();
        }

        var ticket = SupportTicket.Open(valid.TenantId, valid.CustomerId, "  Acme  ", valid.ReporterUserId, "  Morgan  ", "  morgan@example.com  ", "  Subject  ", new string('x', 260), SupportTicketPriority.Urgent, "  urgent  ");
        ticket.CustomerName.Should().Be("Acme");
        ticket.ReporterName.Should().Be("Morgan");
        ticket.ReporterEmail.Should().Be("morgan@example.com");
        ticket.Subject.Should().Be("Subject");
        ticket.Category.Should().Be("urgent");
        ticket.LastMessagePreview.Should().EndWith("...");

        new Action(() => ticket.AddMessage(Guid.Empty, "Agent", null, SupportTicketMessageAuthorType.Agent, "Body", false)).Should().Throw<ArgumentException>();
        new Action(() => ticket.AddMessage(Guid.NewGuid(), " ", null, SupportTicketMessageAuthorType.Agent, "Body", false)).Should().Throw<ArgumentException>();
        new Action(() => ticket.AddMessage(Guid.NewGuid(), "Agent", null, SupportTicketMessageAuthorType.Agent, " ", false)).Should().Throw<ArgumentException>();
        new Action(() => ticket.Assign(Guid.Empty, "Agent")).Should().Throw<ArgumentException>();
        new Action(() => ticket.Assign(Guid.NewGuid(), " ")).Should().Throw<ArgumentException>();
        ticket.Assign(Guid.NewGuid(), "  Sasha  ");
        ticket.AssignedToName.Should().Be("Sasha");

        new Action(() => SupportTicketMessage.Create(Guid.Empty, valid.TenantId, valid.ReporterUserId, "Morgan", null, SupportTicketMessageAuthorType.Customer, "Body", false)).Should().Throw<ArgumentException>();
        new Action(() => SupportTicketMessage.Create(Guid.NewGuid(), Guid.Empty, valid.ReporterUserId, "Morgan", null, SupportTicketMessageAuthorType.Customer, "Body", false)).Should().Throw<ArgumentException>();
        new Action(() => SupportTicketMessage.Create(Guid.NewGuid(), valid.TenantId, Guid.Empty, "Morgan", null, SupportTicketMessageAuthorType.Customer, "Body", false)).Should().Throw<ArgumentException>();
        new Action(() => SupportTicketMessage.Create(Guid.NewGuid(), valid.TenantId, valid.ReporterUserId, " ", null, SupportTicketMessageAuthorType.Customer, "Body", false)).Should().Throw<ArgumentException>();
        new Action(() => SupportTicketMessage.Create(Guid.NewGuid(), valid.TenantId, valid.ReporterUserId, "Morgan", null, SupportTicketMessageAuthorType.Customer, " ", false)).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SupportTicketEntity_ShouldGuardClosedAndCancelledStateTransitions()
    {
        var command = NewCreateCommand(Guid.NewGuid(), SupportTicketPriority.Normal);
        var ticket = SupportTicket.Open(
            command.TenantId,
            command.CustomerId,
            command.CustomerName,
            command.ReporterUserId,
            command.ReporterName,
            command.ReporterEmail,
            command.Subject,
            command.Body,
            command.Priority,
            command.Category);
        var agentId = Guid.NewGuid();

        new Action(() => ticket.Resolve(agentId, "Agent", " ")).Should().Throw<ArgumentException>();
        ticket.Close(agentId, "Agent", null).Should().BeNull();
        new Action(() => ticket.AddMessage(agentId, "Agent", null, SupportTicketMessageAuthorType.Agent, "body", false)).Should().Throw<InvalidOperationException>();
        new Action(() => ticket.Assign(agentId, "Agent")).Should().Throw<InvalidOperationException>();
        new Action(() => ticket.Resolve(agentId, "Agent", "summary")).Should().Throw<InvalidOperationException>();

        var cancelled = SupportTicket.Open(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Customer",
            Guid.NewGuid(),
            "Reporter",
            null,
            "Subject",
            "Body",
            SupportTicketPriority.Low,
            null);
        typeof(SupportTicket).GetProperty(nameof(SupportTicket.Status))!.SetValue(cancelled, SupportTicketStatus.Cancelled);

        new Action(() => cancelled.Close(agentId, "Agent", null)).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task SupportTicketsController_ShouldDispatchAllRoutes()
    {
        var sender = new Mock<ISender>();
        var ticketId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var dto = new SupportTicketDto(
            ticketId,
            tenantId,
            Guid.NewGuid(),
            "Acme",
            Guid.NewGuid(),
            "Morgan",
            null,
            "Subject",
            "billing",
            SupportTicketStatus.Open,
            SupportTicketPriority.Normal,
            null,
            null,
            DateTime.UtcNow,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            0,
            []);
        sender.Setup(service => service.Send(It.IsAny<GetSupportTicketsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<SupportTicketDto>([dto], 1, 0, 10));
        sender.Setup(service => service.Send(It.Is<GetSupportTicketByIdQuery>(query => query.TicketId == ticketId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        sender.Setup(service => service.Send(It.IsAny<CreateSupportTicketCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        sender.Setup(service => service.Send(It.IsAny<AddSupportTicketMessageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        sender.Setup(service => service.Send(It.IsAny<AssignSupportTicketCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        sender.Setup(service => service.Send(It.IsAny<ResolveSupportTicketCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        sender.Setup(service => service.Send(It.IsAny<CloseSupportTicketCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        var controller = new SupportTicketsController(sender.Object);

        var list = await controller.List(tenantId, SupportTicketStatus.Open, SupportTicketPriority.Normal, "Acme", 0, 10, CancellationToken.None);
        var get = await controller.GetById(ticketId, tenantId, CancellationToken.None);
        var created = await controller.Create(new CreateSupportTicketRequest(tenantId, dto.CustomerId, "Acme", dto.ReporterUserId, "Morgan", null, "Subject", "Body"), CancellationToken.None);
        var added = await controller.AddMessage(ticketId, new AddSupportTicketMessageRequest(tenantId, agentId, "Agent", null, SupportTicketMessageAuthorType.Agent, "Reply", true), CancellationToken.None);
        var assigned = await controller.Assign(ticketId, new AssignSupportTicketRequest(tenantId, agentId, "Agent"), CancellationToken.None);
        var resolved = await controller.Resolve(ticketId, new ResolveSupportTicketRequest(tenantId, agentId, "Agent", "Done"), CancellationToken.None);
        var closed = await controller.Close(ticketId, new CloseSupportTicketRequest(tenantId, agentId, "Agent", "Closed"), CancellationToken.None);

        list.Result.Should().BeOfType<OkObjectResult>();
        get.Result.Should().BeOfType<OkObjectResult>();
        created.Result.Should().BeOfType<CreatedAtRouteResult>();
        added.Result.Should().BeOfType<OkObjectResult>();
        assigned.Result.Should().BeOfType<OkObjectResult>();
        resolved.Result.Should().BeOfType<OkObjectResult>();
        closed.Result.Should().BeOfType<OkObjectResult>();

        sender.Setup(service => service.Send(It.Is<GetSupportTicketByIdQuery>(query => query.TicketId == Guid.Empty), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SupportTicketDto?)null);

        var missing = await controller.GetById(Guid.Empty, tenantId, CancellationToken.None);

        missing.Result.Should().BeOfType<NotFoundResult>();
    }

    private static CreateSupportTicketCommand NewCreateCommand(
        Guid tenantId,
        SupportTicketPriority priority,
        string body = "The account billing sync is not refreshing.")
        => new(
            tenantId,
            Guid.NewGuid(),
            "Acme Properties",
            Guid.NewGuid(),
            "Morgan Support",
            "morgan@example.com",
            "Billing sync",
            body,
            priority,
            "billing");

    private static SupportTicketTestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SupportTicketTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SupportTicketTestDbContext(options);
    }

    private sealed class SupportTicketTestDbContext(DbContextOptions<SupportTicketTestDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ProductsModule.ConfigureProductsModel(modelBuilder);
        }
    }
}
