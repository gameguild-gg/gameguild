using FluentAssertions;
using GameGuild.Localization;
using Xunit;

namespace GameGuild.Tests.Localization.Unit;

/// <summary>
/// Unit tests for the LocalizationStatus enum
/// </summary>
public class LocalizationStatusTests
{
    [Fact]
    public void LocalizationStatus_Should_Have_Correct_Default_Value()
    {
        // Arrange
        LocalizationStatus defaultStatus = default;

        // Act & Assert
        defaultStatus.Should().Be(LocalizationStatus.Draft);
        ((int)defaultStatus).Should().Be(0);
    }

    [Theory]
    [InlineData(LocalizationStatus.Draft, 0)]
    [InlineData(LocalizationStatus.Published, 1)]
    [InlineData(LocalizationStatus.NeedsReview, 2)]
    [InlineData(LocalizationStatus.Archived, 3)]
    [InlineData(LocalizationStatus.MachineTranslated, 4)]
    public void LocalizationStatus_Should_Have_Correct_Values(LocalizationStatus status, int expectedValue)
    {
        // Act & Assert
        ((int)status).Should().Be(expectedValue);
    }

    [Fact]
    public void LocalizationStatus_Should_Support_All_Workflow_States()
    {
        // Arrange & Act
        var allStatuses = Enum.GetValues<LocalizationStatus>();

        // Assert
        allStatuses.Should().HaveCount(5);
        allStatuses.Should().Contain(LocalizationStatus.Draft);
        allStatuses.Should().Contain(LocalizationStatus.Published);
        allStatuses.Should().Contain(LocalizationStatus.NeedsReview);
        allStatuses.Should().Contain(LocalizationStatus.Archived);
        allStatuses.Should().Contain(LocalizationStatus.MachineTranslated);
    }

    [Theory]
    [InlineData(0, LocalizationStatus.Draft)]
    [InlineData(1, LocalizationStatus.Published)]
    [InlineData(2, LocalizationStatus.NeedsReview)]
    [InlineData(3, LocalizationStatus.Archived)]
    [InlineData(4, LocalizationStatus.MachineTranslated)]
    public void Int_To_LocalizationStatus_Conversion_Should_Work(int value, LocalizationStatus expectedStatus)
    {
        // Act
        var status = (LocalizationStatus)value;

        // Assert
        status.Should().Be(expectedStatus);
    }

    [Theory]
    [InlineData(LocalizationStatus.Draft, "Draft")]
    [InlineData(LocalizationStatus.Published, "Published")]
    [InlineData(LocalizationStatus.NeedsReview, "NeedsReview")]
    [InlineData(LocalizationStatus.Archived, "Archived")]
    [InlineData(LocalizationStatus.MachineTranslated, "MachineTranslated")]
    public void ToString_Should_Return_Correct_String_Representation(LocalizationStatus status, string expectedString)
    {
        // Act
        var result = status.ToString();

        // Assert
        result.Should().Be(expectedString);
    }

    [Theory]
    [InlineData("Draft", LocalizationStatus.Draft)]
    [InlineData("Published", LocalizationStatus.Published)]
    [InlineData("NeedsReview", LocalizationStatus.NeedsReview)]
    [InlineData("Archived", LocalizationStatus.Archived)]
    [InlineData("MachineTranslated", LocalizationStatus.MachineTranslated)]
    public void Parse_Should_Convert_String_To_Status(string statusString, LocalizationStatus expectedStatus)
    {
        // Act
        var success = Enum.TryParse<LocalizationStatus>(statusString, out var result);

        // Assert
        success.Should().BeTrue();
        result.Should().Be(expectedStatus);
    }

    [Theory]
    [InlineData("draft")]
    [InlineData("PUBLISHED")]
    [InlineData("needsreview")]
    public void Parse_Should_Be_Case_Insensitive(string statusString)
    {
        // Act
        var success = Enum.TryParse<LocalizationStatus>(statusString, true, out var result);

        // Assert
        success.Should().BeTrue();
        result.Should().BeOneOf(Enum.GetValues<LocalizationStatus>());
    }

    [Theory]
    [InlineData("InvalidStatus")]
    [InlineData("")]
    [InlineData(" ")]
    public void Parse_Should_Fail_For_Invalid_Values(string invalidStatus)
    {
        // Act
        var success = Enum.TryParse<LocalizationStatus>(invalidStatus, out var result);

        // Assert
        success.Should().BeFalse();
        result.Should().Be(default(LocalizationStatus));
    }

    [Theory]
    [InlineData("123")]  // Numeric values that don't correspond to defined enum values
    [InlineData("999")]
    [InlineData("-1")]
    public void Parse_Should_Succeed_For_Numeric_Values_But_May_Not_Be_Defined(string numericStatus)
    {
        // Act
        var success = Enum.TryParse<LocalizationStatus>(numericStatus, out var result);

        // Assert - TryParse succeeds for numeric strings but result may not be a defined value
        success.Should().BeTrue();

        // Check if the result is a defined enum value
        var isDefined = Enum.IsDefined(typeof(LocalizationStatus), result);
        if (numericStatus == "123" || numericStatus == "999" || numericStatus == "-1")
        {
            isDefined.Should().BeFalse("because these numeric values don't correspond to defined enum values");
        }
    }

    [Fact]
    public void LocalizationStatus_Should_Be_Comparable()
    {
        // Arrange
        var draft = LocalizationStatus.Draft;
        var published = LocalizationStatus.Published;
        var needsReview = LocalizationStatus.NeedsReview;

        // Act & Assert
        (draft < published).Should().BeTrue();
        (published < needsReview).Should().BeTrue();
        (draft < needsReview).Should().BeTrue();
        (published > draft).Should().BeTrue();
    }

    [Fact]
    public void LocalizationStatus_Should_Support_Equality_Comparison()
    {
        // Arrange
        var status1 = LocalizationStatus.Published;
        var status2 = LocalizationStatus.Published;
        var status3 = LocalizationStatus.Draft;

        // Act & Assert
        status1.Should().Be(status2);
        status1.Should().NotBe(status3);
        (status1 == status2).Should().BeTrue();
        (status1 != status3).Should().BeTrue();
    }

    [Fact]
    public void LocalizationStatus_Should_Have_Distinct_Values()
    {
        // Arrange
        var allStatuses = Enum.GetValues<LocalizationStatus>().ToList();

        // Act
        var distinctStatuses = allStatuses.Distinct().ToList();

        // Assert
        distinctStatuses.Should().HaveCount(allStatuses.Count);
        distinctStatuses.Should().BeEquivalentTo(allStatuses);
    }

    [Fact]
    public void LocalizationStatus_Workflow_Should_Follow_Logical_Order()
    {
        // Arrange & Act
        var workflowOrder = new[]
        {
            LocalizationStatus.Draft,
            LocalizationStatus.Published,
            LocalizationStatus.NeedsReview,
            LocalizationStatus.Archived,
            LocalizationStatus.MachineTranslated
        };

        // Assert - Check that the enum values follow a logical numerical progression
        for (int i = 0; i < workflowOrder.Length; i++)
        {
            ((int)workflowOrder[i]).Should().Be(i);
        }
    }
}