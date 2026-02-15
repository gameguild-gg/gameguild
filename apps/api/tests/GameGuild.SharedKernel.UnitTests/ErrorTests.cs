using FluentAssertions;
using Xunit;

namespace GameGuild.SharedKernel.UnitTests;

public class ErrorTests
{
    [Fact]
    public void None_ShouldHaveEmptyCodeAndDescription()
    {
        Error.None.Code.Should().BeEmpty();
        Error.None.Description.Should().BeEmpty();
        Error.None.Type.Should().Be(ErrorType.None);
    }

    [Fact]
    public void NullValue_ShouldHaveCorrectCodeAndType()
    {
        Error.NullValue.Code.Should().Be("General.Null");
        Error.NullValue.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public void Failure_ShouldCreateWithCorrectType()
    {
        var error = Error.Failure("code", "desc");
        error.Code.Should().Be("code");
        error.Description.Should().Be("desc");
        error.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public void NotFound_ShouldCreateWithCorrectType()
    {
        var error = Error.NotFound("code", "desc");
        error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void Problem_ShouldCreateWithCorrectType()
    {
        var error = Error.Problem("code", "desc");
        error.Type.Should().Be(ErrorType.Problem);
    }

    [Fact]
    public void Conflict_ShouldCreateWithCorrectType()
    {
        var error = Error.Conflict("code", "desc");
        error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public void Validation_ShouldCreateWithCorrectType()
    {
        var error = Error.Validation("code", "desc");
        error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Unauthorized_ShouldCreateWithCorrectType()
    {
        var error = Error.Unauthorized("code", "desc");
        error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public void Forbidden_ShouldCreateWithCorrectType()
    {
        var error = Error.Forbidden("code", "desc");
        error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public void Equality_ShouldCompareByValue()
    {
        var a = Error.Failure("code", "desc");
        var b = Error.Failure("code", "desc");
        a.Should().Be(b);
    }

    [Fact]
    public void Inequality_ShouldDifferByCode()
    {
        var a = Error.Failure("code1", "desc");
        var b = Error.Failure("code2", "desc");
        a.Should().NotBe(b);
    }
}
