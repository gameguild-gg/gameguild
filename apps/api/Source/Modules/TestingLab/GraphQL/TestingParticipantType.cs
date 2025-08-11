namespace GameGuild.Modules.TestingLab;

/// <summary>
/// GraphQL type definition for TestingParticipant entity
/// </summary>
public class TestingParticipantType : ObjectType<TestingParticipant> {
  protected override void Configure(IObjectTypeDescriptor<TestingParticipant> descriptor) {
    descriptor.Name("TestingParticipant");
    descriptor.Description("Represents a participant in a testing request.");

    descriptor.Field(p => p.Id).Type<NonNullType<UuidType>>().Description("The unique identifier for the participant.");

    descriptor.Field(p => p.TestingRequestId)
              .Type<NonNullType<UuidType>>()
              .Description("The testing request this participant is part of.");

    descriptor.Field(p => p.UserId).Type<NonNullType<UuidType>>().Description("The user ID of the participant.");

    descriptor.Field(p => p.InstructionsAcknowledged)
              .Type<NonNullType<BooleanType>>()
              .Description("Whether the participant has acknowledged instructions.");

    descriptor.Field(p => p.InstructionsAcknowledgedAt)
              .Type<DateTimeType>()
              .Description("When the participant acknowledged instructions.");

    descriptor.Field(p => p.StartedAt)
              .Type<NonNullType<DateTimeType>>()
              .Description("When the participant started testing.");

    descriptor.Field(p => p.CompletedAt).Type<DateTimeType>().Description("When the participant completed testing.");
  }
}
