namespace GameGuild.Assets.UnitTests;

public sealed class AssetLibraryDomainTests
{
    [Fact]
    public void FolderRestriction_StoresTypedTeamsAndAuthorities()
    {
        var tenantId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var folder = AssetFolder.Create(tenantId, "Project", projectId, null, "Deliverables");

        folder.SetRestriction(
            AssetFolderRestrictionMode.SelectedTeams,
            [teamId],
            ["Owner", "Manager"]);

        folder.RestrictionMode.Should().Be(AssetFolderRestrictionMode.SelectedTeams);
        folder.AllowedTeamIds.Should().Equal(teamId);
        folder.AllowedAuthorities.Should().Equal("Owner", "Manager");
        folder.BelongsTo("projects", projectId).Should().BeTrue();
    }

    [Fact]
    public void Reference_Copy_ReusesDeduplicatedContentButCreatesLogicalReference()
    {
        var contentId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var reference = new AssetReference(
            contentId,
            Guid.NewGuid(),
            "Build.zip",
            AssetAccessPolicy.Inherited,
            "Project",
            parentId);

        var copy = reference.CopyTo(Guid.NewGuid(), "Build copy.zip", Guid.NewGuid());

        copy.Id.Should().NotBe(reference.Id);
        copy.AssetContentId.Should().Be(contentId);
        copy.ParentResourceType.Should().Be("Project");
        copy.ParentResourceId.Should().Be(parentId);
        copy.DisplayName.Should().Be("Build copy.zip");
    }

    [Fact]
    public void Reference_ReplaceAndRestoreContent_AppendsImmutableRevisions()
    {
        var initialContentId = Guid.NewGuid();
        var replacementContentId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var reference = new AssetReference(
            initialContentId,
            actorId,
            "Build.zip",
            AssetAccessPolicy.Inherited,
            "Project",
            Guid.NewGuid());

        var first = reference.CreateInitialRevision(actorId);
        var second = reference.ReplaceContent(replacementContentId, actorId, "Uploaded replacement");
        var restored = reference.RestoreRevision(first, actorId);

        first.RevisionNumber.Should().Be(1);
        first.AssetContentId.Should().Be(initialContentId);
        second.RevisionNumber.Should().Be(2);
        second.AssetContentId.Should().Be(replacementContentId);
        restored.RevisionNumber.Should().Be(3);
        restored.AssetContentId.Should().Be(initialContentId);
        reference.AssetContentId.Should().Be(initialContentId);
        reference.CurrentRevisionNumber.Should().Be(3);
    }
}
