using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Queries;

public class GetWebhookEventQueryValidatorTests
{
    [Fact]
    public void Validator_Should_Reject_Empty_Id()
    {
        var validator = new GetWebhookEventQueryValidator();

        var result = validator.Validate(new GetWebhookEventQuery(""));

        result.IsValid.Should().BeFalse();
    }
}