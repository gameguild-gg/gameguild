namespace GameGuild.Modules.TestingLab;

/// <summary>
/// GraphQL type definition for TestingSession entity
/// </summary>
public class TestingSessionType : ObjectType<TestingSession> {
  protected override void Configure(IObjectTypeDescriptor<TestingSession> descriptor) {
    descriptor.Name("TestingSession");
    descriptor.Description("Represents a testing session in the TestingLab system.");

    descriptor.Field(p => p.Id)
              .Type<NonNullType<UuidType>>()
              .Description("The unique identifier for the testing session.");

    descriptor.Field(p => p.TestingRequestId)
              .Type<NonNullType<UuidType>>()
              .Description("The testing request this session belongs to.");

    descriptor.Field(p => p.LocationId).Type<UuidType>().Description("The location where testing will take place.");

    descriptor.Field(p => p.SessionName).Type<NonNullType<StringType>>().Description("Name of the testing session.");

    descriptor.Field(p => p.SessionDate).Type<NonNullType<DateTimeType>>().Description("Date of the testing session.");

    descriptor.Field(p => p.StartTime).Type<NonNullType<DateTimeType>>().Description("Start time of the session.");

    descriptor.Field(p => p.EndTime).Type<NonNullType<DateTimeType>>().Description("End time of the session.");

    descriptor.Field(p => p.MaxTesters).Type<IntType>().Description("Maximum testers for this session.");

    descriptor.Field(p => p.Status)
              .Type<NonNullType<EnumType<SessionStatus>>>()
              .Description("Current status of the session.");

    descriptor.Field(p => p.ManagerUserId).Type<UuidType>().Description("User managing this session.");

    descriptor.Field(p => p.CreatedById)
              .Type<NonNullType<UuidType>>()
              .Description("ID of the user who created this session.");

    descriptor.Field(p => p.CreatedAt).Type<NonNullType<DateTimeType>>().Description("When the session was created.");

    descriptor.Field(p => p.UpdatedAt).Type<DateTimeType>().Description("When the session was last updated.");
  }
}
