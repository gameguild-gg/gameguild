using FluentAssertions;
using GameGuild.CQRS;
using Xunit;

namespace GameGuild.Tests.CQRS.Unit;

/// <summary>
/// Unit tests for CQRS abstractions and interfaces
/// </summary>
public class CQRSAbstractionsTests
{
    [Fact]
    public void ICommand_Should_Inherit_From_IRequest()
    {
        // Assert
        typeof(ICommand).Should().BeAssignableTo<IRequest>();
    }

    [Fact]
    public void ICommand_Generic_Should_Inherit_From_IRequest_Generic()
    {
        // Assert
        typeof(ICommand<string>).Should().BeAssignableTo<IRequest<string>>();
    }

    [Fact]
    public void IResultCommand_Should_Inherit_From_ICommand_Result()
    {
        // Assert
        typeof(IResultCommand).Should().BeAssignableTo<ICommand<Result>>();
    }

    [Fact]
    public void IQuery_Should_Inherit_From_IRequest()
    {
        // Assert
        typeof(IQuery<string>).Should().BeAssignableTo<IRequest<string>>();
    }

    [Fact]
    public void ICommandHandler_Should_Inherit_From_IRequestHandler()
    {
        // Assert
        typeof(ICommandHandler<TestCommand, string>).Should().BeAssignableTo<IRequestHandler<TestCommand, string>>();
        typeof(ICommandHandler<TestCommand>).Should().BeAssignableTo<IRequestHandler<TestCommand>>();
    }

    [Fact]
    public void IQueryHandler_Should_Inherit_From_IRequestHandler()
    {
        // Assert
        typeof(IQueryHandler<TestQuery, string>).Should().BeAssignableTo<IRequestHandler<TestQuery, string>>();
    }

    [Fact]
    public void IResultCommandHandler_Should_Inherit_From_IRequestHandler()
    {
        // Assert
        typeof(IResultCommandHandler<TestResultCommand>).Should().BeAssignableTo<IRequestHandler<TestResultCommand, Result>>();
    }

    [Fact]
    public void IResultQueryHandler_Should_Inherit_From_IRequestHandler()
    {
        // Assert
        typeof(IResultQueryHandler<TestResultQuery>).Should().BeAssignableTo<IRequestHandler<TestResultQuery, Result>>();
    }

    [Fact]
    public void Unit_Should_Have_Default_Value()
    {
        // Act
        var unit1 = Unit.Value;
        var unit2 = new Unit();
        var unit3 = default(Unit);

        // Assert
        unit1.Should().Be(unit2);
        unit1.Should().Be(unit3);
        unit2.Should().Be(unit3);
    }

    [Fact]
    public void Unit_Should_Be_Equal()
    {
        // Arrange
        var unit1 = Unit.Value;
        var unit2 = Unit.Value;

        // Act & Assert
        unit1.Equals(unit2).Should().BeTrue();
        (unit1 == unit2).Should().BeTrue();
        (unit1 != unit2).Should().BeFalse();
        unit1.GetHashCode().Should().Be(unit2.GetHashCode());
    }

    [Fact]
    public void Unit_ToString_Should_Return_Unit()
    {
        // Act
        var result = Unit.Value.ToString();

        // Assert
        result.Should().Be("()");
    }

    [Fact]
    public void SortDirection_Should_Have_Expected_Values()
    {
        // Assert
        Enum.GetValues<SortDirection>().Should().Contain(new[] { SortDirection.Ascending, SortDirection.Descending });
    }

    [Fact]
    public void PaginatedQuery_Should_Have_Default_Values()
    {
        // Act
        var query = new TestPaginatedQuery();

        // Assert
        query.Page.Should().Be(1);
        query.PageSize.Should().Be(10);
        query.SortBy.Should().BeNull();
        query.SortDirection.Should().Be(SortDirection.Ascending);
    }

    [Fact]
    public void PaginatedQuery_Should_Accept_Custom_Values()
    {
        // Act
        var query = new TestPaginatedQuery
        {
            Page = 5,
            PageSize = 20,
            SortBy = "Name",
            SortDirection = SortDirection.Descending
        };

        // Assert
        query.Page.Should().Be(5);
        query.PageSize.Should().Be(20);
        query.SortBy.Should().Be("Name");
        query.SortDirection.Should().Be(SortDirection.Descending);
    }

    [Fact]
    public void IHasDomainEvents_Should_Provide_Domain_Events_Access()
    {
        // Arrange
        var entity = new TestEntityWithDomainEvents();
        var domainEvent = new TestDomainEvent();

        // Act
        entity.AddDomainEvent(domainEvent);

        // Assert
        entity.DomainEvents.Should().Contain(domainEvent);
        entity.DomainEvents.Should().HaveCount(1);
    }

    // Test classes
    public class TestCommand : ICommand<string> { }
    public class TestCommandWithoutResponse : ICommand { }
    public class TestQuery : IQuery<string> { }
    public class TestResultCommand : IResultCommand { }
    public class TestResultQuery : IResultQuery { }
    public class TestPaginatedQuery : PaginatedQuery<string> { }

    public class TestDomainEvent : IDomainEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    }

    public class TestEntityWithDomainEvents : IHasDomainEvents
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        public void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}