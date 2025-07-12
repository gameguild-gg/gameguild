using MediatR;

namespace GameGuild.Tests.MockModules;

/// <summary>
/// Handler for GetTestItemsQuery - simulates real CQRS behavior
/// </summary>
public class GetTestItemsHandler : IRequestHandler<GetTestItemsQuery, IEnumerable<TestItem>>
{
    public Task<IEnumerable<TestItem>> Handle(GetTestItemsQuery request, CancellationToken cancellationToken)
    {
        // Simulate some test data
        var items = new List<TestItem>
        {
            new TestItem
            {
                Id = Guid.NewGuid(),
                Name = "Test Item 1",
                Description = "First test item for infrastructure testing",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                IsActive = true
            },
            new TestItem
            {
                Id = Guid.NewGuid(),
                Name = "Test Item 2", 
                Description = "Second test item for infrastructure testing",
                CreatedAt = DateTime.UtcNow.AddHours(-12),
                IsActive = true
            },
            new TestItem
            {
                Id = Guid.NewGuid(),
                Name = "Inactive Test Item",
                Description = "Inactive test item",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                IsActive = false
            }
        };

        // Apply filtering based on request
        var filtered = items.AsEnumerable();
        
        if (!request.IncludeInactive)
        {
            filtered = filtered.Where(item => item.IsActive);
        }

        return Task.FromResult(filtered.Take(request.Take));
    }
}

/// <summary>
/// Handler for GetTestItemByIdQuery - simulates real CQRS behavior
/// </summary>
public class GetTestItemByIdHandler : IRequestHandler<GetTestItemByIdQuery, TestItem?>
{
    public Task<TestItem?> Handle(GetTestItemByIdQuery request, CancellationToken cancellationToken)
    {
        // Simulate finding an item by ID
        if (request.Id != Guid.Empty)
        {
            return Task.FromResult<TestItem?>(new TestItem
            {
                Id = request.Id,
                Name = $"Test Item {request.Id.ToString()[..8]}",
                Description = $"Mock test item with ID {request.Id}",
                CreatedAt = DateTime.UtcNow.AddHours(-6),
                IsActive = true
            });
        }

        return Task.FromResult<TestItem?>(null);
    }
}
