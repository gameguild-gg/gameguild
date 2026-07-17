using FluentAssertions;
using Xunit;

namespace GameGuild.Social.Profiles.UnitTests.Repositories;

public sealed class SocialProfileRepositoryTests
{
    [Fact]
    public async Task AddAndLookups_ReturnActiveProfileWithChildrenAndNormalizeHandle()
    {
        var databaseName = Guid.NewGuid().ToString();
        var profile = new SocialProfile
        {
            UserId = Guid.NewGuid(),
            Handle = "creator",
            DisplayName = "Creator",
            Skills = [new ProfileSkill { Name = "C#" }],
            PortfolioItems = [new ProfilePortfolioItem { Title = "Game" }]
        };

        await using (var writeContext = SocialProfilesTestDbContext.Create(databaseName))
        {
            var writeRepository = new SocialProfileRepository(writeContext);
            (await writeRepository.AddAsync(profile)).Should().BeSameAs(profile);
        }

        await using var readContext = SocialProfilesTestDbContext.Create(databaseName);
        var repository = new SocialProfileRepository(readContext);

        (await repository.GetByIdAsync(profile.Id))!.Skills.Should().ContainSingle();
        (await repository.GetByUserAsync(profile.UserId))!.PortfolioItems.Should().ContainSingle();
        (await repository.GetByHandleAsync("  @CREATOR "))!.Id.Should().Be(profile.Id);
    }

    [Fact]
    public async Task Lookups_ExcludeSoftDeletedProfiles()
    {
        await using var context = SocialProfilesTestDbContext.Create();
        var profile = new SocialProfile
        {
            UserId = Guid.NewGuid(), Handle = "deleted", DisplayName = "Deleted", DeletedAt = DateTime.UtcNow
        };
        context.Add(profile);
        await context.SaveChangesAsync();
        var repository = new SocialProfileRepository(context);

        (await repository.GetByIdAsync(profile.Id)).Should().BeNull();
        (await repository.GetByUserAsync(profile.UserId)).Should().BeNull();
        (await repository.GetByHandleAsync(profile.Handle)).Should().BeNull();
    }

    [Fact]
    public async Task Search_FiltersVisibilityAndDeletionThenMatchesAndOrdersResults()
    {
        await using var context = SocialProfilesTestDbContext.Create();
        var top = CreateProfile("zeta", "Zeta Engineer", ProfileVisibility.Public, 90);
        var second = CreateProfile("alpha", "Alpha", ProfileVisibility.Public, 70, "Engine programmer");
        var noMatch = CreateProfile("artist", "Artist", ProfileVisibility.Public, 100);
        var privateMatch = CreateProfile("private-engineer", "Private", ProfileVisibility.Private, 100);
        var deletedMatch = CreateProfile("deleted-engineer", "Deleted", ProfileVisibility.Public, 100);
        deletedMatch.DeletedAt = DateTime.UtcNow;
        context.AddRange(top, second, noMatch, privateMatch, deletedMatch);
        await context.SaveChangesAsync();
        var repository = new SocialProfileRepository(context);

        var result = await repository.SearchAsync(" ENGINE ", 20);

        result.Select(profile => profile.Id).Should().Equal(top.Id, second.Id);
    }

    [Fact]
    public async Task Search_ClampsTakeBetweenOneAndOneHundred()
    {
        await using var context = SocialProfilesTestDbContext.Create();
        context.AddRange(Enumerable.Range(0, 105).Select(index =>
            CreateProfile($"user-{index:D3}", $"User {index}", ProfileVisibility.Public, index)));
        await context.SaveChangesAsync();
        var repository = new SocialProfileRepository(context);

        (await repository.SearchAsync(null, 0)).Should().ContainSingle();
        (await repository.SearchAsync(null, 500)).Should().HaveCount(100);
    }

    [Fact]
    public async Task Update_PersistsChangesAndTouchesProfile()
    {
        await using var context = SocialProfilesTestDbContext.Create();
        var repository = new SocialProfileRepository(context);
        var profile = await repository.AddAsync(CreateProfile("before", "Before", ProfileVisibility.Public, 50));
        var previousUpdatedAt = profile.UpdatedAt;
        profile.DisplayName = "After";

        await repository.UpdateAsync(profile);
        context.ChangeTracker.Clear();

        var persisted = await repository.GetByIdAsync(profile.Id);
        persisted!.DisplayName.Should().Be("After");
        persisted.UpdatedAt.Should().BeOnOrAfter(previousUpdatedAt);
    }

    private static SocialProfile CreateProfile(
        string handle,
        string displayName,
        ProfileVisibility visibility,
        int completeness,
        string? headline = null) => new()
        {
            UserId = Guid.NewGuid(),
            Handle = handle,
            DisplayName = displayName,
            Visibility = visibility,
            CompletenessScore = completeness,
            Headline = headline
        };
}

public sealed class ProfileSkillRepositoryTests
{
    [Fact]
    public async Task SkillCrud_UsesProfileAndExactNameAndPhysicallyDeletes()
    {
        await using var context = SocialProfilesTestDbContext.Create();
        var profile = new SocialProfile { UserId = Guid.NewGuid(), Handle = "skills", DisplayName = "Skills" };
        context.Add(profile);
        await context.SaveChangesAsync();
        var repository = new ProfileSkillRepository(context);
        var skill = new ProfileSkill { ProfileId = profile.Id, Name = "C#" };

        (await repository.AddAsync(skill)).Should().BeSameAs(skill);
        (await repository.GetByIdAsync(skill.Id)).Should().BeSameAs(skill);
        (await repository.GetByProfileAndNameAsync(profile.Id, "C#")).Should().BeSameAs(skill);
        (await repository.GetByProfileAndNameAsync(profile.Id, "c#")).Should().BeNull();

        skill.Update(ProfileSkillProficiency.Expert, 2);
        await repository.UpdateAsync(skill);
        context.ChangeTracker.Clear();
        (await repository.GetByIdAsync(skill.Id))!.Proficiency.Should().Be(ProfileSkillProficiency.Expert);

        var persisted = (await repository.GetByIdAsync(skill.Id))!;
        await repository.DeleteAsync(persisted);
        (await repository.GetByIdAsync(skill.Id)).Should().BeNull();
    }
}

public sealed class ProfilePortfolioRepositoryTests
{
    [Fact]
    public async Task PortfolioCrud_OrdersPinnedItemsAndPersistsUpdatesAndDeletes()
    {
        await using var context = SocialProfilesTestDbContext.Create();
        var profile = new SocialProfile { UserId = Guid.NewGuid(), Handle = "portfolio", DisplayName = "Portfolio" };
        context.Add(profile);
        await context.SaveChangesAsync();
        var repository = new ProfilePortfolioRepository(context);
        var unpinned = new ProfilePortfolioItem { ProfileId = profile.Id, Title = "Unpinned", DisplayOrder = 0 };
        var pinnedLater = new ProfilePortfolioItem { ProfileId = profile.Id, Title = "Pinned later", IsPinned = true, DisplayOrder = 10 };
        var pinnedFirst = new ProfilePortfolioItem { ProfileId = profile.Id, Title = "Pinned first", IsPinned = true, DisplayOrder = 1 };

        (await repository.AddAsync(unpinned)).Should().BeSameAs(unpinned);
        context.AddRange(pinnedLater, pinnedFirst);
        await context.SaveChangesAsync();

        (await repository.GetByProfileAsync(profile.Id)).Select(item => item.Id)
            .Should().Equal(pinnedFirst.Id, pinnedLater.Id, unpinned.Id);

        unpinned.Update("Updated", null, null, null, false, 20);
        await repository.UpdateAsync(unpinned);
        context.ChangeTracker.Clear();
        (await repository.GetByIdAsync(unpinned.Id))!.Title.Should().Be("Updated");

        var persisted = (await repository.GetByIdAsync(unpinned.Id))!;
        await repository.DeleteAsync(persisted);
        (await repository.GetByIdAsync(unpinned.Id)).Should().BeNull();
    }
}
