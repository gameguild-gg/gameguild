using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Query;
using GameGuild.CQRS;
using GameGuild.Identity.Tenants;
using Moq;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Commands;

public class PurgeUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPublisher> _publisherMock;
    private readonly PurgeUserCommandHandler _handler;

    public PurgeUserCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _publisherMock = new Mock<IPublisher>();
        _handler = new PurgeUserCommandHandler(_userRepositoryMock.Object, _publisherMock.Object);
    }

    [Fact]
    public async Task Handle_WithPurgeableUser_ShouldPurgeAndPublish()
    {
        var user = User.Create("purge@test.com", "Purge User");
        user.Version = 1;
        user.MarkDeleted();
        user.TenantMemberships.Add(new TenantMember { TenantId = Guid.NewGuid(), IsActive = false, Role = "Member" });
        var users = new[] { user };
        var command = new PurgeUserCommand(user.Id, PurgeStrategy.Immediate);

        _userRepositoryMock.Setup(x => x.GetQueryable()).Returns(new TestAsyncEnumerable<User>(users));
        _userRepositoryMock.Setup(x => x.PurgeAsync(user, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _publisherMock.Setup(x => x.Publish(It.IsAny<UserPurgedNotification>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().Be(Unit.Value);
        _userRepositoryMock.Verify(x => x.PurgeAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(
            x => x.Publish(
                It.Is<UserPurgedNotification>(n =>
                    n.UserId == user.Id &&
                    n.Email == user.Email &&
                    n.Name == user.Name &&
                    n.PurgeStrategy == PurgeStrategy.Immediate.ToString()),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentUser_ShouldThrowUserNotFoundException()
    {
        var command = new PurgeUserCommand(Guid.NewGuid());

        _userRepositoryMock.Setup(x => x.GetQueryable()).Returns(new TestAsyncEnumerable<User>(Array.Empty<User>()));

        var action = () => _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<UserNotFoundException>();
        _userRepositoryMock.Verify(x => x.PurgeAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithActiveMembership_ShouldThrowAndNotPurge()
    {
        var user = User.Create("active@test.com", "Active Member");
        user.Version = 1;
        user.MarkDeleted();
        user.TenantMemberships.Add(new TenantMember { TenantId = Guid.NewGuid(), IsActive = true, Role = "Admin" });
        var command = new PurgeUserCommand(user.Id);

        _userRepositoryMock.Setup(x => x.GetQueryable()).Returns(new TestAsyncEnumerable<User>(new[] { user }));

        var action = () => _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*active tenant memberships*");
        _userRepositoryMock.Verify(x => x.PurgeAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _publisherMock.Verify(x => x.Publish(It.IsAny<UserPurgedNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

internal sealed class TestAsyncQueryProvider<TEntity>(IQueryProvider inner) : IAsyncQueryProvider
{
    public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<TEntity>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestAsyncEnumerable<TElement>(expression);

    public object? Execute(Expression expression) => inner.Execute(expression);

    public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var expectedResultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = typeof(IQueryProvider)
            .GetMethod(nameof(IQueryProvider.Execute), 1, new[] { typeof(Expression) })!
            .MakeGenericMethod(expectedResultType)
            .Invoke(inner, new object[] { expression });

        return (TResult)typeof(Task)
            .GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(expectedResultType)
            .Invoke(null, new[] { executionResult })!;
    }
}

internal sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable)
        : base(enumerable)
    {
    }

    public TestAsyncEnumerable(Expression expression)
        : base(expression)
    {
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal sealed class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
{
    public T Current => inner.Current;

    public ValueTask DisposeAsync()
    {
        inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync() => new(inner.MoveNext());
}
