using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Controllers;

public class UserMembershipsControllerTests
{
    [Fact]
    public async Task GetUserMemberships_Should_Return_Ok()
    {
        var sender = new StubSender();
        sender.Setup<GetUserMembershipsQuery, GetUserMembershipsResponse>(_ => new GetUserMembershipsResponse { TotalCount = 1, Memberships = new List<UserMembershipDto>() });

        var controller = new UserMembershipsController(sender);
        var result = await controller.GetUserMemberships(Guid.NewGuid(), false, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CheckUserHasMemberships_Should_Return_NotFound_When_Empty()
    {
        var sender = new StubSender();
        sender.Setup<GetUserMembershipsQuery, GetUserMembershipsResponse>(_ => new GetUserMembershipsResponse { TotalCount = 0, Memberships = new List<UserMembershipDto>() });

        var controller = new UserMembershipsController(sender);
        var result = await controller.CheckUserHasMemberships(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetMembershipCount_Should_Return_Count()
    {
        var sender = new StubSender();
        sender.Setup<GetUserMembershipsQuery, GetUserMembershipsResponse>(_ => new GetUserMembershipsResponse { TotalCount = 3, Memberships = new List<UserMembershipDto>() });

        var controller = new UserMembershipsController(sender);
        var result = await controller.GetMembershipCount(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AddUserMembership_Should_Return_Created_When_Command_Succeeds()
    {
        var sender = new StubSender();
        sender.Setup<AddTenantMemberCommand, AddTenantMemberResponse>(_ => new AddTenantMemberResponse
        {
            Success = true,
            MemberId = Guid.NewGuid(),
            Message = "Member added successfully"
        });

        var controller = new UserMembershipsController(sender);
        var result = await controller.AddUserMembership(
            Guid.NewGuid(),
            new AddUserMembershipRequest
            {
                TenantId = Guid.NewGuid(),
                Role = "Renter",
                InvitedByEmail = "admin@game-guild.com"
            },
            CancellationToken.None);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    private sealed class StubSender : ISender
    {
        private readonly Dictionary<Type, Func<object, object?>> _handlers = new();

        public void Setup<TRequest, TResponse>(Func<TRequest, TResponse> handler)
        {
            _handlers[typeof(TRequest)] = request => handler((TRequest)request);
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (_handlers.TryGetValue(request.GetType(), out var handler))
            {
                return Task.FromResult((TResponse)handler(request)!);
            }

            return Task.FromResult(default(TResponse)!);
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            if (_handlers.TryGetValue(request.GetType(), out var handler))
            {
                return Task.FromResult(handler(request));
            }

            return Task.FromResult<object?>(null);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        {
            return Task.CompletedTask;
        }

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
