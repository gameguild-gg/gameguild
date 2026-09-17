using FluentAssertions;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingLabValidationBranchCoverageTests
{
    [Fact]
    public void ProjectBrief_RejectsEveryMissingRequiredField()
    {
        Invoking(() => Brief(objective: " ").EnsureValid()).Should().Throw<ArgumentException>();
        Invoking(() => Brief(installation: " ").EnsureValid()).Should().Throw<ArgumentException>();
        Invoking(() => Brief(controls: " ").EnsureValid()).Should().Throw<ArgumentException>();
        Invoking(() => Brief(limitations: " ").EnsureValid()).Should().Throw<ArgumentException>();
        Invoking(() => new TestingProjectBrief(
            "Objective", "Install", null!, "Controls", "Limitations").EnsureValid())
            .Should().Throw<ArgumentException>();
        Invoking(() => Brief(tasks: []).EnsureValid()).Should().Throw<ArgumentException>();
        Invoking(() => Brief(tasks: ["Task", " "]).EnsureValid()).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProjectBrief_EnforcesTaskAndLinkBounds()
    {
        Invoking(() => Brief(tasks: Enumerable.Repeat("Task", 51).ToArray()).EnsureValid())
            .Should().Throw<ArgumentException>().WithMessage("*more than 50 tasks*");
        Invoking(() => Brief(links: ["relative/path"]).EnsureValid())
            .Should().Throw<ArgumentException>().WithMessage("*absolute URL*");

        Invoking(() => Brief(links: null).EnsureValid()).Should().NotThrow();
        Invoking(() => Brief(links: ["https://example.test/build"]).EnsureValid()).Should().NotThrow();
    }

    [Fact]
    public void QuestionnaireRevision_ValidatesIdentityRevisionAndOptionalSchemaValidation()
    {
        var applicationId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var valid = new QuestionnaireSchema("Valid", []);

        Invoking(() => TestingQuestionnaireRevision.Create(
            Guid.Empty, 1, valid, creatorId, null)).Should().Throw<ArgumentException>();
        Invoking(() => TestingQuestionnaireRevision.Create(
            applicationId, 1, valid, Guid.Empty, null)).Should().Throw<ArgumentException>();
        Invoking(() => TestingQuestionnaireRevision.Create(
            applicationId, 0, valid, creatorId, null)).Should().Throw<ArgumentOutOfRangeException>();

        var invalid = new QuestionnaireSchema("Invalid", [
            new QuestionnaireQuestion("question", " ", QuestionnaireQuestionType.FreeText, true)
        ]);
        Invoking(() => TestingQuestionnaireRevision.Create(
            applicationId, 1, invalid, creatorId, null, ensureValid: true))
            .Should().Throw<ArgumentException>();
        var legacy = TestingQuestionnaireRevision.Create(
            applicationId, 1, invalid, creatorId, null, ensureValid: false);
        legacy.Schema.Questions.Should().ContainSingle().Which.Prompt.Should().Be(" ");
    }

    private static TestingProjectBrief Brief(
        string objective = "Objective",
        string installation = "Install",
        IReadOnlyList<string>? tasks = null,
        string controls = "Controls",
        string limitations = "Limitations",
        IReadOnlyList<string>? links = null) =>
        new(objective, installation, tasks ?? ["Task"], controls, limitations, links);

    private static Action Invoking(Action action) => action;
}
