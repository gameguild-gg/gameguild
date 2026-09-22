using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
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
                false),
            CancellationToken.None);

        replyResult.MessageCount.Should().Be(2);
        replyResult.Status.Should().Be(SupportTicketStatus.InProgress);
        replyResult.AssignedToUserId.Should().BeNull();

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
    public async Task SupportTicketQuery_ShouldFilterByOwningCustomer()
    {
        await using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var expected = await new CreateSupportTicketCommandHandler(db).Handle(
            NewCreateCommand(tenantId, SupportTicketPriority.Normal, customerId: courseId),
            CancellationToken.None);
        await new CreateSupportTicketCommandHandler(db).Handle(
            NewCreateCommand(tenantId, SupportTicketPriority.Normal, customerId: Guid.NewGuid()),
            CancellationToken.None);

        var result = await new GetSupportTicketsQueryHandler(db).Handle(
            new GetSupportTicketsQuery(TenantId: tenantId, CustomerId: courseId, Take: 100),
            CancellationToken.None);

        result.Items.Should().ContainSingle(ticket => ticket.Id == expected.Id);
        result.TotalCount.Should().Be(1);
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
    public void Customer_Reply_Should_Reopen_A_Resolved_Ticket()
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
        ticket.Resolve(Guid.NewGuid(), "Support Agent", "Issue fixed.");

        ticket.AddMessage(
            command.ReporterUserId,
            command.ReporterName,
            command.ReporterEmail,
            SupportTicketMessageAuthorType.Customer,
            "The issue came back.",
            false);

        ticket.Status.Should().Be(SupportTicketStatus.Open);
        ticket.ResolvedAt.Should().BeNull();
        ticket.ClosedAt.Should().BeNull();
    }

    [Fact]
    public async Task Self_Service_Query_Should_Never_Return_Internal_Notes()
    {
        await using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var created = await new CreateSupportTicketCommandHandler(db).Handle(
            NewCreateCommand(tenantId, SupportTicketPriority.Normal),
            CancellationToken.None);
        await new AddSupportTicketMessageCommandHandler(db).Handle(
            new AddSupportTicketMessageCommand(
                created.Id,
                tenantId,
                Guid.NewGuid(),
                "Support Agent",
                "agent@example.test",
                SupportTicketMessageAuthorType.Agent,
                "Internal investigation details.",
                true),
            CancellationToken.None);

        var result = await new GetSupportTicketByIdQueryHandler(db).Handle(
            new GetSupportTicketByIdQuery(created.Id, tenantId, IncludeInternalMessages: false),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Messages.Should().OnlyContain(message => !message.IsInternal);
        result.Messages.Should().NotContain(message => message.Body.Contains("investigation"));
    }

    [Fact]
    public void Internal_Note_Should_Not_Start_The_Public_Response_Clock()
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

        ticket.AddMessage(
            Guid.NewGuid(),
            "Support Agent",
            "agent@example.test",
            SupportTicketMessageAuthorType.Agent,
            "Private note.",
            true);

        ticket.Status.Should().Be(SupportTicketStatus.Open);
        ticket.FirstResponseAt.Should().BeNull();
    }

    [Fact]
    public void CorrectCustomerIdentity_Should_Backfill_Legacy_Tenant_Identifiers()
    {
        var tenantId = Guid.NewGuid();
        var reporterId = Guid.NewGuid();
        var ticket = SupportTicket.Open(
            tenantId,
            tenantId,
            "Legacy workspace",
            reporterId,
            "Riley Parker",
            "riley@example.test",
            "Legacy ticket",
            "This ticket predates the customer identity correction.",
            SupportTicketPriority.Normal,
            "General");

        ticket.CorrectCustomerIdentity(reporterId, "Riley Parker");

        ticket.CustomerId.Should().Be(reporterId);
        ticket.CustomerName.Should().Be("Riley Parker");
    }

    [Fact]
    public async Task Manager_Workflow_Should_Start_Change_Priority_And_Reopen()
    {
        await using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var created = await new CreateSupportTicketCommandHandler(db).Handle(
            NewCreateCommand(tenantId, SupportTicketPriority.Low), CancellationToken.None);

        var started = await new StartSupportTicketCommandHandler(db).Handle(
            new StartSupportTicketCommand(created.Id, tenantId), CancellationToken.None);
        var prioritized = await new ChangeSupportTicketPriorityCommandHandler(db).Handle(
            new ChangeSupportTicketPriorityCommand(created.Id, tenantId, SupportTicketPriority.Urgent),
            CancellationToken.None);
        await new ResolveSupportTicketCommandHandler(db).Handle(
            new ResolveSupportTicketCommand(created.Id, tenantId, Guid.NewGuid(), "Agent", "Done"),
            CancellationToken.None);
        var reopened = await new ReopenSupportTicketCommandHandler(db).Handle(
            new ReopenSupportTicketCommand(created.Id, tenantId), CancellationToken.None);

        started.Status.Should().Be(SupportTicketStatus.InProgress);
        prioritized.Priority.Should().Be(SupportTicketPriority.Urgent);
        reopened.Status.Should().Be(SupportTicketStatus.Open);
        reopened.ResolvedAt.Should().BeNull();
    }

    [Fact]
    public async Task SupportTicketsController_ShouldDispatchAllRoutes()
    {
        await using var db = CreateDbContext();
        var sender = new Mock<ISender>();
        var ticketId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        db.AddRange(
            new User { Id = managerId, Name = "Manager", Email = "manager@example.test", IsActive = true },
            new User { Id = agentId, Name = "Agent", Email = "agent@example.test", IsActive = true },
            new User { Id = customerId, Name = "Acme", Email = "customer@example.test", IsActive = true },
            new TenantMember { Id = Guid.NewGuid(), TenantId = tenantId, UserId = managerId, Role = "PropertyManager", IsActive = true },
            new TenantMember { Id = Guid.NewGuid(), TenantId = tenantId, UserId = agentId, Role = "PropertyManager", IsActive = true },
            new TenantMember { Id = Guid.NewGuid(), TenantId = tenantId, UserId = customerId, Role = "Renter", IsActive = true });
        await db.SaveChangesAsync();
        var dto = new SupportTicketDto(
            ticketId,
            tenantId,
            customerId,
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
        sender.Setup(service => service.Send(It.IsAny<StartSupportTicketCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        sender.Setup(service => service.Send(It.IsAny<ChangeSupportTicketPriorityCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        sender.Setup(service => service.Send(It.IsAny<ReopenSupportTicketCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);
        var actorAccessor = new ActorContextAccessor();
        actorAccessor.SetActorContext(ActorContextBuilder.ForUser(managerId)
            .WithTenantId(tenantId).WithRole("PropertyManager").Build());
        var resolvedTenant = new Mock<IAuthorizationTenantContext>();
        resolvedTenant.SetupGet(item => item.TenantId).Returns(tenantId);
        var controller = new SupportTicketsController(sender.Object, actorAccessor, resolvedTenant.Object, db);

        var list = await controller.List(SupportTicketStatus.Open, SupportTicketPriority.Normal, "Acme", 0, 10, CancellationToken.None);
        var get = await controller.GetById(ticketId, CancellationToken.None);
        var created = await controller.Create(new CreateSupportTicketRequest(dto.CustomerId, "Subject", "Body"), CancellationToken.None);
        var added = await controller.AddMessage(ticketId, new AddSupportTicketMessageRequest("Reply", true), CancellationToken.None);
        var assigned = await controller.Assign(ticketId, new AssignSupportTicketRequest(agentId), CancellationToken.None);
        var started = await controller.Start(ticketId, CancellationToken.None);
        var prioritized = await controller.ChangePriority(ticketId, new ChangeSupportTicketPriorityRequest(SupportTicketPriority.High), CancellationToken.None);
        var resolved = await controller.Resolve(ticketId, new ResolveSupportTicketRequest("Done"), CancellationToken.None);
        var closed = await controller.Close(ticketId, new CloseSupportTicketRequest("Closed"), CancellationToken.None);
        var reopened = await controller.Reopen(ticketId, CancellationToken.None);

        list.Result.Should().BeOfType<OkObjectResult>();
        get.Result.Should().BeOfType<OkObjectResult>();
        created.Result.Should().BeOfType<CreatedAtRouteResult>();
        added.Result.Should().BeOfType<OkObjectResult>();
        assigned.Result.Should().BeOfType<OkObjectResult>();
        started.Result.Should().BeOfType<OkObjectResult>();
        prioritized.Result.Should().BeOfType<OkObjectResult>();
        resolved.Result.Should().BeOfType<OkObjectResult>();
        closed.Result.Should().BeOfType<OkObjectResult>();
        reopened.Result.Should().BeOfType<OkObjectResult>();

        sender.Setup(service => service.Send(It.Is<GetSupportTicketByIdQuery>(query => query.TicketId == Guid.Empty), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SupportTicketDto?)null);

        var missing = await controller.GetById(Guid.Empty, CancellationToken.None);

        missing.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task MySupportTicketsController_Create_ShouldUseResolvedTenantAndAuthenticatedUser()
    {
        var sender = new Mock<ISender>();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var actorAccessor = new ActorContextAccessor();
        actorAccessor.SetActorContext(ActorContextBuilder.ForUser(userId)
            .WithTenantId(tenantId)
            .WithRole("Renter")
            .Build());
        var resolvedTenant = new Mock<IAuthorizationTenantContext>();
        resolvedTenant.SetupGet(item => item.TenantId).Returns(tenantId);
        sender.Setup(service => service.Send(It.IsAny<CreateSupportTicketCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SupportTicketDto)null!);
        var controller = new MySupportTicketsController(sender.Object, actorAccessor, resolvedTenant.Object);

        await controller.Create(
            new CreateMySupportTicketRequest("Lease question", "Please review my lease."),
            CancellationToken.None);

        sender.Verify(service => service.Send(
            It.Is<CreateSupportTicketCommand>(command =>
                command.TenantId == tenantId &&
                command.CustomerId == userId &&
                command.ReporterUserId == userId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MySupportTicketsController_List_ShouldForbidMismatchedResolvedTenant()
    {
        var sender = new Mock<ISender>();
        var actorTenantId = Guid.NewGuid();
        var resolvedTenantId = Guid.NewGuid();
        var actorAccessor = new ActorContextAccessor();
        actorAccessor.SetActorContext(ActorContextBuilder.ForUser(Guid.NewGuid())
            .WithTenantId(actorTenantId)
            .WithRole("Renter")
            .Build());
        var resolvedTenant = new Mock<IAuthorizationTenantContext>();
        resolvedTenant.SetupGet(item => item.TenantId).Returns(resolvedTenantId);
        var controller = new MySupportTicketsController(sender.Object, actorAccessor, resolvedTenant.Object);

        var result = await controller.List(cancellationToken: CancellationToken.None);

        result.Result.Should().BeOfType<ForbidResult>();
        sender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MySupportTicketsController_List_ShouldAllowTenantAdministratorSelfService()
    {
        var sender = new Mock<ISender>();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var actorAccessor = new ActorContextAccessor();
        actorAccessor.SetActorContext(ActorContextBuilder.ForUser(userId)
            .WithTenantId(tenantId)
            .WithRole("TenantAdmin")
            .Build());
        var resolvedTenant = new Mock<IAuthorizationTenantContext>();
        resolvedTenant.SetupGet(item => item.TenantId).Returns(tenantId);
        sender.Setup(service => service.Send(
                It.Is<GetSupportTicketsQuery>(query =>
                    query.TenantId == tenantId &&
                    query.CustomerId == userId &&
                    !query.IncludeInternalMessages),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<SupportTicketDto>([], 0, 0, 50));
        var controller = new MySupportTicketsController(sender.Object, actorAccessor, resolvedTenant.Object);

        var result = await controller.List(cancellationToken: CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        sender.VerifyAll();
    }

    private static CreateSupportTicketCommand NewCreateCommand(
        Guid tenantId,
        SupportTicketPriority priority,
        string body = "The account billing sync is not refreshing.",
        Guid? customerId = null)
        => new(
            tenantId,
            customerId ?? Guid.NewGuid(),
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
