namespace GameGuild.Modules.TestingLab;

public class GetTestingRequestQueryHandler : ITestingLabQueryHandler<GetTestingRequestQuery, TestingRequest?> {
  private readonly ITestingRequestService _service;

  public GetTestingRequestQueryHandler(ITestingRequestService service) { _service = service; }

  public async Task<TestingRequest?> Handle(GetTestingRequestQuery request, CancellationToken cancellationToken) { return await _service.GetByIdWithDetailsAsync(request.Id); }
}
