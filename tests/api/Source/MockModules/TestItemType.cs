using HotChocolate.Types;

namespace GameGuild.Tests.MockModules;

/// <summary>
/// GraphQL type definition for TestItem
/// </summary>
public class TestItemType : ObjectType<TestItem>
{
    protected override void Configure(IObjectTypeDescriptor<TestItem> descriptor)
    {
        descriptor.Field(t => t.Id).Type<NonNullType<UuidType>>();
        descriptor.Field(t => t.Name).Type<NonNullType<StringType>>();
        descriptor.Field(t => t.Description).Type<StringType>();
        descriptor.Field(t => t.IsActive).Type<NonNullType<BooleanType>>();
        descriptor.Field(t => t.CreatedAt).Type<NonNullType<DateTimeType>>();
    }
}
