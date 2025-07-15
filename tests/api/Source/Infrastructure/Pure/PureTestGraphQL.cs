using GameGuild.Common;
using HotChocolate;
using HotChocolate.Types;
using MediatR;


namespace GameGuild.Tests.Infrastructure.Pure;

/// <summary>
/// Pure GraphQL queries for testing infrastructure without business modules
/// </summary>
[ExtendObjectType<Query>]
public class PureTestQueries
{
    /// <summary>
    /// Test query that returns a simple message to verify GraphQL→CQRS works
    /// </summary>
    public async Task<string> GetPureTestMessage([Service] IMediator mediator)
    {
        return await mediator.Send(new GetPureTestMessageQuery());
    }

    /// <summary>
    /// Test query that returns a list of test items to verify GraphQL→CQRS→Collection works
    /// </summary>
    public async Task<IEnumerable<PureTestItem>> GetPureTestItems(
        [Service] IMediator mediator,
        int take = 3)
    {
        return await mediator.Send(new GetPureTestItemsQuery { Take = take });
    }
}

/// <summary>
/// GraphQL type definition for PureTestItem
/// </summary>
public class PureTestItemType : ObjectType<PureTestItem>
{
    protected override void Configure(IObjectTypeDescriptor<PureTestItem> descriptor)
    {
        descriptor.Description("A pure test item for infrastructure testing");
        
        descriptor.Field(t => t.Id)
            .Description("The unique identifier of the test item");
            
        descriptor.Field(t => t.Name)
            .Description("The name of the test item");
            
        descriptor.Field(t => t.CreatedAt)
            .Description("When the test item was created");
    }
}
