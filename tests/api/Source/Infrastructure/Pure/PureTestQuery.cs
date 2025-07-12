using HotChocolate;
using HotChocolate.Types;
using MediatR;

namespace GameGuild.API.Tests.Infrastructure.Pure;

/// <summary>
/// Simple pure test query type for testing GraphQL infrastructure
/// </summary>
public class PureTestQuery : ObjectType<PureTestQueryResolver>
{
    protected override void Configure(IObjectTypeDescriptor<PureTestQueryResolver> descriptor)
    {
        descriptor.Name("Query"); // Set the proper GraphQL type name
        
        descriptor.Field(f => f.GetPureTestMessage(default!))
                  .Name("getPureTestMessage")
                  .Description("Test query that returns a simple message to verify GraphQL→CQRS works");

        descriptor.Field(f => f.GetPureTestItems(default!, default))
                  .Name("getPureTestItems")
                  .Description("Test query that returns a list of test items to verify GraphQL→CQRS→Collection works")
                  .Argument("take", a => a.Type<IntType>().DefaultValue(3));
    }
}

/// <summary>
/// Resolver for pure test queries
/// </summary>
public class PureTestQueryResolver
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
