using FluentAssertions;
using GameGuild.CQRS;

namespace GameGuild.SharedKernel.UnitTests.CQRS;

public class UnitTests
{
    [Fact]
    public void Value_ShouldReturnDefault()
    {
        var unit = Unit.Value;
        unit.Should().Be(default(Unit));
    }

    [Fact]
    public void Task_ShouldReturnCompletedTask()
    {
        Unit.Task.Should().NotBeNull();
        Unit.Task.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void Equals_Unit_ShouldBeTrue()
    {
        var a = Unit.Value;
        var b = Unit.Value;
        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Equals_Object_SameType_ShouldBeTrue()
    {
        var a = Unit.Value;
        object b = Unit.Value;
        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Equals_Object_DifferentType_ShouldBeFalse()
    {
        var a = Unit.Value;
        a.Equals("string").Should().BeFalse();
    }

    [Fact]
    public void Equals_Object_Null_ShouldBeFalse()
    {
        var a = Unit.Value;
        a.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ShouldBeZero()
    {
        Unit.Value.GetHashCode().Should().Be(0);
    }

    [Fact]
    public void CompareTo_Unit_ShouldBeZero()
    {
        var a = Unit.Value;
        var b = Unit.Value;
        a.CompareTo(b).Should().Be(0);
    }

    [Fact]
    public void CompareTo_Object_Unit_ShouldBeZero()
    {
        var a = Unit.Value;
        object b = Unit.Value;
        a.CompareTo(b).Should().Be(0);
    }

    [Fact]
    public void CompareTo_Object_Null_ShouldBeZero()
    {
        var a = Unit.Value;
        a.CompareTo(null).Should().Be(0);
    }

    [Fact]
    public void ToString_ShouldReturnParentheses()
    {
        Unit.Value.ToString().Should().Be("()");
    }

    [Fact]
    public void EqualityOperator_ShouldBeTrue()
    {
        var a = Unit.Value;
        var b = Unit.Value;
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void InequalityOperator_ShouldBeFalse()
    {
        var a = Unit.Value;
        var b = Unit.Value;
        (a != b).Should().BeFalse();
    }
}

public class PaginatedQueryTests
{
    [Fact]
    public void Defaults_ShouldBeReasonable()
    {
        var query = new TestPaginatedQuery();
        query.Skip.Should().Be(0);
        query.Take.Should().Be(50);
        query.SearchTerm.Should().BeNull();
        query.IncludeDeleted.Should().BeFalse();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var query = new TestPaginatedQuery
        {
            Skip = 10,
            Take = 25,
            SearchTerm = "hello",
            IncludeDeleted = true
        };
        query.Skip.Should().Be(10);
        query.Take.Should().Be(25);
        query.SearchTerm.Should().Be("hello");
        query.IncludeDeleted.Should().BeTrue();
    }

    private class TestPaginatedQuery : PaginatedQuery<string>;
}

public class RequestExceptionHandlerStateTests
{
    [Fact]
    public void Wrapper_DefaultState_ShouldBeUnhandled()
    {
        var wrapper = new RequestExceptionHandlerStateWrapper();
        wrapper.State.Should().Be(RequestExceptionHandlerState.Continue);
    }

    [Fact]
    public void Wrapper_SetToHandled()
    {
        var wrapper = new RequestExceptionHandlerStateWrapper();
        wrapper.State = RequestExceptionHandlerState.Handled;
        wrapper.State.Should().Be(RequestExceptionHandlerState.Handled);
    }
}
