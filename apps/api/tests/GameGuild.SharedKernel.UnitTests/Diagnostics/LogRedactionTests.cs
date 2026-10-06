using FluentAssertions;
using Xunit;

namespace GameGuild.SharedKernel.UnitTests;

public class LogRedactionTests
{
    [Theory]
    [InlineData("alice@private-organization.example")]
    [InlineData("alice@example.com\r\nFORGED EVENT")]
    [InlineData("alice@example.com\u0085FORGED EVENT")]
    [InlineData("alice@example.com\u2028FORGED EVENT")]
    public void MaskEmail_ShouldNotExposeAnyPartOfTheAddress(string email)
    {
        LogRedaction.MaskEmail(email).Should().Be("email:redacted");
    }

    [Fact]
    public void RedactSecret_ShouldNotExposeADictionaryCheckableFingerprint()
    {
        LogRedaction.RedactSecret("password").Should().Be("secret:redacted");
        LogRedaction.RedactSecret("different-password").Should().Be("secret:redacted");
    }

    [Fact]
    public void Sanitize_ShouldReplaceUnicodeLineSeparatorsAndC1Controls()
    {
        LogRedaction.Sanitize("a\u007Fb\u0085c\u009Fd\u2028e\u2029f").Should().Be("a␀b␀c␀d␀e␀f");
    }

    [Fact]
    public void RedactId_Guid_ShouldReturnPrefixedHash()
    {
        var id = Guid.NewGuid();
        var result = LogRedaction.RedactId(id);

        result.Should().StartWith("tid:");
        result.Should().HaveLength(12); // "tid:" + 8 hex chars
    }

    [Fact]
    public void RedactId_Guid_ShouldBeDeterministic()
    {
        var id = Guid.NewGuid();
        var result1 = LogRedaction.RedactId(id);
        var result2 = LogRedaction.RedactId(id);

        result1.Should().Be(result2);
    }

    [Fact]
    public void RedactId_Guid_Null_ShouldReturnNone()
    {
        LogRedaction.RedactId((Guid?)null).Should().Be("none");
    }

    [Fact]
    public void RedactId_Guid_Empty_ShouldReturnNone()
    {
        LogRedaction.RedactId(Guid.Empty).Should().Be("none");
    }

    [Fact]
    public void RedactId_Guid_CustomPrefix_ShouldUseIt()
    {
        var id = Guid.NewGuid();
        var result = LogRedaction.RedactId(id, "usr");

        result.Should().StartWith("usr:");
    }

    [Fact]
    public void RedactId_String_ShouldReturnPrefixedHash()
    {
        var result = LogRedaction.RedactId("user-123");

        result.Should().StartWith("uid:");
        result.Should().HaveLength(12); // "uid:" + 8 hex chars
    }

    [Fact]
    public void RedactId_String_Null_ShouldReturnNone()
    {
        LogRedaction.RedactId((string?)null).Should().Be("none");
    }

    [Fact]
    public void RedactId_String_Empty_ShouldReturnNone()
    {
        LogRedaction.RedactId("").Should().Be("none");
    }

    [Fact]
    public void RedactId_String_CustomPrefix_ShouldUseIt()
    {
        var result = LogRedaction.RedactId("test", "sub");

        result.Should().StartWith("sub:");
    }

    [Fact]
    public void RedactId_DifferentGuids_ShouldProduceDifferentHashes()
    {
        var hash1 = LogRedaction.RedactId(Guid.NewGuid());
        var hash2 = LogRedaction.RedactId(Guid.NewGuid());

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void MaskEmail_TypicalAddress_ShouldRedactLocalPartAndDomain()
    {
        LogRedaction.MaskEmail("alice@example.com").Should().Be("email:redacted");
    }

    [Fact]
    public void MaskEmail_SingleCharLocalPart_ShouldNotLeakAnyCharacter()
    {
        LogRedaction.MaskEmail("a@example.com").Should().Be("email:redacted");
    }

    [Fact]
    public void MaskEmail_NullOrEmpty_ShouldReturnNone()
    {
        LogRedaction.MaskEmail(null).Should().Be("none");
        LogRedaction.MaskEmail("").Should().Be("none");
    }

    [Fact]
    public void MaskEmail_NoAtSign_ShouldReturnInvalid()
    {
        LogRedaction.MaskEmail("not-an-email").Should().Be("invalid");
    }

    [Fact]
    public void RedactSecret_ShouldReturnConstantRedactionMarker()
    {
        var result = LogRedaction.RedactSecret("super-secret-token");

        result.Should().Be("secret:redacted");
    }

    [Fact]
    public void RedactSecret_ShouldBeDeterministic()
    {
        LogRedaction.RedactSecret("same-token").Should().Be(LogRedaction.RedactSecret("same-token"));
    }

    [Fact]
    public void RedactSecret_NullOrEmpty_ShouldReturnNone()
    {
        LogRedaction.RedactSecret(null).Should().Be("none");
        LogRedaction.RedactSecret("").Should().Be("none");
    }

    [Fact]
    public void RedactSecret_ShouldNotContainOriginalValue()
    {
        var result = LogRedaction.RedactSecret("hunter2-password");

        result.Should().NotContain("hunter2");
    }

    [Fact]
    public void Sanitize_ShouldReplaceNewlinesWithVisibleMarker()
    {
        LogRedaction.Sanitize("first line\r\nsecond line\nthird").Should().Be("first line␀␀second line␀third");
    }

    [Fact]
    public void Sanitize_ShouldReplaceAllControlCharacters()
    {
        LogRedaction.Sanitize("a\u0000b\u001Fc").Should().Be("a␀b␀c");
    }

    [Fact]
    public void Sanitize_NullOrEmpty_ShouldReturnEmpty()
    {
        LogRedaction.Sanitize(null).Should().BeEmpty();
        LogRedaction.Sanitize("").Should().BeEmpty();
    }

    [Fact]
    public void Sanitize_CleanText_ShouldRemainUnchanged()
    {
        LogRedaction.Sanitize("normal log text").Should().Be("normal log text");
    }
}
