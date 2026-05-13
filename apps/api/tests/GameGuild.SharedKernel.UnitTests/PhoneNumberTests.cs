using FluentAssertions;


namespace GameGuild.Tests.SharedKernel.Unit;

public class PhoneNumberTests
{
    [Fact]
    public void Constructor_WithNullOrEmpty_ShouldThrow()
    {
        var act1 = () => new PhoneNumber(null!);
        act1.Should().Throw<ArgumentException>();

        var act2 = () => new PhoneNumber("");
        act2.Should().Throw<ArgumentException>();

        var act3 = () => new PhoneNumber("   ");
        act3.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_InternationalFormat_ShouldParseCorrectly()
    {
        var phone = new PhoneNumber("+14155551234");

        phone.CountryCode.Should().Be("+1");
        phone.NationalNumber.Should().Be("4155551234");
        phone.Value.Should().Be("+14155551234");
    }

    [Fact]
    public void Constructor_InternationalFormat_WithDashes_ShouldClean()
    {
        var phone = new PhoneNumber("+1-415-555-1234");

        phone.CountryCode.Should().Be("+1");
        phone.NationalNumber.Should().Be("4155551234");
        phone.Value.Should().Be("+14155551234");
    }

    [Fact]
    public void Constructor_InternationalFormat_WithSpaces_ShouldClean()
    {
        var phone = new PhoneNumber("+44 20 7946 0958");

        phone.CountryCode.Should().Be("+44");
        phone.NationalNumber.Should().Be("2079460958");
    }

    [Fact]
    public void Constructor_InternationalFormat_WithParentheses_ShouldClean()
    {
        var phone = new PhoneNumber("+1 (415) 555-1234");

        phone.CountryCode.Should().Be("+1");
        phone.NationalNumber.Should().Be("4155551234");
    }

    [Fact]
    public void Constructor_InternationalFormat_TooShort_ShouldThrow()
    {
        var act = () => new PhoneNumber("+12345");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_InternationalFormat_TooLong_ShouldThrow()
    {
        var act = () => new PhoneNumber("+12345678901234567");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_NationalFormat_WithCountryCode_ShouldWork()
    {
        var phone = new PhoneNumber("4155551234", "+1");

        phone.CountryCode.Should().Be("+1");
        phone.NationalNumber.Should().Be("4155551234");
        phone.Value.Should().Be("+14155551234");
    }

    [Fact]
    public void Constructor_NationalFormat_WithoutCountryCode_ShouldThrow()
    {
        var act = () => new PhoneNumber("4155551234");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_NationalFormat_TooShort_ShouldThrow()
    {
        var act = () => new PhoneNumber("123456", "+1");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_NationalFormat_TooLong_ShouldThrow()
    {
        var act = () => new PhoneNumber("1234567890123", "+1");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ImplicitConversion_ToStringShouldReturnValue()
    {
        var phone = new PhoneNumber("+14155551234");
        string value = phone;
        value.Should().Be("+14155551234");
    }

    [Fact]
    public void GetDisplayFormat_USNumber_ShouldFormatWithParentheses()
    {
        var phone = new PhoneNumber("+14155551234");

        phone.GetDisplayFormat().Should().Be("(415) 555-1234");
    }

    [Fact]
    public void GetDisplayFormat_NonUSNumber_ShouldFormatSimply()
    {
        var phone = new PhoneNumber("+442079460958");

        phone.GetDisplayFormat().Should().Be("+44 2079460958");
    }

    [Fact]
    public void ToString_ShouldReturnDisplayFormat()
    {
        var phone = new PhoneNumber("+14155551234");

        phone.ToString().Should().Be("(415) 555-1234");
    }

    [Theory]
    [InlineData("+553182255100", "+55")]
    [InlineData("+491711234567", "+49")]
    [InlineData("+819012345678", "+81")]
    [InlineData("+861391234567", "+86")]
    [InlineData("+74951234567", "+7")]
    public void Constructor_Various_CountryCodes_Parsed_Correctly(string number, string expectedCode)
    {
        var phone = new PhoneNumber(number);
        phone.CountryCode.Should().Be(expectedCode);
    }

    [Fact]
    public void RecordEquality_SameNumber_ShouldBeEqual()
    {
        var phone1 = new PhoneNumber("+14155551234");
        var phone2 = new PhoneNumber("+1-415-555-1234");

        phone1.Should().Be(phone2);
    }
}