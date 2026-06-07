using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using GameGuild.Social.Ratings;
using GameGuild.Social.Ratings.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace GameGuild.Social.Ratings.Tests;

public class ConfigurationModuleAndServiceTests
{
    // --- EF Core Configurations ---
    [Fact]
    public void RatingEntityConfiguration_Configures()
    {
        var mb = new ModelBuilder(new ConventionSet());
        new RatingEntityConfiguration().Configure(mb.Entity<Rating>());
    }

    [Fact]
    public void RatingSummaryEntityConfiguration_Configures()
    {
        var mb = new ModelBuilder(new ConventionSet());
        new RatingSummaryEntityConfiguration().Configure(mb.Entity<RatingSummary>());
    }

    [Fact]
    public void RatingHelpfulVoteEntityConfiguration_Configures()
    {
        var mb = new ModelBuilder(new ConventionSet());
        new RatingHelpfulVoteEntityConfiguration().Configure(mb.Entity<RatingHelpfulVote>());
    }

    // --- RatingsModule ---
    [Fact]
    public void AddRatingsModule_RegistersServices()
    {
        var sc = new ServiceCollection();
        sc.AddLogging();
        sc.AddScoped<IApplicationDbContext>(_ => Mock.Of<IApplicationDbContext>());
        sc.AddScoped<IActorContextAccessor>(_ => Mock.Of<IActorContextAccessor>());
        sc.AddRatingsModule();
        var sp = sc.BuildServiceProvider();
        sp.GetService<IRatingService>().Should().NotBeNull();
    }

    // --- RatingsController ---
    [Fact]
    public void RatingsController_Ctor()
    {
        var svc = Mock.Of<IRatingService>();
        new RatingsController(svc).Should().NotBeNull();
    }

    // --- RatingCrudService ---
    [Fact]
    public void RatingCrudService_Ctor_AndGetCurrentUserId()
    {
        var db = new Mock<IApplicationDbContext>();
        var actor = new Mock<IActorContextAccessor>();
        var uid = Guid.NewGuid();
        actor.Setup(a => a.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User, SubjectId = uid.ToString(), TenantId = Guid.NewGuid(),
            IsAuthenticated = true, Roles = new HashSet<string>(), Permissions = new HashSet<string>()
        });
        var qs = Mock.Of<IRatingQueryService>();
        var log = Mock.Of<ILogger<RatingCrudService>>();
        var svc = new RatingCrudService(db.Object, actor.Object, qs, log);
        svc.Should().NotBeNull();
    }

    // --- RatingModerationService ---
    [Fact]
    public void RatingModerationService_Ctor()
    {
        var db = new Mock<IApplicationDbContext>();
        var actor = new Mock<IActorContextAccessor>();
        actor.Setup(a => a.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User, SubjectId = Guid.NewGuid().ToString(), TenantId = Guid.NewGuid(),
            IsAuthenticated = true, Roles = new HashSet<string>(), Permissions = new HashSet<string>()
        });
        var qs = Mock.Of<IRatingQueryService>();
        var log = Mock.Of<ILogger<RatingModerationService>>();
        var svc = new RatingModerationService(db.Object, actor.Object, qs, log);
        svc.Should().NotBeNull();
    }

    // --- RatingQueryService ---
    [Fact]
    public void RatingQueryService_Ctor()
    {
        var db = new Mock<IApplicationDbContext>();
        var actor = new Mock<IActorContextAccessor>();
        actor.Setup(a => a.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User, SubjectId = Guid.NewGuid().ToString(), TenantId = Guid.NewGuid(),
            IsAuthenticated = true, Roles = new HashSet<string>(), Permissions = new HashSet<string>()
        });
        var log = Mock.Of<ILogger<RatingQueryService>>();
        var svc = new RatingQueryService(db.Object, actor.Object, log);
        svc.Should().NotBeNull();
    }

    [Fact]
    public async Task RatingServices_WithAnonymousActor_ShouldThrowUnauthorized()
    {
        var db = new Mock<IApplicationDbContext>();
        var actor = new Mock<IActorContextAccessor>();
        actor.Setup(a => a.ActorContext).Returns(ActorContext.Anonymous);
        var query = new RatingQueryService(db.Object, actor.Object, Mock.Of<ILogger<RatingQueryService>>());
        var crud = new RatingCrudService(db.Object, actor.Object, query, Mock.Of<ILogger<RatingCrudService>>());
        var moderation = new RatingModerationService(db.Object, actor.Object, query, Mock.Of<ILogger<RatingModerationService>>());

        await crud.Invoking(x => x.RateAsync(Guid.NewGuid(), "Course", 4))
            .Should().ThrowAsync<UnauthorizedAccessException>();
        await query.Invoking(x => x.HasUserRatedAsync(Guid.NewGuid(), "Course"))
            .Should().ThrowAsync<UnauthorizedAccessException>();
        await moderation.Invoking(x => x.VoteHelpfulAsync(Guid.NewGuid(), true))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task RatingServices_WithAuthenticatedActor_ShouldUseUserIdBranch()
    {
        var db = new Mock<IApplicationDbContext>();
        db.Setup(x => x.Set<Rating>())
            .Returns(new List<Rating>().AsQueryable().BuildMockDbSet().Object);
        db.Setup(x => x.Set<RatingHelpfulVote>())
            .Returns(new List<RatingHelpfulVote>().AsQueryable().BuildMockDbSet().Object);

        var actor = new Mock<IActorContextAccessor>();
        actor.Setup(a => a.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = Guid.NewGuid().ToString(),
            TenantId = Guid.NewGuid(),
            IsAuthenticated = true,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>()
        });

        var query = new RatingQueryService(db.Object, actor.Object, Mock.Of<ILogger<RatingQueryService>>());
        var crud = new RatingCrudService(db.Object, actor.Object, query, Mock.Of<ILogger<RatingCrudService>>());
        var moderation = new RatingModerationService(db.Object, actor.Object, query, Mock.Of<ILogger<RatingModerationService>>());

        (await crud.GetUserRatingAsync(Guid.NewGuid(), "Course")).IsFailure.Should().BeTrue();
        (await query.HasUserRatedAsync(Guid.NewGuid(), "Course")).Value.Should().BeFalse();
        (await moderation.VoteHelpfulAsync(Guid.NewGuid(), true)).IsFailure.Should().BeTrue();
    }
}
