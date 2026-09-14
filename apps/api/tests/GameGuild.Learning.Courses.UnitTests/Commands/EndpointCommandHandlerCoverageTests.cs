using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Commands;

public sealed class EndpointCommandHandlerCoverageTests
{
    [Fact]
    public async Task ProgramCrudHandler_DelegatesEveryCommand()
    {
        var service = new Mock<IProgramCrudService>(MockBehavior.Strict);
        var handler = new ProgramCrudEndpointCommandHandler(service.Object);
        var programId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        CreateProgramDto create = null!;
        UpdateProgramDto update = null!;
        UpdateProgressDto progress = null!;
        MonetizationDto monetization = null!;
        UpdatePricingDto pricing = null!;
        CreateProductFromProgramDto product = null!;
        var created = new Program();
        var updated = new Program();
        var cloned = new Program();
        var enabled = new Program();
        var disabled = new Program();
        UserProgressDto? addedUser = null;
        UserProgressDto? updatedProgress = null;
        PricingDto? updatedPricing = null;
        Guid? createdProductId = Guid.NewGuid();

        service.Setup(candidate => candidate.CreateProgramAsync(create)).ReturnsAsync(created);
        service.Setup(candidate => candidate.UpdateProgramAsync(programId, update)).ReturnsAsync(updated);
        service.Setup(candidate => candidate.DeleteProgramAsync(programId)).Returns(Task.CompletedTask);
        service.Setup(candidate => candidate.CloneProgramAsync(programId, "Copy")).ReturnsAsync(cloned);
        service.Setup(candidate => candidate.AddUserToProgramAsync(programId, userId)).ReturnsAsync(addedUser);
        service.Setup(candidate => candidate.RemoveUserFromProgramAsync(programId, userId)).ReturnsAsync(true);
        service.Setup(candidate => candidate.UpdateUserProgressAsync(programId, userId, progress)).ReturnsAsync(updatedProgress);
        service.Setup(candidate => candidate.MarkContentCompletedAsync(programId, userId, contentId)).ReturnsAsync(true);
        service.Setup(candidate => candidate.ResetUserProgressAsync(programId, userId)).ReturnsAsync(false);
        service.Setup(candidate => candidate.EnableMonetizationAsync(programId, monetization)).ReturnsAsync(enabled);
        service.Setup(candidate => candidate.DisableMonetizationAsync(programId)).ReturnsAsync(disabled);
        service.Setup(candidate => candidate.UpdateProgramPricingAsync(programId, pricing)).ReturnsAsync(updatedPricing);
        service.Setup(candidate => candidate.CreateProductFromProgramAsync(programId, product)).ReturnsAsync(createdProductId);
        service.Setup(candidate => candidate.LinkProgramToProductAsync(programId, productId)).ReturnsAsync(true);
        service.Setup(candidate => candidate.UnlinkProgramFromProductAsync(programId, productId)).ReturnsAsync(false);

        Assert.Same(created, await handler.Handle(new CreateProgramEndpointCommand(create), default));
        Assert.Same(updated, await handler.Handle(new UpdateProgramEndpointCommand(programId, update), default));
        Assert.Equal(Unit.Value, await handler.Handle(new DeleteProgramEndpointCommand(programId), default));
        Assert.Same(cloned, await handler.Handle(new CloneProgramEndpointCommand(programId, "Copy"), default));
        Assert.Null(await handler.Handle(new AddUserToProgramEndpointCommand(programId, userId), default));
        Assert.True(await handler.Handle(new RemoveUserFromProgramEndpointCommand(programId, userId), default));
        Assert.Null(await handler.Handle(new UpdateUserProgressEndpointCommand(programId, userId, progress), default));
        Assert.True(await handler.Handle(new MarkProgramContentCompletedEndpointCommand(programId, userId, contentId), default));
        Assert.False(await handler.Handle(new ResetUserProgressEndpointCommand(programId, userId), default));
        Assert.Same(enabled, await handler.Handle(new EnableProgramMonetizationEndpointCommand(programId, monetization), default));
        Assert.Same(disabled, await handler.Handle(new DisableProgramMonetizationEndpointCommand(programId), default));
        Assert.Null(await handler.Handle(new UpdateProgramPricingEndpointCommand(programId, pricing), default));
        Assert.Equal(createdProductId, await handler.Handle(new CreateProductFromProgramEndpointCommand(programId, product), default));
        Assert.True(await handler.Handle(new LinkProgramToProductEndpointCommand(programId, productId), default));
        Assert.False(await handler.Handle(new UnlinkProgramFromProductEndpointCommand(programId, productId), default));

        service.VerifyAll();
    }

    [Fact]
    public async Task ProgramContentHandler_DelegatesEveryCommandAndQuery()
    {
        var contentService = new Mock<IProgramContentService>(MockBehavior.Strict);
        var programService = new Mock<IProgramCrudService>(MockBehavior.Strict);
        var codingService = new Mock<ICodingAssignmentContentService>(MockBehavior.Strict);
        var handler = new ProgramContentEndpointCommandHandler(contentService.Object, programService.Object, codingService.Object);
        var programId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var cancellationToken = new CancellationTokenSource().Token;
        var content = new ProgramContent();
        var interaction = new ContentInteraction();
        var order = new List<(Guid contentId, int sortOrder)> { (contentId, 3) };
        var coding = new CodingAssignmentContent
        {
            Environment = new CodingEnvironment { Language = "csharp", Tools = "dotnet" },
            Data = new WorkspaceData(),
            Tests = new TestSuite(),
            Grading = new GradingConfig { MaxScore = 100 }
        };
        var codingResult = default(Result<CodingAssignmentContent>)!;
        IEnumerable<ProgramContent> searchResult = [content];

        programService.Setup(candidate => candidate.SubmitUserContentAsync(programId, userId, contentId, "answer")).ReturnsAsync(interaction);
        contentService.Setup(candidate => candidate.CreateContentAsync(content)).ReturnsAsync(content);
        contentService.Setup(candidate => candidate.UpdateContentAsync(content)).ReturnsAsync(content);
        contentService.Setup(candidate => candidate.DeleteContentAsync(contentId)).ReturnsAsync(true);
        contentService.Setup(candidate => candidate.ReorderContentAsync(programId, order)).ReturnsAsync(true);
        contentService.Setup(candidate => candidate.MoveContentAsync(contentId, parentId, 7)).ReturnsAsync(false);
        codingService.Setup(candidate => candidate.UpsertAsync(programId, contentId, coding, actorId, cancellationToken)).ReturnsAsync(codingResult);
        contentService.Setup(candidate => candidate.SearchContentAsync(programId, "needle")).ReturnsAsync(searchResult);

        Assert.Same(interaction, await handler.Handle(new SubmitProgramContentEndpointCommand(programId, userId, contentId, "answer"), cancellationToken));
        Assert.Same(content, await handler.Handle(new CreateProgramContentEndpointCommand(content), cancellationToken));
        Assert.Same(content, await handler.Handle(new UpdateProgramContentEndpointCommand(content), cancellationToken));
        Assert.True(await handler.Handle(new DeleteProgramContentEndpointCommand(contentId), cancellationToken));
        Assert.True(await handler.Handle(new ReorderProgramContentEndpointCommand(programId, order), cancellationToken));
        Assert.False(await handler.Handle(new MoveProgramContentEndpointCommand(contentId, parentId, 7), cancellationToken));
        Assert.Equal(codingResult, await handler.Handle(new PutCodingAssignmentEndpointCommand(programId, contentId, coding, actorId), cancellationToken));
        Assert.Same(searchResult, await handler.Handle(new SearchProgramContentEndpointQuery(programId, "needle"), cancellationToken));

        programService.VerifyAll();
        contentService.VerifyAll();
        codingService.VerifyAll();
    }

    [Fact]
    public async Task AuthoringHandlers_ForwardActorScopePayloadAndCancellation()
    {
        var authoring = new Mock<IProgramContentAuthoringService>(MockBehavior.Strict);
        var ai = new Mock<IAuthoringAiService>(MockBehavior.Strict);
        var contentHandler = new ProgramContentAuthoringCommandHandler(authoring.Object);
        var aiHandler = new AiAuthoringCommandHandler(ai.Object);
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var proposalId = Guid.NewGuid();
        var cancellationToken = new CancellationTokenSource().Token;
        AuthoringContentPayload payload = null!;
        AiAuthoringRunRequest runRequest = null!;
        ApplyAiProposalRequest applyRequest = null!;
        AuthoringDraftDto draft = null!;
        PublishAuthoringResult published = null!;
        AiAuthoringRunDto run = null!;
        AiProposalDto proposal = null!;

        authoring.Setup(candidate => candidate.SaveDraft(programId, contentId, 2, payload, actorId, cancellationToken)).ReturnsAsync(draft);
        authoring.Setup(candidate => candidate.Publish(programId, contentId, 3, actorId, cancellationToken)).ReturnsAsync(published);
        ai.Setup(candidate => candidate.CreateRun(tenantId, actorId, programId, contentId, runRequest, cancellationToken)).ReturnsAsync(run);
        ai.Setup(candidate => candidate.CancelRun(tenantId, actorId, programId, contentId, runId, cancellationToken)).ReturnsAsync(run);
        ai.Setup(candidate => candidate.ApplyProposal(tenantId, actorId, programId, contentId, proposalId, applyRequest, cancellationToken)).ReturnsAsync(draft);
        ai.Setup(candidate => candidate.DiscardProposal(tenantId, actorId, programId, contentId, proposalId, cancellationToken)).ReturnsAsync(proposal);

        Assert.Null(await contentHandler.Handle(new SaveProgramContentDraftCommand(programId, contentId, 2, payload, actorId), cancellationToken));
        Assert.Null(await contentHandler.Handle(new PublishProgramContentDraftCommand(programId, contentId, 3, actorId), cancellationToken));
        Assert.Null(await aiHandler.Handle(new CreateAiAuthoringRunCommand(tenantId, actorId, programId, contentId, runRequest), cancellationToken));
        Assert.Null(await aiHandler.Handle(new CancelAiAuthoringRunCommand(tenantId, actorId, programId, contentId, runId), cancellationToken));
        Assert.Null(await aiHandler.Handle(new ApplyAiAuthoringProposalCommand(tenantId, actorId, programId, contentId, proposalId, applyRequest), cancellationToken));
        Assert.Null(await aiHandler.Handle(new DiscardAiAuthoringProposalCommand(tenantId, actorId, programId, contentId, proposalId), cancellationToken));

        authoring.VerifyAll();
        ai.VerifyAll();
    }

    [Fact]
    public async Task ActivityGradeHandler_DelegatesEveryCommand()
    {
        var service = new Mock<IActivityGradeService>(MockBehavior.Strict);
        var handler = new ActivityGradeEndpointCommandHandler(service.Object);
        var interactionId = Guid.NewGuid();
        var graderId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var created = new ActivityGrade();
        var updated = new ActivityGrade();

        service.Setup(candidate => candidate.GradeActivityAsync(interactionId, graderId, 91m, "Good", "{}" )).ReturnsAsync(created);
        service.Setup(candidate => candidate.UpdateGradeAsync(gradeId, 95m, "Better", "{\"v\":2}")).ReturnsAsync(updated);
        service.Setup(candidate => candidate.DeleteGradeAsync(gradeId)).ReturnsAsync(true);

        Assert.Same(created, await handler.Handle(new GradeActivityEndpointCommand(interactionId, graderId, 91m, "Good", "{}"), default));
        Assert.Same(updated, await handler.Handle(new UpdateActivityGradeEndpointCommand(gradeId, 95m, "Better", "{\"v\":2}"), default));
        Assert.True(await handler.Handle(new DeleteActivityGradeEndpointCommand(gradeId), default));

        service.VerifyAll();
    }

    [Fact]
    public async Task ProgramLifecycleHandler_DelegatesEveryCommand()
    {
        var service = new Mock<IProgramLifecycleService>(MockBehavior.Strict);
        var handler = new ProgramLifecycleEndpointCommandHandler(service.Object);
        var programId = Guid.NewGuid();
        var publishAt = DateTime.UtcNow.AddDays(2);
        var program = new Program();

        service.Setup(candidate => candidate.SubmitProgramAsync(programId)).ReturnsAsync(program);
        service.Setup(candidate => candidate.ApproveProgramAsync(programId)).ReturnsAsync(program);
        service.Setup(candidate => candidate.RejectProgramAsync(programId, "Needs work")).ReturnsAsync(program);
        service.Setup(candidate => candidate.WithdrawProgramAsync(programId)).ReturnsAsync(program);
        service.Setup(candidate => candidate.ArchiveProgramAsync(programId)).ReturnsAsync(program);
        service.Setup(candidate => candidate.RestoreProgramAsync(programId)).ReturnsAsync(program);
        service.Setup(candidate => candidate.PublishProgramAsync(programId)).ReturnsAsync(program);
        service.Setup(candidate => candidate.UnpublishProgramAsync(programId)).ReturnsAsync(program);
        service.Setup(candidate => candidate.ScheduleProgramAsync(programId, publishAt)).ReturnsAsync(program);

        Assert.Same(program, await handler.Handle(new SubmitProgramLifecycleCommand(programId), default));
        Assert.Same(program, await handler.Handle(new ApproveProgramLifecycleCommand(programId), default));
        Assert.Same(program, await handler.Handle(new RejectProgramLifecycleCommand(programId, "Needs work"), default));
        Assert.Same(program, await handler.Handle(new WithdrawProgramLifecycleCommand(programId), default));
        Assert.Same(program, await handler.Handle(new ArchiveProgramLifecycleCommand(programId), default));
        Assert.Same(program, await handler.Handle(new RestoreProgramLifecycleCommand(programId), default));
        Assert.Same(program, await handler.Handle(new PublishProgramLifecycleCommand(programId), default));
        Assert.Same(program, await handler.Handle(new UnpublishProgramLifecycleCommand(programId), default));
        Assert.Same(program, await handler.Handle(new ScheduleProgramLifecycleCommand(programId, publishAt), default));

        service.VerifyAll();
    }

    [Fact]
    public async Task ContentInteractionHandler_DelegatesEveryCommand()
    {
        var service = new Mock<IContentInteractionService>(MockBehavior.Strict);
        var handler = new ContentInteractionEndpointCommandHandler(service.Object);
        var userId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var interactionId = Guid.NewGuid();
        var interaction = new ContentInteraction();

        service.Setup(candidate => candidate.StartContentAsync(userId, contentId)).ReturnsAsync(interaction);
        service.Setup(candidate => candidate.UpdateProgressAsync(interactionId, 55m)).ReturnsAsync(interaction);
        service.Setup(candidate => candidate.SubmitContentAsync(interactionId, "submission")).ReturnsAsync(interaction);
        service.Setup(candidate => candidate.CompleteContentAsync(interactionId)).ReturnsAsync(interaction);
        service.Setup(candidate => candidate.UpdateTimeSpentAsync(interactionId, 12)).ReturnsAsync(interaction);

        Assert.Same(interaction, await handler.Handle(new StartContentInteractionEndpointCommand(userId, contentId), default));
        Assert.Same(interaction, await handler.Handle(new UpdateContentInteractionProgressEndpointCommand(interactionId, 55m), default));
        Assert.Same(interaction, await handler.Handle(new SubmitContentInteractionEndpointCommand(interactionId, "submission"), default));
        Assert.Same(interaction, await handler.Handle(new CompleteContentInteractionEndpointCommand(interactionId), default));
        Assert.Same(interaction, await handler.Handle(new UpdateContentInteractionTimeEndpointCommand(interactionId, 12), default));

        service.VerifyAll();
    }
}
