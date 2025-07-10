using GameGuild.Modules.Projects;
using GameGuild.Modules.Users;


namespace GameGuild.Modules.TestingLab;

/// <summary>
/// GraphQL type definition for TestingRequest entity
/// </summary>
public class TestingRequestType : ObjectType<TestingRequest> {
  protected override void Configure(IObjectTypeDescriptor<TestingRequest> descriptor) {
    descriptor.Name("TestingRequest");
    descriptor.Description("Represents a testing request in the TestingLab system.");

    // Base Entity Properties
    descriptor.Field(p => p.Id)
              .Type<NonNullType<UuidType>>()
              .Description("The unique identifier for the testing request (UUID).");

    descriptor.Field(p => p.CreatedAt)
              .Type<NonNullType<DateTimeType>>()
              .Description("When the testing request was created.");

    descriptor.Field(p => p.UpdatedAt).Type<DateTimeType>().Description("When the testing request was last updated.");

    // Core Properties
    descriptor.Field(p => p.ProjectVersionId)
              .Type<NonNullType<UuidType>>()
              .Description("The project version ID this testing request is for.");

    descriptor.Field(p => p.Title).Type<NonNullType<StringType>>().Description("The title of the testing request.");

    descriptor.Field(p => p.Description).Type<StringType>().Description("Description of what needs to be tested.");

    descriptor.Field(p => p.InstructionsType)
              .Type<NonNullType<EnumType<InstructionType>>>()
              .Description("Type of instructions provided (Text, Video, etc.)");

    descriptor.Field(p => p.InstructionsContent).Type<StringType>().Description("Text instructions for testing.");

    descriptor.Field(p => p.InstructionsUrl).Type<StringType>().Description("URL to external instructions.");

    descriptor.Field(p => p.InstructionsFileId).Type<UuidType>().Description("File ID for instruction documents.");

    descriptor.Field(p => p.MaxTesters).Type<IntType>().Description("Maximum number of testers allowed.");

    descriptor.Field(p => p.CurrentTesterCount)
              .Type<NonNullType<IntType>>()
              .Description("Current number of registered testers.");

    descriptor.Field(p => p.StartDate).Type<NonNullType<DateTimeType>>().Description("When testing should start.");

    descriptor.Field(p => p.EndDate).Type<NonNullType<DateTimeType>>().Description("When testing should end.");

    descriptor.Field(p => p.Status)
              .Type<NonNullType<EnumType<TestingRequestStatus>>>()
              .Description("Current status of the testing request.");

    descriptor.Field(p => p.CreatedById)
              .Type<NonNullType<UuidType>>()
              .Description("ID of the user who created this testing request.");

    // Navigation Properties
    descriptor.Field("projectVersion")
              .ResolveWith<TestingRequestResolvers>(r => r.GetProjectVersion(default!, default!))
              .Type<ObjectType<ProjectVersion>>()
              .Description("The project version being tested.");

    descriptor.Field("createdBy")
              .ResolveWith<TestingRequestResolvers>(r => r.GetCreatedBy(default!, default!))
              .Type<ObjectType<User>>()
              .Description("The user who created this testing request.");

    descriptor.Field("participants")
              .ResolveWith<TestingRequestResolvers>(r => r.GetParticipants(default!, default!))
              .Type<ListType<TestingParticipantType>>()
              .Description("Users participating in this testing request.");

    descriptor.Field("sessions")
              .ResolveWith<TestingRequestResolvers>(r => r.GetSessions(default!, default!))
              .Type<ListType<TestingSessionType>>()
              .Description("Testing sessions for this request.");
  }
}
