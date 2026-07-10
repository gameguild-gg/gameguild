using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Queries;

public class GetUserMembershipsQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_Map_Memberships_To_Dto()
    {
        var userId = Guid.NewGuid();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Tenant", Slug = "tenant" };
        var members = new List<TenantMember>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Tenant = tenant,
                UserId = userId,
                Role = "Admin",
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            }
        };

        var repo = new Mock<ITenantMemberRepository>();
        repo.Setup(r => r.GetByUserIdAsync(userId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(members);

        var handler = new GetUserMembershipsQueryHandler(repo.Object);
        var result = await handler.Handle(new GetUserMembershipsQuery(userId, IncludeInactive: false), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Memberships[0].TenantName.Should().Be("Tenant");
        result.Memberships[0].Role.Should().Be("Admin");
    }

    [Fact]
    public async Task Handle_Should_Map_Invite_Metadata_To_Dto()
    {
        var userId = Guid.NewGuid();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Tenant", Slug = "tenant" };
        var invitedAt = DateTime.Parse("2026-07-01T12:00:00Z").ToUniversalTime();
        var lastSentAt = DateTime.Parse("2026-07-02T12:00:00Z").ToUniversalTime();
        var members = new List<TenantMember>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Tenant = tenant,
                UserId = userId,
                Role = "Moderator",
                IsActive = false,
                JoinedAt = invitedAt,
                Metadata = $$"""
                {"inviteStatus":"Pending","invitedByEmail":"admin@game-guild.com","inviteeEmail":"learner@example.com","inviteeName":"Learner One","invitedAt":"{{invitedAt:O}}","lastSentAt":"{{lastSentAt:O}}","resendCount":2}
                """
            }
        };

        var repo = new Mock<ITenantMemberRepository>();
        repo.Setup(r => r.GetByUserIdAsync(userId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(members);

        var handler = new GetUserMembershipsQueryHandler(repo.Object);
        var result = await handler.Handle(new GetUserMembershipsQuery(userId, IncludeInactive: true), CancellationToken.None);

        result.Memberships[0].InviteStatus.Should().Be("Pending");
        result.Memberships[0].InvitedByEmail.Should().Be("admin@game-guild.com");
        result.Memberships[0].InviteeEmail.Should().Be("learner@example.com");
        result.Memberships[0].InviteeName.Should().Be("Learner One");
        result.Memberships[0].InvitedAt.Should().Be(invitedAt);
        result.Memberships[0].LastInviteSentAt.Should().Be(lastSentAt);
        result.Memberships[0].InviteResendCount.Should().Be(2);
    }
}
