using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Social.Profiles.UnitTests.Services;

public sealed class SocialProfileServiceTests
{
    private readonly Mock<ISocialProfileRepository> _profiles = new(MockBehavior.Strict);
    private readonly Mock<IProfileSkillRepository> _skills = new(MockBehavior.Strict);
    private readonly Mock<IProfilePortfolioRepository> _portfolio = new(MockBehavior.Strict);

    [Fact]
    public async Task UpsertProfile_WhenMissingCreatesNormalizedProfile()
    {
        var command = CreateUpdateCommand(" @NewCreator ");
        _profiles.Setup(repository => repository.GetByUserAsync(command.UserId, default)).ReturnsAsync((SocialProfile?)null);
        _profiles.Setup(repository => repository.AddAsync(It.IsAny<SocialProfile>(), default))
            .ReturnsAsync((SocialProfile profile, CancellationToken _) => profile);

        var result = await CreateSubject().UpsertProfileAsync(command);

        result.Handle.Should().Be("newcreator");
        result.DisplayName.Should().Be(command.DisplayName);
        result.CompletenessScore.Should().Be(65);
        _profiles.Verify(repository => repository.UpdateAsync(It.IsAny<SocialProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpsertProfile_WhenPresentUpdatesSameAggregate()
    {
        var command = CreateUpdateCommand("updated");
        var profile = new SocialProfile { UserId = command.UserId, Handle = "old", DisplayName = "Old" };
        _profiles.Setup(repository => repository.GetByUserAsync(command.UserId, default)).ReturnsAsync(profile);
        _profiles.Setup(repository => repository.UpdateAsync(profile, default)).Returns(Task.CompletedTask);

        var result = await CreateSubject().UpsertProfileAsync(command);

        result.Id.Should().Be(profile.Id);
        result.Handle.Should().Be("updated");
        result.DisplayName.Should().Be(command.DisplayName);
        _profiles.Verify(repository => repository.AddAsync(It.IsAny<SocialProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAndSearch_MapRepositoryResultsWithoutChangingOrder()
    {
        var userId = Guid.NewGuid();
        var first = new SocialProfile { UserId = userId, Handle = "first" };
        var second = new SocialProfile { UserId = Guid.NewGuid(), Handle = "second" };
        _profiles.Setup(repository => repository.GetByUserAsync(userId, default)).ReturnsAsync(first);
        _profiles.Setup(repository => repository.GetByHandleAsync("@FIRST", default)).ReturnsAsync(first);
        _profiles.Setup(repository => repository.SearchAsync("engineer", 10, default)).ReturnsAsync([first, second]);

        var byUser = await CreateSubject().GetByUserAsync(userId);
        var byHandle = await CreateSubject().GetByHandleAsync("@FIRST");
        var search = await CreateSubject().SearchAsync("engineer", 10);

        byUser!.Id.Should().Be(first.Id);
        byHandle!.Id.Should().Be(first.Id);
        search.Select(profile => profile.Id).Should().Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task UpdatePrivacy_WhenProfileMissingThrowsKeyNotFound()
    {
        var command = new UpdateProfilePrivacyCommand(Guid.NewGuid(), ProfileVisibility.Private, false, false, false);
        _profiles.Setup(repository => repository.GetByUserAsync(command.UserId, default)).ReturnsAsync((SocialProfile?)null);

        var action = () => CreateSubject().UpdatePrivacyAsync(command);

        await action.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"*{command.UserId}*");
    }

    [Fact]
    public async Task UpdatePrivacy_ChangesAllVisibilityFlagsAndPersists()
    {
        var profile = new SocialProfile { UserId = Guid.NewGuid() };
        var command = new UpdateProfilePrivacyCommand(profile.UserId, ProfileVisibility.Private, false, true, false);
        _profiles.Setup(repository => repository.GetByUserAsync(profile.UserId, default)).ReturnsAsync(profile);
        _profiles.Setup(repository => repository.UpdateAsync(profile, default)).Returns(Task.CompletedTask);

        var result = await CreateSubject().UpdatePrivacyAsync(command);

        result.Visibility.Should().Be(ProfileVisibility.Private);
        result.ShowActivity.Should().BeFalse();
        result.ShowPortfolio.Should().BeTrue();
        result.ShowSkills.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateStats_ClampsNegativeInputsBeforePersistence()
    {
        var profile = new SocialProfile { UserId = Guid.NewGuid() };
        var command = new UpdateProfileStatsCommand(profile.UserId, -2, 8, -3, 5);
        _profiles.Setup(repository => repository.GetByUserAsync(profile.UserId, default)).ReturnsAsync(profile);
        _profiles.Setup(repository => repository.UpdateAsync(profile, default)).Returns(Task.CompletedTask);

        var result = await CreateSubject().UpdateStatsAsync(command);

        result.FollowerCount.Should().Be(0);
        result.FollowingCount.Should().Be(8);
        result.PostCount.Should().Be(0);
        result.ProjectCount.Should().Be(5);
    }

    [Fact]
    public async Task AddOrUpdateSkill_WhenNameExistsUpdatesInsteadOfAddingDuplicate()
    {
        var command = new AddProfileSkillCommand(Guid.NewGuid(), "  C#  ", ProfileSkillProficiency.Expert, 3);
        var profile = new SocialProfile { Id = command.ProfileId };
        var existing = new ProfileSkill { ProfileId = command.ProfileId, Name = "C#" };
        _profiles.Setup(repository => repository.GetByIdAsync(command.ProfileId, default)).ReturnsAsync(profile);
        _skills.Setup(repository => repository.GetByProfileAndNameAsync(command.ProfileId, "C#", default)).ReturnsAsync(existing);
        _skills.Setup(repository => repository.UpdateAsync(existing, default)).Returns(Task.CompletedTask);

        var result = await CreateSubject().AddOrUpdateSkillAsync(command);

        result.Id.Should().Be(existing.Id);
        result.Proficiency.Should().Be(ProfileSkillProficiency.Expert);
        result.DisplayOrder.Should().Be(3);
        _skills.Verify(repository => repository.AddAsync(It.IsAny<ProfileSkill>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveSkill_WhenMissingReturnsFalseWithoutDelete()
    {
        var skillId = Guid.NewGuid();
        _skills.Setup(repository => repository.GetByIdAsync(skillId, default)).ReturnsAsync((ProfileSkill?)null);

        var result = await CreateSubject().RemoveSkillAsync(skillId);

        result.Should().BeFalse();
        _skills.Verify(repository => repository.DeleteAsync(It.IsAny<ProfileSkill>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddPortfolioItem_MapsEveryCommandField()
    {
        var command = new AddProfilePortfolioItemCommand(
            Guid.NewGuid(), "Game", Guid.NewGuid(), "Description", "https://game.test", "https://image.test", true, 4);
        var profile = new SocialProfile { Id = command.ProfileId };
        _profiles.Setup(repository => repository.GetByIdAsync(command.ProfileId, default)).ReturnsAsync(profile);
        _portfolio.Setup(repository => repository.AddAsync(It.IsAny<ProfilePortfolioItem>(), default))
            .ReturnsAsync((ProfilePortfolioItem item, CancellationToken _) => item);

        var result = await CreateSubject().AddPortfolioItemAsync(command);

        result.Should().Match<ProfilePortfolioItemDto>(item =>
            item.ProfileId == command.ProfileId && item.ProjectId == command.ProjectId && item.Title == command.Title &&
            item.Description == command.Description && item.Url == command.Url && item.ImageUrl == command.ImageUrl &&
            item.IsPinned == command.IsPinned && item.DisplayOrder == command.DisplayOrder);
    }

    [Fact]
    public async Task UpdatePortfolioItem_WhenMissingThrowsKeyNotFound()
    {
        var command = new UpdateProfilePortfolioItemCommand(Guid.NewGuid(), "Missing");
        _portfolio.Setup(repository => repository.GetByIdAsync(command.ItemId, default)).ReturnsAsync((ProfilePortfolioItem?)null);

        var action = () => CreateSubject().UpdatePortfolioItemAsync(command);

        await action.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"*{command.ItemId}*");
    }

    [Fact]
    public async Task RemovePortfolioItem_WhenMissingReturnsFalseWithoutDelete()
    {
        var itemId = Guid.NewGuid();
        _portfolio.Setup(repository => repository.GetByIdAsync(itemId, default)).ReturnsAsync((ProfilePortfolioItem?)null);

        var result = await CreateSubject().RemovePortfolioItemAsync(itemId);

        result.Should().BeFalse();
        _portfolio.Verify(repository => repository.DeleteAsync(It.IsAny<ProfilePortfolioItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePortfolioItem_ReplacesMutableFieldsAndPersists()
    {
        var item = new ProfilePortfolioItem { Title = "Old" };
        var command = new UpdateProfilePortfolioItemCommand(item.Id, "New", "Description", "url", "image", true, 7);
        _portfolio.Setup(repository => repository.GetByIdAsync(item.Id, default)).ReturnsAsync(item);
        _portfolio.Setup(repository => repository.UpdateAsync(item, default)).Returns(Task.CompletedTask);

        var result = await CreateSubject().UpdatePortfolioItemAsync(command);

        result.Should().Match<ProfilePortfolioItemDto>(value =>
            value.Title == "New" && value.Description == "Description" && value.Url == "url" &&
            value.ImageUrl == "image" && value.IsPinned && value.DisplayOrder == 7);
    }

    [Fact]
    public async Task AddSkill_WhenProfileDoesNotExistRejectsOrphan()
    {
        var command = new AddProfileSkillCommand(Guid.NewGuid(), "C#");
        _skills.Setup(repository => repository.GetByProfileAndNameAsync(command.ProfileId, "C#", default)).ReturnsAsync((ProfileSkill?)null);
        _profiles.Setup(repository => repository.GetByIdAsync(command.ProfileId, default)).ReturnsAsync((SocialProfile?)null);

        var action = () => CreateSubject().AddOrUpdateSkillAsync(command);

        await action.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"*{command.ProfileId}*");
        _skills.Verify(repository => repository.AddAsync(It.IsAny<ProfileSkill>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddPortfolioItem_WhenProfileDoesNotExistRejectsOrphan()
    {
        var command = new AddProfilePortfolioItemCommand(Guid.NewGuid(), "Game");
        _profiles.Setup(repository => repository.GetByIdAsync(command.ProfileId, default)).ReturnsAsync((SocialProfile?)null);

        var action = () => CreateSubject().AddPortfolioItemAsync(command);

        await action.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"*{command.ProfileId}*");
        _portfolio.Verify(repository => repository.AddAsync(It.IsAny<ProfilePortfolioItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private SocialProfileService CreateSubject() => new(_profiles.Object, _skills.Object, _portfolio.Object);

    private static UpdateSocialProfileCommand CreateUpdateCommand(string handle) => new(
        Guid.NewGuid(),
        handle,
        "Creator",
        Bio: "Builds games");
}

public sealed class SocialProfileCompletenessLifecycleTests
{
    [Fact]
    public async Task SkillAndPortfolioMutations_KeepPersistedCompletenessInSync()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var context = SocialProfilesTestDbContext.Create(databaseName);
        var service = new SocialProfileService(
            new SocialProfileRepository(context),
            new ProfileSkillRepository(context),
            new ProfilePortfolioRepository(context));
        var userId = Guid.NewGuid();
        var profile = await service.UpsertProfileAsync(new UpdateSocialProfileCommand(userId, "creator", "Creator"));
        context.ResetSaveChangesCallCount();

        var skill = await service.AddOrUpdateSkillAsync(new AddProfileSkillCommand(profile.Id, "C#"));
        context.SaveChangesCallCount.Should().Be(1);
        var persisted = await ReadPersistedProfileAsync(databaseName, userId);
        persisted.CompletenessScore.Should().Be(55);
        persisted.Skills.Should().ContainSingle().Which.Id.Should().Be(skill.Id);

        context.ResetSaveChangesCallCount();
        var portfolio = await service.AddPortfolioItemAsync(new AddProfilePortfolioItemCommand(profile.Id, "Game"));
        context.SaveChangesCallCount.Should().Be(1);
        persisted = await ReadPersistedProfileAsync(databaseName, userId);
        persisted.CompletenessScore.Should().Be(60);
        persisted.PortfolioItems.Should().ContainSingle().Which.Id.Should().Be(portfolio.Id);

        context.ResetSaveChangesCallCount();
        (await service.RemoveSkillAsync(skill.Id)).Should().BeTrue();
        context.SaveChangesCallCount.Should().Be(1);
        persisted = await ReadPersistedProfileAsync(databaseName, userId);
        persisted.CompletenessScore.Should().Be(55);
        persisted.Skills.Should().BeEmpty();

        context.ResetSaveChangesCallCount();
        (await service.RemovePortfolioItemAsync(portfolio.Id)).Should().BeTrue();
        context.SaveChangesCallCount.Should().Be(1);
        persisted = await ReadPersistedProfileAsync(databaseName, userId);
        persisted.CompletenessScore.Should().Be(50);
        persisted.PortfolioItems.Should().BeEmpty();
    }

    private static async Task<SocialProfileDto> ReadPersistedProfileAsync(string databaseName, Guid userId)
    {
        await using var verificationContext = SocialProfilesTestDbContext.Create(databaseName);
        var profile = await new SocialProfileRepository(verificationContext).GetByUserAsync(userId);
        return profile!.ToDto();
    }
}
