using MediatR;

namespace GameGuild.Tests.MockModules;

/// <summary>
/// Simple query for testing CQRS infrastructure
/// </summary>
public class GetTestItemsQuery : IRequest<IEnumerable<TestItem>> {
  public int Take { get; set; } = 10;
  public bool IncludeInactive { get; set; } = false;
}

/// <summary>
/// Get a single test item by ID
/// </summary>
public class GetTestItemByIdQuery : IRequest<TestItem?> {
  public Guid Id { get; set; }
}
