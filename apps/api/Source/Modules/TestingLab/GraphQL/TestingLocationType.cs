namespace GameGuild.Modules.TestingLab;

/// <summary>
/// GraphQL type definition for TestingLocation entity
/// </summary>
public class TestingLocationType : ObjectType<TestingLocation> {
  protected override void Configure(IObjectTypeDescriptor<TestingLocation> descriptor) {
    descriptor.Name("TestingLocation");
    descriptor.Description("Represents a testing location in the TestingLab system.");

    descriptor.Field(p => p.Id).Type<NonNullType<UuidType>>().Description("The unique identifier for the location.");

    descriptor.Field(p => p.Name).Type<NonNullType<StringType>>().Description("Name of the testing location.");

    descriptor.Field(p => p.MaxTestersCapacity)
              .Type<IntType>()
              .Description("Maximum number of testers this location can accommodate.");

    descriptor.Field(p => p.MaxProjectsCapacity)
              .Type<IntType>()
              .Description("Maximum number of projects that can be tested simultaneously.");

    descriptor.Field(p => p.Status)
              .Type<NonNullType<EnumType<LocationStatus>>>()
              .Description("Current status of the location.");

    descriptor.Field(p => p.CreatedAt).Type<NonNullType<DateTimeType>>().Description("When the location was created.");

    descriptor.Field(p => p.UpdatedAt).Type<DateTimeType>().Description("When the location was last updated.");
  }
}
