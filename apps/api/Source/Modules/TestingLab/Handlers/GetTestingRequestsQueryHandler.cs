namespace GameGuild.Modules.TestingLab;

public class GetTestingRequestsQueryHandler : ITestingLabQueryHandler<GetTestingRequestsQuery, IEnumerable<TestingRequest>> {
  private readonly ITestingRequestService _service;

  public GetTestingRequestsQueryHandler(ITestingRequestService service) { _service = service; }

  public async Task<IEnumerable<TestingRequest>> Handle(GetTestingRequestsQuery request, CancellationToken cancellationToken) {
    // Apply filtering based on query parameters
    var requests = await _service.GetWithPaginationAsync(request.Skip, request.Take);

    if (request.ProjectVersionId.HasValue) { requests = requests.Where(r => r.ProjectVersionId == request.ProjectVersionId.Value); }

    if (request.Status.HasValue) { requests = requests.Where(r => r.Status == request.Status.Value); }

    return requests;
  }
}
