using GameGuild.Modules.TestingLab.Models;

namespace GameGuild.API.Tests.Source.Tests.Modules.TestingLab.Unit.Models;

public class TestingLabModelsTests {
  [Fact]
  public void TestingRequest_Properties_SetCorrectly() {
    // Arrange & Act
    var request = new TestingRequest {
      Id = Guid.NewGuid(),
      Title = "Test Request",
      Description = "Test Description",
      InstructionsType = InstructionType.Text,
      InstructionsContent = "Test instructions",
      MaxTesters = 5,
      StartDate = DateTime.UtcNow,
      EndDate = DateTime.UtcNow.AddDays(7),
      Status = TestingRequestStatus.Open,
    };

    // Assert
    Assert.NotEqual(Guid.Empty, request.Id);
    Assert.Equal("Test Request", request.Title);
    Assert.Equal("Test Description", request.Description);
    Assert.Equal(InstructionType.Text, request.InstructionsType);
    Assert.Equal("Test instructions", request.InstructionsContent);
    Assert.Equal(5, request.MaxTesters);
    Assert.Equal(TestingRequestStatus.Open, request.Status);
  }

  [Fact]
  public void TestingSession_Properties_SetCorrectly() {
    // Arrange & Act
    var session = new TestingSession {
      Id = Guid.NewGuid(),
      SessionName = "Test Session",
      SessionDate = DateTime.UtcNow,
      StartTime = DateTime.UtcNow.AddHours(9),
      EndTime = DateTime.UtcNow.AddHours(17),
      MaxTesters = 10,
      Status = SessionStatus.Scheduled,
    };

    // Assert
    Assert.NotEqual(Guid.Empty, session.Id);
    Assert.Equal("Test Session", session.SessionName);
    Assert.Equal(10, session.MaxTesters);
    Assert.Equal(SessionStatus.Scheduled, session.Status);
    Assert.True(session.EndTime > session.StartTime);
  }

  [Fact]
  public void TestingParticipant_Properties_SetCorrectly() {
    // Arrange & Act
    var participant = new TestingParticipant {
      Id = Guid.NewGuid(),
      TestingRequestId = Guid.NewGuid(),
      UserId = Guid.NewGuid(),
      InstructionsAcknowledged = true,
      StartedAt = DateTime.UtcNow,
    };

    // Assert
    Assert.NotEqual(Guid.Empty, participant.Id);
    Assert.NotEqual(Guid.Empty, participant.TestingRequestId);
    Assert.NotEqual(Guid.Empty, participant.UserId);
    Assert.True(participant.InstructionsAcknowledged);
  }

  [Fact]
  public void SessionRegistration_Properties_SetCorrectly() {
    // Arrange & Act
    var registration = new SessionRegistration {
      Id = Guid.NewGuid(),
      SessionId = Guid.NewGuid(),
      UserId = Guid.NewGuid(),
      RegistrationType = RegistrationType.Tester,
      RegistrationNotes = "Test notes",
    };

    // Assert
    Assert.NotEqual(Guid.Empty, registration.Id);
    Assert.NotEqual(Guid.Empty, registration.SessionId);
    Assert.NotEqual(Guid.Empty, registration.UserId);
    Assert.Equal(RegistrationType.Tester, registration.RegistrationType);
    Assert.Equal("Test notes", registration.RegistrationNotes);
  }

  [Fact]
  public void SessionWaitlist_Properties_SetCorrectly() {
    // Arrange & Act
    var waitlist = new SessionWaitlist {
      Id = Guid.NewGuid(),
      SessionId = Guid.NewGuid(),
      UserId = Guid.NewGuid(),
      RegistrationType = RegistrationType.Tester,
      Position = 1,
    };

    // Assert
    Assert.NotEqual(Guid.Empty, waitlist.Id);
    Assert.NotEqual(Guid.Empty, waitlist.SessionId);
    Assert.NotEqual(Guid.Empty, waitlist.UserId);
    Assert.Equal(RegistrationType.Tester, waitlist.RegistrationType);
    Assert.Equal(1, waitlist.Position);
  }

  [Fact]
  public void TestingFeedback_Properties_SetCorrectly() {
    // Arrange & Act
    var feedback = new TestingFeedback {
      Id = Guid.NewGuid(),
      TestingRequestId = Guid.NewGuid(),
      UserId = Guid.NewGuid(),
      FeedbackFormId = Guid.NewGuid(),
      FeedbackData = "Test feedback data",
      TestingContext = TestingContext.Online,
      AdditionalNotes = "Additional notes",
    };

    // Assert
    Assert.NotEqual(Guid.Empty, feedback.Id);
    Assert.NotEqual(Guid.Empty, feedback.TestingRequestId);
    Assert.NotEqual(Guid.Empty, feedback.UserId);
    Assert.NotEqual(Guid.Empty, feedback.FeedbackFormId);
    Assert.Equal("Test feedback data", feedback.FeedbackData);
    Assert.Equal(TestingContext.Online, feedback.TestingContext);
    Assert.Equal("Additional notes", feedback.AdditionalNotes);
  }

  [Fact]
  public void TestingLocation_Properties_SetCorrectly() {
    // Arrange & Act
    var location = new TestingLocation {
      Id = Guid.NewGuid(),
      Name = "Test Location",
      Description = "Test Description",
      Address = "123 Test St",
      MaxTestersCapacity = 20,
      MaxProjectsCapacity = 5,
      EquipmentAvailable = "Computers, VR headsets",
      Status = LocationStatus.Active,
    };

    // Assert
    Assert.NotEqual(Guid.Empty, location.Id);
    Assert.Equal("Test Location", location.Name);
    Assert.Equal("Test Description", location.Description);
    Assert.Equal("123 Test St", location.Address);
    Assert.Equal(20, location.MaxTestersCapacity);
    Assert.Equal(5, location.MaxProjectsCapacity);
    Assert.Equal("Computers, VR headsets", location.EquipmentAvailable);
    Assert.Equal(LocationStatus.Active, location.Status);
  }

  [Theory]
  [InlineData(TestingRequestStatus.Open)]
  [InlineData(TestingRequestStatus.InProgress)]
  [InlineData(TestingRequestStatus.Completed)]
  [InlineData(TestingRequestStatus.Cancelled)]
  public void TestingRequestStatus_AllValues_AreValid(TestingRequestStatus status) {
    // Arrange & Act
    var request = new TestingRequest { Status = status };

    // Assert
    Assert.Equal(status, request.Status);
  }

  [Theory]
  [InlineData(SessionStatus.Scheduled)]
  [InlineData(SessionStatus.Active)]
  [InlineData(SessionStatus.Completed)]
  [InlineData(SessionStatus.Cancelled)]
  public void SessionStatus_AllValues_AreValid(SessionStatus status) {
    // Arrange & Act
    var session = new TestingSession { Status = status };

    // Assert
    Assert.Equal(status, session.Status);
  }

  [Theory]
  [InlineData(TestingMode.Online)]
  [InlineData(TestingMode.InPerson)]
  [InlineData(TestingMode.Hybrid)]
  public void TestingMode_AllValues_AreValid(TestingMode mode) {
    // Act & Assert - Testing that all enum values are properly defined
    Assert.True(Enum.IsDefined(typeof(TestingMode), mode));
  }

  [Theory]
  [InlineData(InstructionType.Text)]
  [InlineData(InstructionType.File)]
  [InlineData(InstructionType.Url)]
  public void InstructionType_AllValues_AreValid(InstructionType type) {
    // Arrange & Act
    var request = new TestingRequest { InstructionsType = type };

    // Assert
    Assert.Equal(type, request.InstructionsType);
  }

  [Theory]
  [InlineData(RegistrationType.Tester)]
  [InlineData(RegistrationType.ProjectMember)]
  public void RegistrationType_AllValues_AreValid(RegistrationType type) {
    // Arrange & Act
    var registration = new SessionRegistration { RegistrationType = type };

    // Assert
    Assert.Equal(type, registration.RegistrationType);
  }

  [Theory]
  [InlineData(TestingContext.Online)]
  [InlineData(TestingContext.InPerson)]
  public void TestingContext_AllValues_AreValid(TestingContext context) {
    // Arrange & Act
    var feedback = new TestingFeedback { TestingContext = context };

    // Assert
    Assert.Equal(context, feedback.TestingContext);
  }

  [Theory]
  [InlineData(LocationStatus.Active)]
  [InlineData(LocationStatus.Maintenance)]
  [InlineData(LocationStatus.Inactive)]
  public void LocationStatus_AllValues_AreValid(LocationStatus status) {
    // Arrange & Act
    var location = new TestingLocation { Status = status };

    // Assert
    Assert.Equal(status, location.Status);
  }
}
