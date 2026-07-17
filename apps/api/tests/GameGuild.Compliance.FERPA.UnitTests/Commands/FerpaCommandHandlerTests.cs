using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Compliance.FERPA.UnitTests.Commands;

public sealed class FerpaCommandHandlerTests
{
    [Fact]
    public async Task RegisterRecordHandler_ForwardsFullCommandAndCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var command = new RegisterEducationRecordCommand(Guid.NewGuid(), EducationRecordKind.Certificate, "cert-1", "Certificate");
        var expected = new FerpaEducationRecordDto(
            Guid.NewGuid(), command.StudentUserId, command.RecordKind, command.ExternalRecordId, command.Title,
            command.ProtectionLevel, command.IsDirectoryInformation, command.RetentionUntil, command.MetadataJson, DateTime.UtcNow);
        var service = new Mock<IFerpaService>(MockBehavior.Strict);
        service.Setup(subject => subject.RegisterEducationRecordAsync(command, cancellation.Token)).ReturnsAsync(expected);

        var result = await new RegisterEducationRecordCommandHandler(service.Object).Handle(command, cancellation.Token);

        result.Should().BeSameAs(expected);
        service.VerifyAll();
    }

    [Fact]
    public async Task GrantConsentHandler_ForwardsEffectiveWindowAndGuardian()
    {
        var command = new GrantFerpaDisclosureConsentCommand(
            Guid.NewGuid(), "Parent", "Progress review", "grades", DateTime.UtcNow, Guid.NewGuid(), DateTime.UtcNow.AddMonths(1));
        var expected = new FerpaDisclosureConsentDto(
            Guid.NewGuid(), command.StudentUserId, command.GuardianUserId, command.Recipient, command.Purpose,
            command.Scope, command.EffectiveFrom, command.ExpiresAt, null, true);
        var service = new Mock<IFerpaService>(MockBehavior.Strict);
        service.Setup(subject => subject.GrantDisclosureConsentAsync(command, default)).ReturnsAsync(expected);

        var result = await new GrantFerpaDisclosureConsentCommandHandler(service.Object).Handle(command, default);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task SubmitInspectionHandler_ForwardsDeadlineAndRequester()
    {
        var command = new SubmitFerpaInspectionRequestCommand(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(45), "Transcript");
        var expected = new FerpaInspectionRequestDto(
            Guid.NewGuid(), command.StudentUserId, command.RequestedByUserId, FerpaRequestStatus.Pending,
            command.Deadline, null, null, null);
        var service = new Mock<IFerpaService>(MockBehavior.Strict);
        service.Setup(subject => subject.SubmitInspectionRequestAsync(command, default)).ReturnsAsync(expected);

        var result = await new SubmitFerpaInspectionRequestCommandHandler(service.Object).Handle(command, default);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task DirectoryPolicyQueryHandler_ForwardsTenantScope()
    {
        var tenantId = Guid.NewGuid();
        var query = new GetDirectoryInformationPolicyQuery(tenantId);
        var expected = new FerpaDirectoryInformationPolicyDto(Guid.NewGuid(), tenantId, "[]", true, null, null);
        var service = new Mock<IFerpaService>(MockBehavior.Strict);
        service.Setup(subject => subject.GetDirectoryPolicyAsync(tenantId, default)).ReturnsAsync(expected);

        var result = await new GetDirectoryInformationPolicyQueryHandler(service.Object).Handle(query, default);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task RecordDisclosureHandler_ForwardsSecuritySensitiveCommandAndCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var command = new RecordFerpaDisclosureCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Registrar",
            FerpaDisclosureBasis.CourtOrder,
            "Legal request",
            "transcript",
            "[\"record-1\"]",
            DateTime.UtcNow);
        var expected = new FerpaDisclosureLogDto(
            Guid.NewGuid(), command.StudentUserId, command.DisclosedByUserId, command.Recipient,
            command.Basis, command.Purpose, command.RecordIdsJson, command.DisclosedAt);
        var service = new Mock<IFerpaService>(MockBehavior.Strict);
        service.Setup(subject => subject.RecordDisclosureAsync(command, cancellation.Token)).ReturnsAsync(expected);

        var result = await new RecordFerpaDisclosureCommandHandler(service.Object).Handle(command, cancellation.Token);

        result.Should().BeSameAs(expected);
        service.VerifyAll();
    }

    [Fact]
    public async Task CompleteInspectionHandler_ForwardsDecisionUnchanged()
    {
        var command = new CompleteFerpaInspectionRequestCommand(Guid.NewGuid(), Guid.NewGuid(), false, "Identity mismatch");
        var expected = new FerpaInspectionRequestDto(
            command.RequestId, Guid.NewGuid(), Guid.NewGuid(), FerpaRequestStatus.Denied,
            DateTime.UtcNow, command.ProcessedByUserId, DateTime.UtcNow, command.Notes);
        var service = new Mock<IFerpaService>(MockBehavior.Strict);
        service.Setup(subject => subject.CompleteInspectionRequestAsync(command, default)).ReturnsAsync(expected);

        var result = await new CompleteFerpaInspectionRequestCommandHandler(service.Object).Handle(command, default);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task RevokeConsentHandler_UsesConsentIdFromCommand()
    {
        var command = new RevokeFerpaDisclosureConsentCommand(Guid.NewGuid());
        var service = new Mock<IFerpaService>(MockBehavior.Strict);
        service.Setup(subject => subject.RevokeDisclosureConsentAsync(command.ConsentId, default)).ReturnsAsync(true);

        var result = await new RevokeFerpaDisclosureConsentCommandHandler(service.Object).Handle(command, default);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task PendingInspectionQueryHandler_ReturnsServiceOrdering()
    {
        var expected = new List<FerpaInspectionRequestDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), FerpaRequestStatus.Pending, DateTime.UtcNow.AddDays(1), null, null, null),
            new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), FerpaRequestStatus.Pending, DateTime.UtcNow.AddDays(2), null, null, null)
        };
        var service = new Mock<IFerpaService>(MockBehavior.Strict);
        service.Setup(subject => subject.GetPendingInspectionRequestsAsync(default)).ReturnsAsync(expected);

        var result = await new GetPendingFerpaInspectionRequestsQueryHandler(service.Object)
            .Handle(new GetPendingFerpaInspectionRequestsQuery(), default);

        result.Should().BeSameAs(expected);
    }
}
