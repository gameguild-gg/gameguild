using FluentAssertions;
using GameGuild.Learning.Assessments.QuizAdapter;
using GameGuild.Learning.Courses;
using Xunit;

namespace GameGuild.Learning.Assessments.Tests;

public sealed class QuizProgramContentBoundarySecurityTests
{
    private const string GradedDocument = """
        {"schemaVersion":1,"order":[["q1","quiz"]],"blocks":{"q1":{"type":"TRUE_FALSE","stem":"Question","points":200,"correctAnswer":true,"settings":{"allowRetry":false}}},"grading":{"schemaVersion":2,"items":{"q1":{}}}}
        """;

    [Theory]
    [InlineData(ProgramContentType.Questionnaire)]
    [InlineData(ProgramContentType.Code)]
    [InlineData(ProgramContentType.Assignment)]
    [InlineData(ProgramContentType.Lesson)]
    public void GradedPayload_CannotOptOutOfTheAcademicWorkflowByChangingItsType(ProgramContentType type)
    {
        var content = new ProgramContent { Type = type, JsonBody = GradedDocument };

        CreateBoundary().GetRejection(content, ProgramContentAcademicMutation.Authoring)
            .Should().NotBeNull();
    }

    [Fact]
    public void OrdinaryStructuredLesson_StillAllowsGenericAuthoring()
    {
        var content = new ProgramContent { Type = ProgramContentType.Lesson, JsonBody = """{"root":{"children":[]}}""" };

        CreateBoundary().GetRejection(content, ProgramContentAcademicMutation.Authoring)
            .Should().BeNull();
    }

    private static QuizProgramContentBoundary CreateBoundary() => new(new QuizAssessmentTypeAdapter(
        new QuizAuthoringAdapter(new QuizItemProjector()),
        new QuizDeliveryGenerator(),
        new QuizAnswerDecoder(),
        new QuizDeterministicReviewAlgorithm()));
}
