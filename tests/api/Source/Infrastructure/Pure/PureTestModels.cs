using MediatR;


namespace GameGuild.Tests.Infrastructure.Pure;

/// <summary>
/// Minimal test item for pure infrastructure testing
/// </summary>
public class PureTestItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Test Item";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Simple query for testing CQRS infrastructure without business logic
/// </summary>
public class GetPureTestItemsQuery : IRequest<IEnumerable<PureTestItem>>
{
    public int Take { get; set; } = 5;
}

/// <summary>
/// Handler for pure test items query - simulates real CQRS behavior
/// </summary>
public class GetPureTestItemsHandler : IRequestHandler<GetPureTestItemsQuery, IEnumerable<PureTestItem>>
{
    public Task<IEnumerable<PureTestItem>> Handle(GetPureTestItemsQuery request, CancellationToken cancellationToken)
    {
        var items = Enumerable.Range(1, request.Take)
            .Select(i => new PureTestItem 
            { 
                Id = $"test-{i}",
                Name = $"Pure Test Item {i}"
            });

        return Task.FromResult(items);
    }
}

/// <summary>
/// Simple query that returns a test message
/// </summary>
public class GetPureTestMessageQuery : IRequest<string>
{
}

/// <summary>
/// Handler for test message query
/// </summary>
public class GetPureTestMessageHandler : IRequestHandler<GetPureTestMessageQuery, string>
{
    public Task<string> Handle(GetPureTestMessageQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult("Hello from pure infrastructure test!");
    }
}
