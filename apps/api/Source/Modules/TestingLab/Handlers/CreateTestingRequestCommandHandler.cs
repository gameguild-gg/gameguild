using MediatR;
using GameGuild.Modules.TestingLab.Abstractions;

namespace GameGuild.Modules.TestingLab.Handlers;

public class CreateTestingRequestCommandHandler : ITestingLabCommandHandler<CreateTestingRequestCommand, TestingRequest>
{
    private readonly ITestingRequestRepository _repository;
    private readonly ITestingRequestService _service;
    private readonly IMediator _mediator;

    public CreateTestingRequestCommandHandler(
        ITestingRequestRepository repository,
        ITestingRequestService service,
        IMediator mediator)
    {
        _repository = repository;
        _service = service;
        _mediator = mediator;
    }

    public async Task<TestingRequest> Handle(CreateTestingRequestCommand request, CancellationToken cancellationToken)
    {
        var testingRequest = new TestingRequest
        {
            Id = Guid.NewGuid(),
            ProjectVersionId = request.ProjectVersionId,
            Title = request.Title,
            Description = request.Description,
            DownloadUrl = request.DownloadUrl,
            InstructionsType = request.InstructionsType,
            InstructionsContent = request.InstructionsContent,
            InstructionsUrl = request.InstructionsUrl,
            InstructionsFileId = request.InstructionsFileId,
            FeedbackFormContent = request.FeedbackFormContent,
            MaxTesters = request.MaxTesters,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = TestingRequestStatus.Draft,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        var createdRequest = await _service.CreateAsync(testingRequest);

        // Publish domain event
        var domainEvent = new TestingRequestCreatedEvent(
            createdRequest.Id,
            createdRequest.ProjectVersionId,
            createdRequest.Title,
            createdRequest.CreatedByUserId ?? Guid.Empty,
            createdRequest.CreatedAt
        );

        await _mediator.Publish(domainEvent, cancellationToken);

        return createdRequest;
    }
}
