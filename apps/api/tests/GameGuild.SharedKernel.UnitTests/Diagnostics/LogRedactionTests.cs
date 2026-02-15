using FluentAssertions;
using Xunit;

namespace GameGuild.SharedKernel.UnitTests;

public class LogRedactionTests
{
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
}
