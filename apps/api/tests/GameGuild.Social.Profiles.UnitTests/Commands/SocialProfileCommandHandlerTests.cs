using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Social.Profiles.UnitTests.Commands;

public sealed class SocialProfileCommandHandlerTests
{
    [Fact]
    public async Task UpdateProfileHandler_ForwardsFullCommandAndCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var command = new UpdateSocialProfileCommand(Guid.NewGuid(), "creator", "Creator", Bio: "Bio");
        var expected = CreateProfileDto(command.UserId, command.Handle);
        var service = new Mock<ISocialProfileService>(MockBehavior.Strict);
        service.Setup(subject => subject.UpsertProfileAsync(command, cancellation.Token)).ReturnsAsync(expected);

        var result = await new UpdateSocialProfileCommandHandler(service.Object).Handle(command, cancellation.Token);

        result.Should().BeSameAs(expected);
        service.VerifyAll();
    }

    [Fact]
    public async Task PrivacyAndStatsHandlers_ForwardUserScopedCommands()
    {
        var userId = Guid.NewGuid();
        var privacy = new UpdateProfilePrivacyCommand(userId, ProfileVisibility.Private, false, false, false);
        var stats = new UpdateProfileStatsCommand(userId, 1, 2, 3, 4);
        var expected = CreateProfileDto(userId, "creator");
        var service = new Mock<ISocialProfileService>(MockBehavior.Strict);
        service.Setup(subject => subject.UpdatePrivacyAsync(privacy, default)).ReturnsAsync(expected);
        service.Setup(subject => subject.UpdateStatsAsync(stats, default)).ReturnsAsync(expected);

        (await new UpdateProfilePrivacyCommandHandler(service.Object).Handle(privacy, default)).Should().BeSameAs(expected);
        (await new UpdateProfileStatsCommandHandler(service.Object).Handle(stats, default)).Should().BeSameAs(expected);
    }

    [Fact]
    public async Task SkillHandlers_ForwardMutationIdentifiers()
    {
        var add = new AddProfileSkillCommand(Guid.NewGuid(), "C#", ProfileSkillProficiency.Expert, 2);
        var skill = new ProfileSkillDto(Guid.NewGuid(), add.ProfileId, add.Name, add.Proficiency, add.DisplayOrder);
        var remove = new RemoveProfileSkillCommand(skill.Id);
        var service = new Mock<ISocialProfileService>(MockBehavior.Strict);
        service.Setup(subject => subject.AddOrUpdateSkillAsync(add, default)).ReturnsAsync(skill);
        service.Setup(subject => subject.RemoveSkillAsync(skill.Id, default)).ReturnsAsync(true);

        (await new AddProfileSkillCommandHandler(service.Object).Handle(add, default)).Should().BeSameAs(skill);
        (await new RemoveProfileSkillCommandHandler(service.Object).Handle(remove, default)).Should().BeTrue();
    }

    [Fact]
    public async Task PortfolioHandlers_ForwardAddUpdateAndRemoveCommands()
    {
        var add = new AddProfilePortfolioItemCommand(Guid.NewGuid(), "Game");
        var item = new ProfilePortfolioItemDto(Guid.NewGuid(), add.ProfileId, null, add.Title, null, null, null, false, 0);
        var update = new UpdateProfilePortfolioItemCommand(item.Id, "Updated");
        var remove = new RemoveProfilePortfolioItemCommand(item.Id);
        var service = new Mock<ISocialProfileService>(MockBehavior.Strict);
        service.Setup(subject => subject.AddPortfolioItemAsync(add, default)).ReturnsAsync(item);
        service.Setup(subject => subject.UpdatePortfolioItemAsync(update, default)).ReturnsAsync(item);
        service.Setup(subject => subject.RemovePortfolioItemAsync(item.Id, default)).ReturnsAsync(true);

        (await new AddProfilePortfolioItemCommandHandler(service.Object).Handle(add, default)).Should().BeSameAs(item);
        (await new UpdateProfilePortfolioItemCommandHandler(service.Object).Handle(update, default)).Should().BeSameAs(item);
        (await new RemoveProfilePortfolioItemCommandHandler(service.Object).Handle(remove, default)).Should().BeTrue();
    }

    [Fact]
    public async Task SearchHandler_ForwardsQueryAndTakeLimit()
    {
        var query = new SearchSocialProfilesQuery("engineer", 25);
        var expected = new List<SocialProfileDto> { CreateProfileDto(Guid.NewGuid(), "creator") };
        var service = new Mock<ISocialProfileService>(MockBehavior.Strict);
        service.Setup(subject => subject.SearchAsync(query.Query, query.Take, default)).ReturnsAsync(expected);

        var result = await new SearchSocialProfilesQueryHandler(service.Object).Handle(query, default);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task LookupHandlers_ForwardUserIdAndUnnormalizedHandle()
    {
        using var cancellation = new CancellationTokenSource();
        var userQuery = new GetSocialProfileByUserQuery(Guid.NewGuid());
        var handleQuery = new GetSocialProfileByHandleQuery("  @CREATOR ");
        var expected = CreateProfileDto(userQuery.UserId, "creator");
        var service = new Mock<ISocialProfileService>(MockBehavior.Strict);
        service.Setup(subject => subject.GetByUserAsync(userQuery.UserId, cancellation.Token)).ReturnsAsync(expected);
        service.Setup(subject => subject.GetByHandleAsync(handleQuery.Handle, cancellation.Token)).ReturnsAsync(expected);

        var byUser = await new GetSocialProfileByUserQueryHandler(service.Object).Handle(userQuery, cancellation.Token);
        var byHandle = await new GetSocialProfileByHandleQueryHandler(service.Object).Handle(handleQuery, cancellation.Token);

        byUser.Should().BeSameAs(expected);
        byHandle.Should().BeSameAs(expected);
        service.VerifyAll();
    }

    private static SocialProfileDto CreateProfileDto(Guid userId, string handle) => new(
        Guid.NewGuid(), userId, handle, "Creator", null, null, null, null, null, null, null, "{}",
        ProfileVisibility.Public, ProfileAvailabilityStatus.NotSet, true, true, true, null, 50, 0, 0, 0, 0, [], []);
}
