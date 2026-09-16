using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace GameGuild.TestingLab.UnitTests;

public sealed class TestingLabDtoCoverageTests
{
    [Fact]
    public void CreateTestingRequestDto_MapsEverySupportedField()
    {
        var creatorId = Guid.NewGuid();
        var projectVersionId = Guid.NewGuid();
        var instructionsFileId = Guid.NewGuid();
        var start = SystemClock.UtcNow.AddDays(2);
        var end = start.AddHours(4);
        var dto = new CreateTestingRequestDto
        {
            ProjectVersionId = projectVersionId,
            Title = "Build verification",
            Description = "Verify the release candidate",
            DownloadUrl = "https://example.test/build",
            InstructionsType = InstructionType.File,
            InstructionsContent = "Install and complete the smoke test.",
            InstructionsUrl = "https://example.test/instructions",
            InstructionsFileId = instructionsFileId,
            FeedbackFormContent = "What failed?",
            MaxTesters = 12,
            StartDate = start,
            EndDate = end,
            Status = TestingRequestStatus.Active
        };

        var request = dto.ToTestingRequest(creatorId);

        request.ProjectVersionId.Should().Be(projectVersionId);
        request.Title.Should().Be(dto.Title);
        request.Description.Should().Be(dto.Description);
        request.DownloadUrl.Should().Be(dto.DownloadUrl);
        request.InstructionsType.Should().Be(dto.InstructionsType);
        request.InstructionsContent.Should().Be(dto.InstructionsContent);
        request.InstructionsUrl.Should().Be(dto.InstructionsUrl);
        request.InstructionsFileId.Should().Be(instructionsFileId);
        request.FeedbackFormContent.Should().Be(dto.FeedbackFormContent);
        request.MaxTesters.Should().Be(12);
        request.StartDate.Should().Be(start);
        request.EndDate.Should().Be(end);
        request.Status.Should().Be(TestingRequestStatus.Active);
        request.CreatedById.Should().Be(creatorId);
    }

    [Fact]
    public void UpdateTestingRequestDto_AppliesEveryProvidedField()
    {
        var projectVersionId = Guid.NewGuid();
        var instructionsFileId = Guid.NewGuid();
        var start = SystemClock.UtcNow.AddDays(3);
        var end = start.AddHours(3);
        var request = new TestingRequest();
        var dto = new UpdateTestingRequestDto
        {
            ProjectVersionId = projectVersionId,
            Title = "Updated title",
            Description = "Updated description",
            DownloadUrl = "https://example.test/updated-build",
            InstructionsType = InstructionType.Url,
            InstructionsContent = "Updated instructions",
            InstructionsUrl = "https://example.test/updated-instructions",
            InstructionsFileId = instructionsFileId,
            MaxTesters = 8,
            FeedbackFormContent = "Updated questions",
            StartDate = start,
            EndDate = end,
            Status = TestingRequestStatus.Active
        };

        dto.UpdateTestingRequest(request);

        request.ProjectVersionId.Should().Be(projectVersionId);
        request.Title.Should().Be(dto.Title);
        request.Description.Should().Be(dto.Description);
        request.DownloadUrl.Should().Be(dto.DownloadUrl);
        request.InstructionsType.Should().Be(InstructionType.Url);
        request.InstructionsContent.Should().Be(dto.InstructionsContent);
        request.InstructionsUrl.Should().Be(dto.InstructionsUrl);
        request.InstructionsFileId.Should().Be(instructionsFileId);
        request.MaxTesters.Should().Be(8);
        request.FeedbackFormContent.Should().Be(dto.FeedbackFormContent);
        request.StartDate.Should().Be(start);
        request.EndDate.Should().Be(end);
        request.Status.Should().Be(TestingRequestStatus.Active);
    }

    [Fact]
    public void UpdateTestingRequestDto_OmitsUnsetAndEmptyFields()
    {
        var originalId = Guid.NewGuid();
        var originalStart = SystemClock.UtcNow.AddDays(1);
        var request = new TestingRequest
        {
            ProjectVersionId = originalId,
            Title = "Original",
            Description = "Keep",
            DownloadUrl = "https://example.test/original",
            InstructionsType = InstructionType.Text,
            InstructionsContent = "Keep",
            InstructionsUrl = "https://example.test/original-instructions",
            InstructionsFileId = originalId,
            MaxTesters = 4,
            FeedbackFormContent = "Keep",
            StartDate = originalStart,
            EndDate = originalStart.AddHours(1),
            Status = TestingRequestStatus.Draft
        };

        new UpdateTestingRequestDto { Title = string.Empty }.UpdateTestingRequest(request);

        request.ProjectVersionId.Should().Be(originalId);
        request.Title.Should().Be("Original");
        request.Description.Should().Be("Keep");
        request.DownloadUrl.Should().Be("https://example.test/original");
        request.InstructionsType.Should().Be(InstructionType.Text);
        request.InstructionsContent.Should().Be("Keep");
        request.InstructionsUrl.Should().Be("https://example.test/original-instructions");
        request.InstructionsFileId.Should().Be(originalId);
        request.MaxTesters.Should().Be(4);
        request.FeedbackFormContent.Should().Be("Keep");
        request.StartDate.Should().Be(originalStart);
        request.EndDate.Should().Be(originalStart.AddHours(1));
        request.Status.Should().Be(TestingRequestStatus.Draft);
    }

    [Fact]
    public void CreateTestingSessionDto_MapsEverySupportedField()
    {
        var creatorId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var managerId = Guid.NewGuid();
        var sessionDate = SystemClock.UtcNow.AddDays(2).Date;
        var start = sessionDate.AddHours(9);
        var end = start.AddHours(2);
        var dto = new CreateTestingSessionDto
        {
            TestingRequestId = requestId,
            LocationId = locationId,
            SessionName = "Morning session",
            SessionDate = sessionDate,
            StartTime = start,
            EndTime = end,
            MaxTesters = 10,
            MaxProjects = 3,
            Status = SessionStatus.Scheduled,
            ManagerUserId = managerId
        };

        var session = dto.ToTestingSession(creatorId);

        session.TestingRequestId.Should().Be(requestId);
        session.LocationId.Should().Be(locationId);
        session.SessionName.Should().Be(dto.SessionName);
        session.SessionDate.Should().Be(sessionDate);
        session.StartTime.Should().Be(start);
        session.EndTime.Should().Be(end);
        session.MaxTesters.Should().Be(10);
        session.MaxProjects.Should().Be(3);
        session.Status.Should().Be(SessionStatus.Scheduled);
        session.ManagerId.Should().Be(managerId);
        session.ManagerUserId.Should().Be(managerId);
        session.CreatedById.Should().Be(creatorId);
    }

    [Fact]
    public void TestingLocationDtos_MapAndApplyEverySupportedField()
    {
        var create = new CreateTestingLocationDto
        {
            Name = "Studio A",
            Description = "Main lab",
            Address = "100 Test Street",
            City = "Toronto",
            State = "ON",
            PostalCode = "A1A 1A1",
            Country = "Canada",
            MaxTestersCapacity = 20,
            MaxProjectsCapacity = 5,
            EquipmentAvailable = "PCs",
            IsVirtual = true,
            VirtualUrl = "https://example.test/room",
            ContactEmail = "lab@example.test",
            ContactPhone = "+1 555 0100",
            Status = LocationStatus.Maintenance
        };

        var location = create.ToTestingLocation();
        location.Name.Should().Be(create.Name);
        location.Description.Should().Be(create.Description);
        location.Address.Should().Be(create.Address);
        location.City.Should().Be(create.City);
        location.State.Should().Be(create.State);
        location.PostalCode.Should().Be(create.PostalCode);
        location.Country.Should().Be(create.Country);
        location.MaxTestersCapacity.Should().Be(20);
        location.MaxProjectsCapacity.Should().Be(5);
        location.EquipmentAvailable.Should().Be(create.EquipmentAvailable);
        location.IsVirtual.Should().BeTrue();
        location.VirtualUrl.Should().Be(create.VirtualUrl);
        location.ContactEmail.Should().Be(create.ContactEmail);
        location.ContactPhone.Should().Be(create.ContactPhone);
        location.Status.Should().Be(LocationStatus.Maintenance);

        var update = new UpdateTestingLocationDto
        {
            Name = "Studio B",
            Description = "Secondary lab",
            Address = "200 Test Street",
            City = "Vancouver",
            State = "BC",
            PostalCode = "B2B 2B2",
            Country = "Canada",
            MaxTestersCapacity = 30,
            MaxProjectsCapacity = 6,
            EquipmentAvailable = "Consoles",
            IsVirtual = false,
            VirtualUrl = "https://example.test/new-room",
            ContactEmail = "new@example.test",
            ContactPhone = "+1 555 0200",
            Status = LocationStatus.Active
        };
        update.UpdateTestingLocation(location);

        location.Name.Should().Be(update.Name);
        location.Description.Should().Be(update.Description);
        location.Address.Should().Be(update.Address);
        location.City.Should().Be(update.City);
        location.State.Should().Be(update.State);
        location.PostalCode.Should().Be(update.PostalCode);
        location.Country.Should().Be(update.Country);
        location.MaxTestersCapacity.Should().Be(30);
        location.MaxProjectsCapacity.Should().Be(6);
        location.EquipmentAvailable.Should().Be(update.EquipmentAvailable);
        location.IsVirtual.Should().BeFalse();
        location.VirtualUrl.Should().Be(update.VirtualUrl);
        location.ContactEmail.Should().Be(update.ContactEmail);
        location.ContactPhone.Should().Be(update.ContactPhone);
        location.Status.Should().Be(LocationStatus.Active);
    }

    [Fact]
    public void UpdateTestingLocationDto_OmitsUnsetAndEmptyFields()
    {
        var location = new TestingLocation
        {
            Name = "Original",
            Description = "Keep",
            Address = "Keep",
            City = "Keep",
            State = "Keep",
            PostalCode = "Keep",
            Country = "Keep",
            MaxTestersCapacity = 10,
            MaxProjectsCapacity = 2,
            EquipmentAvailable = "Keep",
            IsVirtual = true,
            VirtualUrl = "Keep",
            ContactEmail = "Keep",
            ContactPhone = "Keep",
            Status = LocationStatus.Inactive
        };

        new UpdateTestingLocationDto { Name = string.Empty }.UpdateTestingLocation(location);

        location.Name.Should().Be("Original");
        location.Description.Should().Be("Keep");
        location.Address.Should().Be("Keep");
        location.City.Should().Be("Keep");
        location.State.Should().Be("Keep");
        location.PostalCode.Should().Be("Keep");
        location.Country.Should().Be("Keep");
        location.MaxTestersCapacity.Should().Be(10);
        location.MaxProjectsCapacity.Should().Be(2);
        location.EquipmentAvailable.Should().Be("Keep");
        location.IsVirtual.Should().BeTrue();
        location.VirtualUrl.Should().Be("Keep");
        location.ContactEmail.Should().Be("Keep");
        location.ContactPhone.Should().Be("Keep");
        location.Status.Should().Be(LocationStatus.Inactive);
    }
}

public sealed class TestingLabValidatorCoverageTests
{
    [Fact]
    public void CreateTestingRequestValidator_AcceptsACompleteFutureRequest()
    {
        var start = SystemClock.UtcNow.AddDays(2);
        var command = new CreateTestingRequestCommand(
            Guid.NewGuid(), "Release candidate", "Description", "https://example.test/build",
            InstructionType.Text, "Instructions", null, null, "Feedback", 5,
            start, start.AddHours(2));

        new CreateTestingRequestCommandValidator().TestValidate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateTestingRequestValidator_RejectsEachInvalidConstraint()
    {
        var start = SystemClock.UtcNow.AddDays(-1);
        var command = new CreateTestingRequestCommand(
            Guid.Empty, string.Empty, new string('d', 2001), "relative/build",
            InstructionType.Text, null, null, null, null, 0,
            start, start.AddMinutes(-1));

        var result = new CreateTestingRequestCommandValidator().TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ProjectVersionId);
        result.ShouldHaveValidationErrorFor(x => x.Title);
        result.ShouldHaveValidationErrorFor(x => x.Description);
        result.ShouldHaveValidationErrorFor(x => x.DownloadUrl);
        result.ShouldHaveValidationErrorFor(x => x.MaxTesters);
        result.ShouldHaveValidationErrorFor(x => x.StartDate);
        result.ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public void CreateTestingRequestValidator_SkipsOptionalRulesWhenValuesAreAbsent()
    {
        var start = SystemClock.UtcNow.AddDays(2);
        var command = new CreateTestingRequestCommand(
            Guid.NewGuid(), "Request", null, null, InstructionType.Text, null, null, null,
            null, null, start, start.AddHours(1));

        new CreateTestingRequestCommandValidator().TestValidate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateTestingSessionValidator_CoversOnlineAndPhysicalLocationRules()
    {
        var scheduled = SystemClock.UtcNow.AddDays(2);
        var validator = new CreateTestingSessionCommandValidator();
        var online = new CreateTestingSessionCommand(
            Guid.NewGuid(), "Online", null, scheduled, TimeSpan.FromHours(1), TestingMode.Online,
            null, 5, RegistrationType.Tester);
        var inPerson = online with { Title = "Physical", Mode = TestingMode.InPerson, LocationId = Guid.NewGuid() };
        var hybrid = online with { Title = "Hybrid", Mode = TestingMode.Hybrid, LocationId = Guid.NewGuid() };

        validator.TestValidate(online).IsValid.Should().BeTrue();
        validator.TestValidate(inPerson).IsValid.Should().BeTrue();
        validator.TestValidate(hybrid).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateTestingSessionValidator_RejectsEachInvalidConstraint()
    {
        var command = new CreateTestingSessionCommand(
            Guid.Empty, string.Empty, null, SystemClock.UtcNow.AddDays(-1), TimeSpan.FromHours(9),
            TestingMode.InPerson, null, 101, RegistrationType.Tester);

        var result = new CreateTestingSessionCommandValidator().TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.TestingRequestId);
        result.ShouldHaveValidationErrorFor(x => x.Title);
        result.ShouldHaveValidationErrorFor(x => x.ScheduledDate);
        result.ShouldHaveValidationErrorFor(x => x.Duration);
        result.ShouldHaveValidationErrorFor(x => x.MaxParticipants);
        result.ShouldHaveValidationErrorFor(x => x.LocationId);
    }

    [Fact]
    public void CreateTestingSessionValidator_RejectsZeroDurationAndParticipants()
    {
        var command = new CreateTestingSessionCommand(
            Guid.NewGuid(), "Session", null, SystemClock.UtcNow.AddDays(1), TimeSpan.Zero,
            TestingMode.Online, null, 0, RegistrationType.Tester);

        var result = new CreateTestingSessionCommandValidator().TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Duration);
        result.ShouldHaveValidationErrorFor(x => x.MaxParticipants);
    }

    [Fact]
    public void SubmitFeedbackValidator_CoversValidOptionalValues()
    {
        var validator = new SubmitFeedbackCommandValidator();
        var withoutRating = new SubmitFeedbackCommand(
            Guid.NewGuid(), Guid.NewGuid(), "Useful feedback", FeedbackQuality.High, null);
        var withRating = withoutRating with { Rating = 10 };

        validator.TestValidate(withoutRating).IsValid.Should().BeTrue();
        validator.TestValidate(withRating).IsValid.Should().BeTrue();
    }

    [Fact]
    public void SubmitFeedbackValidator_RejectsEachInvalidConstraint()
    {
        var command = new SubmitFeedbackCommand(
            Guid.Empty, Guid.Empty, new string('x', 5001), null, 11);

        var result = new SubmitFeedbackCommandValidator().TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.TestingRequestId);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
        result.ShouldHaveValidationErrorFor(x => x.Content);
        result.ShouldHaveValidationErrorFor(x => x.Rating);
    }

    [Fact]
    public void SubmitFeedbackValidator_RejectsEmptyContentAndLowRating()
    {
        var command = new SubmitFeedbackCommand(Guid.NewGuid(), Guid.NewGuid(), string.Empty, null, 0);

        var result = new SubmitFeedbackCommandValidator().TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Content);
        result.ShouldHaveValidationErrorFor(x => x.Rating);
    }
}
