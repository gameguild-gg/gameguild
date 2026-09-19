using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests;

public sealed class ProgramContentDeleteWorkflowTests
{
    [Fact]
    public async Task DeleteContent_DelegatesGradedQuizDeletionToTheAtomicContentWorkflow()
    {
        var programId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var content = new ProgramContent
        {
            Id = contentId,
            ProgramId = programId,
            Title = "Graded quiz",
            Type = ProgramContentType.Questionnaire,
            JsonBody = "{\"grading\":{\"enabled\":true}}",
            Version = 1,
        };
        var contentService = new Mock<IProgramContentService>();
        contentService.Setup(service => service.GetContentByIdAsync(contentId))
            .ReturnsAsync(content);
        var sender = new Mock<ISender>();
        sender.Setup(service => service.Send(
                It.Is<DeleteProgramContentEndpointCommand>(command => command.ContentId == contentId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var guard = new Mock<IProgramContentAcademicMutationGuard>();
        guard.Setup(value => value.GetRejection(content, ProgramContentAcademicMutation.Delete))
            .Returns("Graded quiz content and its assessment must be removed atomically.");
        var controller = new ProgramContentController(
            contentService.Object,
            Mock.Of<IProgramCrudService>(),
            Mock.Of<ICodingAssignmentContentService>(),
            Mock.Of<IAuthorizationService>(),
            [],
            [guard.Object],
            Mock.Of<ILogger<ProgramContentController>>(),
            sender.Object);

        var result = await controller.DeleteContent(programId, contentId);

        result.Should().BeOfType<NoContentResult>();
        sender.VerifyAll();
    }
}
