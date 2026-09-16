using FluentAssertions;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Services;

public sealed class ProgramPricingMetadataTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{not-json")]
    public void Read_WhenMetadataIsUnavailable_ReturnsDisabledPricing(string? metadata)
    {
        var program = new Program { Metadata = metadata };

        ProgramPricingMetadata.Read(program).Should().Be(new PricingDto(0m, "USD", false, null, false));
    }

    [Theory]
    [InlineData("{\"other\":{}}")]
    [InlineData("{\"pricing\":\"free\"}")]
    public void Read_WhenPricingObjectIsUnavailable_ReturnsDisabledPricing(string metadata)
    {
        var program = new Program { Metadata = metadata };

        ProgramPricingMetadata.Read(program).Should().Be(new PricingDto(0m, "USD", false, null, false));
    }

    [Fact]
    public void Read_WhenPropertiesAreMissing_UsesSafeDefaults()
    {
        var program = new Program { Metadata = "{\"pricing\":{}}" };

        ProgramPricingMetadata.Read(program).Should().Be(new PricingDto(0m, "USD", false, null, false));
    }

    [Fact]
    public void Read_WhenPropertiesHaveWrongKinds_UsesSafeDefaults()
    {
        var program = new Program
        {
            Metadata = """
                       {"pricing":{"price":"free","currency":12,"isSubscription":"yes","subscriptionDurationDays":"monthly","isMonetizationEnabled":[]}}
                       """
        };

        ProgramPricingMetadata.Read(program).Should().Be(new PricingDto(0m, "USD", false, null, false));
    }

    [Fact]
    public void Read_WhenNumericValuesOverflow_UsesSafeDefaults()
    {
        var program = new Program
        {
            Metadata = """
                       {"pricing":{"price":1e999,"currency":"CAD","isSubscription":true,"subscriptionDurationDays":2147483648,"isMonetizationEnabled":true}}
                       """
        };

        ProgramPricingMetadata.Read(program).Should().Be(new PricingDto(0m, "CAD", true, null, true));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Read_WhenPricingIsValid_ReturnsPersistedValues(bool subscription, bool enabled)
    {
        var program = new Program
        {
            Metadata = "{\"pricing\":{\"price\":19.95,\"currency\":\"BRL\",\"isSubscription\":"
                + subscription.ToString().ToLowerInvariant()
                + ",\"subscriptionDurationDays\":30,\"isMonetizationEnabled\":"
                + enabled.ToString().ToLowerInvariant()
                + "}}"
        };

        ProgramPricingMetadata.Read(program).Should().Be(
            new PricingDto(19.95m, "BRL", subscription, subscription ? 30 : null, enabled));
    }

    [Fact]
    public void Enable_NormalizesUnsafeValuesAndPreservesOtherMetadata()
    {
        var program = new Program();
        program.SetMetadata("catalog", "featured");

        var result = ProgramPricingMetadata.Enable(program, new MonetizationDto(-10m, "  ", false, 365));

        result.Should().Be(new PricingDto(0m, "USD", false, null, true));
        program.Metadata.Should().Contain("catalog");
        ProgramPricingMetadata.Read(program).Should().Be(result);
    }

    [Fact]
    public void Enable_NormalizesCurrencyAndKeepsSubscriptionDuration()
    {
        var program = new Program();

        var result = ProgramPricingMetadata.Enable(program, new MonetizationDto(49.99m, " brl ", true, 90));

        result.Should().Be(new PricingDto(49.99m, "BRL", true, 90, true));
        ProgramPricingMetadata.Read(program).Should().Be(result);
    }

    [Fact]
    public void Disable_PreservesPricingWhileTurningMonetizationOff()
    {
        var program = new Program();
        ProgramPricingMetadata.Enable(program, new MonetizationDto(12m, "eur", true, 30));

        var result = ProgramPricingMetadata.Disable(program);

        result.Should().Be(new PricingDto(12m, "EUR", true, 30, false));
        ProgramPricingMetadata.Read(program).Should().Be(result);
    }

    [Fact]
    public void Update_WhenFieldsAreOmitted_PreservesCurrentPricing()
    {
        var program = new Program();
        var original = ProgramPricingMetadata.Enable(program, new MonetizationDto(15m, "GBP", true, 60));

        var result = ProgramPricingMetadata.Update(program, new UpdatePricingDto());

        result.Should().Be(original);
    }

    [Fact]
    public void Update_WhenSubscriptionIsDisabled_ClearsDurationAndNormalizesFields()
    {
        var program = new Program();
        ProgramPricingMetadata.Enable(program, new MonetizationDto(15m, "GBP", true, 60));

        var result = ProgramPricingMetadata.Update(program, new UpdatePricingDto(-5m, " cad ", false, 365));

        result.Should().Be(new PricingDto(0m, "CAD", false, null, true));
        ProgramPricingMetadata.Read(program).Should().Be(result);
    }

    [Fact]
    public void Update_WhenSubscriptionIsEnabledWithoutDuration_LeavesDurationUnset()
    {
        var program = new Program();
        ProgramPricingMetadata.Enable(program, new MonetizationDto(20m, "USD"));

        var result = ProgramPricingMetadata.Update(program, new UpdatePricingDto(IsSubscription: true));

        result.Should().Be(new PricingDto(20m, "USD", true, null, true));
    }

    [Fact]
    public void Update_WhenSubscriptionDurationIsProvided_UsesTheNewDuration()
    {
        var program = new Program();
        ProgramPricingMetadata.Enable(program, new MonetizationDto(20m, "USD", true, 30));

        var result = ProgramPricingMetadata.Update(program, new UpdatePricingDto(SubscriptionDurationDays: 365));

        result.Should().Be(new PricingDto(20m, "USD", true, 365, true));
    }
}
