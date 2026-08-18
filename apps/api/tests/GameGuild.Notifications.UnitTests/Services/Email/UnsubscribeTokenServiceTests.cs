using GameGuild.Notifications.Services.Email;
using Microsoft.AspNetCore.DataProtection;

namespace GameGuild.Notifications.UnitTests.Services.Email;

public class UnsubscribeTokenServiceTests
{
    private readonly UnsubscribeTokenService _subject = new(new EphemeralDataProtectionProvider());

    [Fact]
    public void Generate_Then_Validate_Should_Roundtrip_Payload()
    {
        var userId = Guid.NewGuid();
        var token = _subject.Generate(userId, "type", "MonthlyStatement");

        var result = _subject.Validate(token);

        result.IsValid.Should().BeTrue();
        result.UserId.Should().Be(userId);
        result.Scope.Should().Be("type");
        result.Value.Should().Be("MonthlyStatement");
    }

    [Fact]
    public void Generate_Then_Validate_Should_Roundtrip_All_Scope_With_Null_Value()
    {
        var userId = Guid.NewGuid();
        var token = _subject.Generate(userId, "all", null);

        var result = _subject.Validate(token);

        result.IsValid.Should().BeTrue();
        result.Scope.Should().Be("all");
        result.Value.Should().BeNull();
    }

    [Fact]
    public void Generate_Should_Produce_Url_Safe_Token_Without_Cleartext_UserId()
    {
        var userId = Guid.NewGuid();
        var token = _subject.Generate(userId, "category", "marketing");

        token.Should().MatchRegex("^[A-Za-z0-9_-]+$");
        token.Should().NotContain(userId.ToString());
        token.Should().NotContain("marketing");
    }

    [Fact]
    public void Validate_Should_Reject_Tampered_Token()
    {
        var token = _subject.Generate(Guid.NewGuid(), "type", "Billing");
        var middle = token.Length / 2;
        var flipped = token[middle] == 'A' ? 'B' : 'A';
        var tampered = string.Concat(token.AsSpan(0, middle), flipped.ToString(), token.AsSpan(middle + 1));

        var result = _subject.Validate(tampered);

        result.IsValid.Should().BeFalse();
        result.UserId.Should().Be(Guid.Empty);
        result.Scope.Should().BeEmpty();
    }

    [Theory]
    [InlineData("garbage")]
    [InlineData("!!!not-base64url!!!")]
    [InlineData("aGVsbG8gd29ybGQ=")] // valid base64url-ish but not a protected payload
    [InlineData("")]
    public void Validate_Should_Reject_Malformed_Tokens(string token)
    {
        var result = _subject.Validate(token);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_Reject_Token_From_Different_Purpose()
    {
        var otherProtector = new EphemeralDataProtectionProvider().CreateProtector("some-other-purpose");
        var foreign = otherProtector.Protect("anything"u8.ToArray());
        var token = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(foreign);

        var result = _subject.Validate(token);

        result.IsValid.Should().BeFalse();
    }
}
