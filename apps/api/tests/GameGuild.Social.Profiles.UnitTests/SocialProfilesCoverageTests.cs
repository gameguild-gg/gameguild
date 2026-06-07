using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GameGuild.Social.Profiles.UnitTests;

public sealed class SocialProfilesCoverageTests
{
    [Fact]
    public async Task SocialProfileService_ShouldCover_ProfilePrivacyStatsSkillsAndPortfolioFlows()
    {
        var profiles = new MemoryProfileRepository();
        var skills = new MemorySkillRepository();
        var portfolio = new MemoryPortfolioRepository();
        var service = new SocialProfileService(profiles, skills, portfolio);
        var userId = Guid.NewGuid();

        var created = await service.UpsertProfileAsync(new UpdateSocialProfileCommand(
            userId,
            " @CreatorOne ",
            "Creator One",
            "Builds learning games",
            "https://cdn/avatar.png",
            "https://cdn/banner.png",
            "Gameplay programmer",
            "Remote",
            "UTC",
            "https://creator.test",
            "{\"github\":\"creator\"}",
            ProfileAvailabilityStatus.OpenToCollaborate));

        created.Handle.Should().Be("creatorone");
        created.CompletenessScore.Should().BeGreaterThan(80);
        (await service.GetByUserAsync(userId))!.DisplayName.Should().Be("Creator One");
        (await service.GetByHandleAsync("@CreatorOne"))!.UserId.Should().Be(userId);
        (await service.SearchAsync("creator", 20)).Should().ContainSingle();
        (await service.SearchAsync(null, 20)).Should().ContainSingle();

        var updated = await service.UpsertProfileAsync(new UpdateSocialProfileCommand(
            userId,
            "creator-two",
            "Creator Two",
            Headline: "Technical artist",
            AvailabilityStatus: ProfileAvailabilityStatus.Busy));
        updated.Handle.Should().Be("creator-two");

        var privacy = await service.UpdatePrivacyAsync(new UpdateProfilePrivacyCommand(
            userId,
            ProfileVisibility.Connections,
            false,
            true,
            false));
        privacy.Visibility.Should().Be(ProfileVisibility.Connections);
        privacy.ShowActivity.Should().BeFalse();

        var stats = await service.UpdateStatsAsync(new UpdateProfileStatsCommand(userId, -1, 5, 10, 2));
        stats.FollowerCount.Should().Be(0);
        stats.FollowingCount.Should().Be(5);

        await service.Invoking(current => current.UpdatePrivacyAsync(new UpdateProfilePrivacyCommand(Guid.NewGuid(), ProfileVisibility.Private, false, false, false)))
            .Should().ThrowAsync<KeyNotFoundException>();
        await service.Invoking(current => current.UpdateStatsAsync(new UpdateProfileStatsCommand(Guid.NewGuid(), 1, 1, 1, 1)))
            .Should().ThrowAsync<KeyNotFoundException>();

        var skill = await service.AddOrUpdateSkillAsync(new AddProfileSkillCommand(created.Id, " C# ", ProfileSkillProficiency.Advanced, 2));
        skill.Name.Should().Be("C#");
        var skillUpdate = await service.AddOrUpdateSkillAsync(new AddProfileSkillCommand(created.Id, "C#", ProfileSkillProficiency.Expert, 1));
        skillUpdate.Proficiency.Should().Be(ProfileSkillProficiency.Expert);
        (await service.RemoveSkillAsync(Guid.NewGuid())).Should().BeFalse();
        (await service.RemoveSkillAsync(skill.Id)).Should().BeTrue();

        var item = await service.AddPortfolioItemAsync(new AddProfilePortfolioItemCommand(
            created.Id,
            "Quest Builder",
            Guid.NewGuid(),
            "A tool",
            "https://example.test",
            "https://cdn/image.png",
            true,
            1));
        item.IsPinned.Should().BeTrue();
        var itemUpdate = await service.UpdatePortfolioItemAsync(new UpdateProfilePortfolioItemCommand(
            item.Id,
            "Quest Builder 2",
            "Updated",
            null,
            null,
            false,
            2));
        itemUpdate.Title.Should().Be("Quest Builder 2");
        await service.Invoking(current => current.UpdatePortfolioItemAsync(new UpdateProfilePortfolioItemCommand(Guid.NewGuid(), "Missing")))
            .Should().ThrowAsync<KeyNotFoundException>();
        (await service.RemovePortfolioItemAsync(Guid.NewGuid())).Should().BeFalse();
        (await service.RemovePortfolioItemAsync(item.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task SocialProfileRepositories_ShouldCover_EfQueriesAndMutations()
    {
        await using var db = CreateDbContext();
        var profileRepository = new SocialProfileRepository(db);
        var skillRepository = new ProfileSkillRepository(db);
        var portfolioRepository = new ProfilePortfolioRepository(db);
        var userId = Guid.NewGuid();
        var profile = new SocialProfile
        {
            UserId = userId,
            Handle = "creator",
            DisplayName = "Creator",
            Headline = "Gameplay programmer",
            Visibility = ProfileVisibility.Public,
            CompletenessScore = 90
        };

        profile = await profileRepository.AddAsync(profile);
        await profileRepository.AddAsync(new SocialProfile
        {
            UserId = Guid.NewGuid(),
            Handle = "private",
            DisplayName = "Private",
            Visibility = ProfileVisibility.Private
        });

        (await profileRepository.GetByIdAsync(profile.Id))!.Handle.Should().Be("creator");
        (await profileRepository.GetByIdAsync(Guid.NewGuid())).Should().BeNull();
        (await profileRepository.GetByUserAsync(userId))!.Id.Should().Be(profile.Id);
        (await profileRepository.GetByHandleAsync("@CREATOR"))!.UserId.Should().Be(userId);
        (await profileRepository.SearchAsync("gameplay", 50)).Should().ContainSingle();
        (await profileRepository.SearchAsync("private", 50)).Should().BeEmpty();
        profile.DisplayName = "Creator Updated";
        await profileRepository.UpdateAsync(profile);

        var skill = await skillRepository.AddAsync(new ProfileSkill
        {
            ProfileId = profile.Id,
            Name = "C#",
            Proficiency = ProfileSkillProficiency.Advanced
        });
        (await skillRepository.GetByIdAsync(skill.Id))!.Name.Should().Be("C#");
        (await skillRepository.GetByIdAsync(Guid.NewGuid())).Should().BeNull();
        (await skillRepository.GetByProfileAndNameAsync(profile.Id, "C#"))!.Id.Should().Be(skill.Id);
        skill.Update(ProfileSkillProficiency.Expert, 3);
        await skillRepository.UpdateAsync(skill);

        var item = await portfolioRepository.AddAsync(new ProfilePortfolioItem
        {
            ProfileId = profile.Id,
            Title = "Tool",
            IsPinned = true,
            DisplayOrder = 2
        });
        await portfolioRepository.AddAsync(new ProfilePortfolioItem
        {
            ProfileId = profile.Id,
            Title = "Other",
            DisplayOrder = 1
        });
        (await portfolioRepository.GetByIdAsync(item.Id))!.Title.Should().Be("Tool");
        (await portfolioRepository.GetByIdAsync(Guid.NewGuid())).Should().BeNull();
        (await portfolioRepository.GetByProfileAsync(profile.Id)).First().Title.Should().Be("Tool");
        item.Update("Tool Updated", "desc", "https://tool.test", "https://image.test", false, 3);
        await portfolioRepository.UpdateAsync(item);
        await portfolioRepository.DeleteAsync(item);
        await skillRepository.DeleteAsync(skill);
    }

    [Fact]
    public async Task CqrsHandlersAndController_ShouldDelegateThroughSender()
    {
        var service = new Mock<ISocialProfileService>();
        var sender = new Mock<ISender>();
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var profile = new SocialProfileDto(
            profileId,
            userId,
            "creator",
            "Creator",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "{}",
            ProfileVisibility.Public,
            ProfileAvailabilityStatus.NotSet,
            true,
            true,
            true,
            null,
            50,
            1,
            2,
            3,
            4,
            [],
            []);
        var skill = new ProfileSkillDto(skillId, profileId, "C#", ProfileSkillProficiency.Expert, 1);
        var item = new ProfilePortfolioItemDto(itemId, profileId, null, "Tool", null, null, null, false, 0);

        service.Setup(current => current.UpsertProfileAsync(It.IsAny<UpdateSocialProfileCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        service.Setup(current => current.UpdatePrivacyAsync(It.IsAny<UpdateProfilePrivacyCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        service.Setup(current => current.UpdateStatsAsync(It.IsAny<UpdateProfileStatsCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        service.Setup(current => current.AddOrUpdateSkillAsync(It.IsAny<AddProfileSkillCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(skill);
        service.Setup(current => current.RemoveSkillAsync(skillId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        service.Setup(current => current.AddPortfolioItemAsync(It.IsAny<AddProfilePortfolioItemCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(item);
        service.Setup(current => current.UpdatePortfolioItemAsync(It.IsAny<UpdateProfilePortfolioItemCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(item);
        service.Setup(current => current.RemovePortfolioItemAsync(itemId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        service.Setup(current => current.GetByUserAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        service.Setup(current => current.GetByHandleAsync("creator", It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        service.Setup(current => current.SearchAsync("creator", 20, It.IsAny<CancellationToken>())).ReturnsAsync([profile]);

        (await new UpdateSocialProfileCommandHandler(service.Object).Handle(new UpdateSocialProfileCommand(userId, "creator", "Creator"), CancellationToken.None)).Should().Be(profile);
        (await new UpdateProfilePrivacyCommandHandler(service.Object).Handle(new UpdateProfilePrivacyCommand(userId, ProfileVisibility.Public, true, true, true), CancellationToken.None)).Should().Be(profile);
        (await new UpdateProfileStatsCommandHandler(service.Object).Handle(new UpdateProfileStatsCommand(userId, 1, 2, 3, 4), CancellationToken.None)).Should().Be(profile);
        (await new AddProfileSkillCommandHandler(service.Object).Handle(new AddProfileSkillCommand(profileId, "C#"), CancellationToken.None)).Should().Be(skill);
        (await new RemoveProfileSkillCommandHandler(service.Object).Handle(new RemoveProfileSkillCommand(skillId), CancellationToken.None)).Should().BeTrue();
        (await new AddProfilePortfolioItemCommandHandler(service.Object).Handle(new AddProfilePortfolioItemCommand(profileId, "Tool"), CancellationToken.None)).Should().Be(item);
        (await new UpdateProfilePortfolioItemCommandHandler(service.Object).Handle(new UpdateProfilePortfolioItemCommand(itemId, "Tool"), CancellationToken.None)).Should().Be(item);
        (await new RemoveProfilePortfolioItemCommandHandler(service.Object).Handle(new RemoveProfilePortfolioItemCommand(itemId), CancellationToken.None)).Should().BeTrue();
        (await new GetSocialProfileByUserQueryHandler(service.Object).Handle(new GetSocialProfileByUserQuery(userId), CancellationToken.None)).Should().Be(profile);
        (await new GetSocialProfileByHandleQueryHandler(service.Object).Handle(new GetSocialProfileByHandleQuery("creator"), CancellationToken.None)).Should().Be(profile);
        (await new SearchSocialProfilesQueryHandler(service.Object).Handle(new SearchSocialProfilesQuery("creator"), CancellationToken.None)).Should().ContainSingle();

        sender.Setup(current => current.Send(It.IsAny<GetSocialProfileByUserQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        sender.Setup(current => current.Send(It.IsAny<GetSocialProfileByHandleQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        sender.Setup(current => current.Send(It.IsAny<SearchSocialProfilesQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([profile]);
        sender.Setup(current => current.Send(It.IsAny<UpdateSocialProfileCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        sender.Setup(current => current.Send(It.IsAny<UpdateProfilePrivacyCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        sender.Setup(current => current.Send(It.IsAny<UpdateProfileStatsCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        sender.Setup(current => current.Send(It.IsAny<AddProfileSkillCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(skill);
        sender.Setup(current => current.Send(new RemoveProfileSkillCommand(skillId), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        sender.Setup(current => current.Send(new RemoveProfileSkillCommand(Guid.Empty), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        sender.Setup(current => current.Send(It.IsAny<AddProfilePortfolioItemCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(item);
        sender.Setup(current => current.Send(It.IsAny<UpdateProfilePortfolioItemCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(item);
        sender.Setup(current => current.Send(new RemoveProfilePortfolioItemCommand(itemId), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        sender.Setup(current => current.Send(new RemoveProfilePortfolioItemCommand(Guid.Empty), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var controller = new SocialProfilesController(sender.Object);

        (await controller.GetByUser(userId, CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.GetByHandle("creator", CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.Search("creator", 20, CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.Upsert(userId, new UpdateSocialProfileBody("creator", "Creator"), CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.UpdatePrivacy(userId, new UpdateProfilePrivacyBody(ProfileVisibility.Public, true, true, true), CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.UpdateStats(userId, new UpdateProfileStatsBody(1, 2, 3, 4), CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.AddSkill(profileId, new AddProfileSkillBody("C#"), CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.RemoveSkill(skillId, CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.RemoveSkill(Guid.Empty, CancellationToken.None)).Should().BeOfType<NotFoundResult>();
        (await controller.AddPortfolioItem(profileId, new AddProfilePortfolioItemBody("Tool"), CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.UpdatePortfolioItem(itemId, new UpdateProfilePortfolioItemBody("Tool"), CancellationToken.None)).Result.Should().BeOfType<OkObjectResult>();
        (await controller.RemovePortfolioItem(itemId, CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.RemovePortfolioItem(Guid.Empty, CancellationToken.None)).Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public void EntitiesConfigurationAndDependencyInjection_ShouldCoverRemainingMembers()
    {
        SocialProfile.NormalizeHandle(" @PlayerOne ").Should().Be("playerone");

        var profile = new SocialProfile
        {
            UserId = Guid.NewGuid(),
            Handle = "creator",
            DisplayName = "Creator"
        };
        profile.CalculateCompleteness().Should().Be(50);
        profile.UpdatePrivacy(ProfileVisibility.Private, false, false, false);
        profile.Visibility.Should().Be(ProfileVisibility.Private);
        profile.UpdateStats(-5, -1, 2, 3);
        profile.FollowerCount.Should().Be(0);
        profile.FollowingCount.Should().Be(0);
        var skill = new ProfileSkill { ProfileId = profile.Id, Name = "Design" };
        skill.Update(ProfileSkillProficiency.Advanced, 2);
        skill.ToDto().DisplayOrder.Should().Be(2);
        var item = new ProfilePortfolioItem { ProfileId = profile.Id, Title = "Old" };
        item.Update("New", "desc", "url", "image", true, 1);
        item.ToDto().IsPinned.Should().BeTrue();
        profile.Skills.Add(skill);
        profile.PortfolioItems.Add(item);
        profile.CalculateCompleteness().Should().Be(60);
        profile.ToDto().Skills.Should().ContainSingle();

        var modelBuilder = new ModelBuilder();
        new SocialProfilesModelConfiguration().Configure(modelBuilder);
        modelBuilder.Model.FindEntityType(typeof(SocialProfile))!.FindProperty(nameof(SocialProfile.Handle))!.GetMaxLength().Should().Be(80);
        modelBuilder.Model.FindEntityType(typeof(ProfileSkill))!.FindProperty(nameof(ProfileSkill.Proficiency))!.GetMaxLength().Should().Be(40);
        modelBuilder.Model.FindEntityType(typeof(ProfilePortfolioItem))!.FindProperty(nameof(ProfilePortfolioItem.Title))!.GetMaxLength().Should().Be(200);

        var services = new ServiceCollection();
        services.AddSocialProfilesModule().Should().BeSameAs(services);
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(ISocialProfileService) && descriptor.ImplementationType == typeof(SocialProfileService));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(ISocialProfileRepository) && descriptor.ImplementationType == typeof(SocialProfileRepository));

        var module = new SocialProfilesModule();
        module.Name.Should().Be("Social.Profiles");
        module.Order.Should().Be(160);
        module.ConfigureServices(new ServiceCollection(), new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build()).Should().NotBeNull();
        var endpoints = new Mock<Microsoft.AspNetCore.Routing.IEndpointRouteBuilder>().Object;
        module.MapEndpoints(endpoints).Should().BeSameAs(endpoints);
    }

    private static ProfilesTestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ProfilesTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ProfilesTestDbContext(options);
    }

    private sealed class ProfilesTestDbContext(DbContextOptions<ProfilesTestDbContext> options) : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new SocialProfileConfiguration().Configure(modelBuilder.Entity<SocialProfile>());
            new ProfileSkillConfiguration().Configure(modelBuilder.Entity<ProfileSkill>());
            new ProfilePortfolioItemConfiguration().Configure(modelBuilder.Entity<ProfilePortfolioItem>());
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class MemoryProfileRepository : ISocialProfileRepository
    {
        public List<SocialProfile> Items { get; } = [];

        public Task<SocialProfile> AddAsync(SocialProfile profile, CancellationToken ct = default)
        {
            Items.Add(profile);
            return Task.FromResult(profile);
        }

        public Task<SocialProfile?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(Items.FirstOrDefault(item => item.Id == id && !item.IsDeleted));

        public Task<SocialProfile?> GetByUserAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult(Items.FirstOrDefault(item => item.UserId == userId && !item.IsDeleted));

        public Task<SocialProfile?> GetByHandleAsync(string handle, CancellationToken ct = default)
        {
            var normalized = SocialProfile.NormalizeHandle(handle);
            return Task.FromResult(Items.FirstOrDefault(item => item.Handle == normalized && !item.IsDeleted));
        }

        public Task<List<SocialProfile>> SearchAsync(string? query, int take, CancellationToken ct = default)
        {
            var results = Items.Where(item => item.Visibility == ProfileVisibility.Public && !item.IsDeleted);
            if (!string.IsNullOrWhiteSpace(query))
            {
                var normalized = query.Trim().ToLowerInvariant();
                results = results.Where(item => item.Handle.Contains(normalized) || item.DisplayName.ToLowerInvariant().Contains(normalized));
            }

            return Task.FromResult(results.Take(take).ToList());
        }

        public Task UpdateAsync(SocialProfile profile, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class MemorySkillRepository : IProfileSkillRepository
    {
        public List<ProfileSkill> Items { get; } = [];
        public Task<ProfileSkill> AddAsync(ProfileSkill skill, CancellationToken ct = default)
        {
            Items.Add(skill);
            return Task.FromResult(skill);
        }

        public Task<ProfileSkill?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(Items.FirstOrDefault(item => item.Id == id && !item.IsDeleted));

        public Task<ProfileSkill?> GetByProfileAndNameAsync(Guid profileId, string name, CancellationToken ct = default)
            => Task.FromResult(Items.FirstOrDefault(item => item.ProfileId == profileId && item.Name == name && !item.IsDeleted));

        public Task UpdateAsync(ProfileSkill skill, CancellationToken ct = default) => Task.CompletedTask;

        public Task DeleteAsync(ProfileSkill skill, CancellationToken ct = default)
        {
            Items.Remove(skill);
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryPortfolioRepository : IProfilePortfolioRepository
    {
        public List<ProfilePortfolioItem> Items { get; } = [];
        public Task<ProfilePortfolioItem> AddAsync(ProfilePortfolioItem item, CancellationToken ct = default)
        {
            Items.Add(item);
            return Task.FromResult(item);
        }

        public Task<ProfilePortfolioItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(Items.FirstOrDefault(item => item.Id == id && !item.IsDeleted));

        public Task<List<ProfilePortfolioItem>> GetByProfileAsync(Guid profileId, CancellationToken ct = default)
            => Task.FromResult(Items.Where(item => item.ProfileId == profileId && !item.IsDeleted).ToList());

        public Task UpdateAsync(ProfilePortfolioItem item, CancellationToken ct = default) => Task.CompletedTask;

        public Task DeleteAsync(ProfilePortfolioItem item, CancellationToken ct = default)
        {
            Items.Remove(item);
            return Task.CompletedTask;
        }
    }
}
