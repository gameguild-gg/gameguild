using GameGuild.Common;
using HotChocolate.Types;
using MediatR;

namespace GameGuild.Tests.MockModules;

/// <summary>
/// GraphQL queries for the test module - demonstrates GraphQL → CQRS → MediatR integration
/// This class extends the root Query type to add test-specific fields
/// </summary>
public class TestModuleQueries : ObjectTypeExtension<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor.Field("getTestMessage")
            .Type<StringType>()
            .Resolve(() => Task.FromResult("Hello from GraphQL infrastructure test!"));

        descriptor.Field("getTestItems")
            .Type<ListType<TestItemType>>()
            .Argument("take", a => a.Type<IntType>().DefaultValue(10))
            .Argument("includeInactive", a => a.Type<BooleanType>().DefaultValue(false))
            .Resolve(async context =>
            {
                var mediator = context.Service<IMediator>();
                var take = context.ArgumentValue<int>("take");
                var includeInactive = context.ArgumentValue<bool>("includeInactive");
                
                var query = new GetTestItemsQuery 
                { 
                    Take = take, 
                    IncludeInactive = includeInactive,
                };
                
                return await mediator.Send(query);
            });

        descriptor.Field("getTestItemById")
            .Type<TestItemType>()
            .Argument("id", a => a.Type<NonNullType<UuidType>>())
            .Resolve(async context =>
            {
                var mediator = context.Service<IMediator>();
                var id = context.ArgumentValue<Guid>("id");
                
                var query = new GetTestItemByIdQuery { Id = id };
                
                return await mediator.Send(query);
            });
    }
}
