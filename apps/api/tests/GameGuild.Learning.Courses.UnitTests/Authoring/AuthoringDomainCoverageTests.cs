using FluentAssertions;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Authoring;

public sealed class AuthoringDomainCoverageTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 14, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ProgramContentDraft_CreateRejectsEveryMissingIdentity(int emptyIndex)
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        ids[emptyIndex] = Guid.Empty;

        var act = () => ProgramContentDraft.Create(ids[0], ids[1], ids[2], ids[3], 1, "{}", Now);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Draft, program, content, and author IDs*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-json")]
    public void ProgramContentDraft_CreateRejectsInvalidPayload(string payload)
    {
        var act = () => CreateDraft(payload);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProgramContentDraft_UpdateRejectsMissingAuthor()
    {
        var draft = CreateDraft();

        var act = () => draft.Update(1, "{}", Guid.Empty, Now.AddMinutes(1));

        act.Should().Throw<ArgumentException>()
            .WithParameterName("authorId");
    }

    [Fact]
    public void ProgramContentDraft_UpdateRejectsStaleRevision()
    {
        var draft = CreateDraft();

        var act = () => draft.Update(9, "{}", Guid.NewGuid(), Now.AddMinutes(1));

        var exception = act.Should().Throw<AuthoringRevisionConflictException>().Which;
        exception.ExpectedRevision.Should().Be(9);
        exception.CurrentRevision.Should().Be(1);
    }

    [Fact]
    public void ProgramContentDraft_RebaseAdvancesPublishedBaseAndDraftRevision()
    {
        var draft = CreateDraft();
        var editor = Guid.NewGuid();

        draft.Rebase(4, editor, "{\"title\":\"rebased\"}", Now.AddMinutes(1));

        draft.BasePublishedVersion.Should().Be(4);
        draft.Revision.Should().Be(2);
        draft.LastEditedBy.Should().Be(editor);
        draft.PayloadJson.Should().Contain("rebased");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-json")]
    public void ProgramContentDraft_RebaseRejectsInvalidPayload(string payload)
    {
        var draft = CreateDraft();

        var act = () => draft.Rebase(2, Guid.NewGuid(), payload, Now.AddMinutes(1));

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void AiAuthoringProposal_CreateRejectsMissingRunOrContent(bool emptyRun, bool emptyContent)
    {
        var act = () => AiAuthoringProposal.Create(
            emptyRun ? Guid.Empty : Guid.NewGuid(),
            emptyContent ? Guid.Empty : Guid.NewGuid(),
            1,
            AiProposalKind.ReplaceDocument,
            "original",
            "proposed",
            Now);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Run and content IDs*");
    }

    [Fact]
    public void AiAuthoringProposal_CreateRejectsBlankProposedContent()
    {
        var act = () => AiAuthoringProposal.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            AiProposalKind.ReplaceDocument,
            "original",
            "   ",
            Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AiAuthoringProposal_NormalizesNullOriginalAndRejectsStaleDraft()
    {
        var proposal = AiAuthoringProposal.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            AiProposalKind.ReplaceDocument,
            null!,
            "proposed",
            Now);

        proposal.OriginalContent.Should().BeEmpty();
        var act = () => proposal.EnsureApplicableTo(2);
        var exception = act.Should().Throw<AuthoringRevisionConflictException>().Which;
        exception.ExpectedRevision.Should().Be(1);
        exception.CurrentRevision.Should().Be(2);
    }

    [Fact]
    public void AiAuthoringProposal_RejectsMissingResolverAndSecondResolution()
    {
        var proposal = CreateProposal();

        var missingActor = () => proposal.MarkApplied(Guid.Empty, Now.AddMinutes(1));
        missingActor.Should().Throw<ArgumentException>()
            .WithParameterName("actorId");

        proposal.Discard(Guid.NewGuid(), Now.AddMinutes(2));
        proposal.Status.Should().Be(AiProposalStatus.Discarded);
        var secondResolution = () => proposal.MarkApplied(Guid.NewGuid(), Now.AddMinutes(3));
        secondResolution.Should().Throw<AiProposalStateConflictException>()
            .Which.CurrentStatus.Should().Be(AiProposalStatus.Discarded);
        var applicability = () => proposal.EnsureApplicableTo(1);
        applicability.Should().Throw<AiProposalStateConflictException>();
    }

    private static ProgramContentDraft CreateDraft(string payload = "{}") =>
        ProgramContentDraft.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            payload,
            Now);

    private static AiAuthoringProposal CreateProposal() =>
        AiAuthoringProposal.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            AiProposalKind.ReplaceDocument,
            "original",
            "proposed",
            Now);
}
