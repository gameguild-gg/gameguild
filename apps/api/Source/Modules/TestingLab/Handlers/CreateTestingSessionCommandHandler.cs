using GameGuild.Modules.TestingLab.Abstractions;


namespace GameGuild.Modules.TestingLab.Handlers;

public class CreateTestingSessionCommandHandler : ITestingLabCommandHandler<CreateTestingSessionCommand, TestingSession> {
  private readonly ITestingLocationRepository _locationRepository;

  private readonly IMediator _mediator;

  private readonly ITestingRequestService _requestService;

  private readonly ITestingSessionService _sessionService;

  public CreateTestingSessionCommandHandler(
    ITestingSessionService sessionService,
    ITestingRequestService requestService,
    ITestingLocationRepository locationRepository,
    IMediator mediator
  ) {
    _sessionService = sessionService;
    _requestService = requestService;
    _locationRepository = locationRepository;
    _mediator = mediator;
  }

  public async Task<TestingSession> Handle(CreateTestingSessionCommand request, CancellationToken cancellationToken) {
    // Validate the testing request exists
    var testingRequest = await _requestService.GetByIdAsync(request.TestingRequestId);

    if (testingRequest == null) { throw new ArgumentException($"Testing request with ID {request.TestingRequestId} not found"); }

    // Validate location if specified
    if (request.LocationId.HasValue) {
      var locationExists = await _locationRepository.ExistsAsync(request.LocationId.Value, cancellationToken);

      if (!locationExists) { throw new ArgumentException($"Location with ID {request.LocationId} not found"); }
    }

    var testingSession = new TestingSession {
      Id = Guid.NewGuid(),
      TestingRequestId = request.TestingRequestId,
      Title = request.Title,
      Description = request.Description,
      ScheduledDate = request.ScheduledDate,
      Duration = request.Duration,
      Mode = request.Mode,
      LocationId = request.LocationId,
      MaxParticipants = request.MaxParticipants,
      RegistrationType = request.RegistrationType,
      Status = SessionStatus.Scheduled,
      IsActive = request.IsActive,
      CreatedAt = DateTime.UtcNow,
    };

    var createdSession = await _sessionService.CreateAsync(testingSession);

    // Publish domain event
    var domainEvent = new TestingSessionCreatedEvent(
      createdSession.Id,
      createdSession.TestingRequestId,
      createdSession.Title,
      createdSession.ScheduledDate,
      createdSession.CreatedByUserId ?? Guid.Empty,
      createdSession.CreatedAt
    );

    await _mediator.Publish(domainEvent, cancellationToken);

    return createdSession;
  }
}
