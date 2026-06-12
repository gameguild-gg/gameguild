using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace GameGuild.Tags.UnitTests;

public sealed class TagsCoverageCompletionTests
{
    [Fact]
    public async Task Repository_Should_Query_All_Tag_Relationship_And_Proficiency_Branches()
    {
        await using var context = CreateContext();
        var repository = new TagsRepository(context);
        var tenantId = Guid.NewGuid();
        var csharp = new Tag { Id = Guid.NewGuid(), Name = "CSharp", Description = "Language", Type = TagType.Technology, TenantId = tenantId, IsActive = true };
        var beginner = new Tag { Id = Guid.NewGuid(), Name = "Beginner", Description = null, Type = TagType.Difficulty, TenantId = tenantId, IsActive = true };
        var inactive = new Tag { Id = Guid.NewGuid(), Name = "Inactive", Description = "Hidden", Type = TagType.Topic, IsActive = false };

        await repository.AddTagAsync(csharp);
        await repository.AddTagAsync(beginner);
        await repository.AddTagAsync(inactive);
        csharp.Color = "#fff";
        await repository.UpdateTagAsync(csharp);

        (await repository.GetTagAsync(csharp.Id)).Should().BeEquivalentTo(csharp);
        (await repository.SearchTagsAsync(null, null, null, false, -10, 0)).Should().ContainSingle().Which.Name.Should().Be("Beginner");
        (await repository.SearchTagsAsync("Lang", TagType.Technology, tenantId, false, 0, 100)).Should().ContainSingle().Which.Id.Should().Be(csharp.Id);
        (await repository.SearchTagsAsync("Inactive", null, null, true, 0, 150)).Should().ContainSingle().Which.Id.Should().Be(inactive.Id);
        (await repository.SearchTagsAsync("missing", null, null, true, 0, 10)).Should().BeEmpty();

        var relationship = new TagRelationship { Id = Guid.NewGuid(), SourceId = csharp.Id, TargetId = beginner.Id, Type = TagRelationshipType.Requires, Weight = .8m, Metadata = "meta" };
        await repository.AddRelationshipAsync(relationship);
        (await repository.GetRelationshipsAsync(csharp.Id)).Should().ContainSingle().Which.Metadata.Should().Be("meta");
        (await repository.GetRelationshipsAsync(beginner.Id)).Should().ContainSingle().Which.SourceId.Should().Be(csharp.Id);

        var activeProficiency = new TagProficiency { Id = Guid.NewGuid(), Name = "Advanced CSharp", Type = TagType.Skill, ProficiencyLevel = SkillProficiencyLevel.Advanced, IsActive = true };
        var inactiveProficiency = new TagProficiency { Id = Guid.NewGuid(), Name = "Hidden", Type = TagType.Skill, ProficiencyLevel = SkillProficiencyLevel.Beginner, IsActive = false };
        await repository.AddProficiencyAsync(activeProficiency);
        await repository.AddProficiencyAsync(inactiveProficiency);
        (await repository.SearchProficienciesAsync(null, null, false)).Should().ContainSingle().Which.Id.Should().Be(activeProficiency.Id);
        (await repository.SearchProficienciesAsync(TagType.Skill, SkillProficiencyLevel.Beginner, true)).Should().ContainSingle().Which.Id.Should().Be(inactiveProficiency.Id);
    }

    [Fact]
    public async Task Service_Should_Cover_Update_Create_Search_And_Error_Branches()
    {
        await using var context = CreateContext();
        var service = new TagsService(new TagsRepository(context));
        var tenantId = Guid.NewGuid();
        var created = await service.CreateTagAsync(new CreateTagRequest(" C# ", TagType.Technology, "Language", "#123456", "code", tenantId));

        created.Name.Should().Be("C#");
        created.IsActive.Should().BeTrue();
        (await service.GetTagAsync(created.Id)).Should().NotBeNull();
        (await service.GetTagAsync(Guid.NewGuid())).Should().BeNull();

        (await service.UpdateTagAsync(Guid.NewGuid(), new UpdateTagRequest(Name: "Missing"))).Should().BeNull();
        var unchanged = await service.UpdateTagAsync(created.Id, new UpdateTagRequest());
        unchanged!.Name.Should().Be("C#");
        var updated = await service.UpdateTagAsync(created.Id, new UpdateTagRequest(" CSharp ", "Updated", "#654321", "hash", false));
        updated!.Name.Should().Be("CSharp");
        updated.IsActive.Should().BeFalse();

        (await service.SearchTagsAsync(new SearchTagsQuery(IncludeInactive: true, Take: 20))).Should().ContainSingle();
        (await service.SearchTagsAsync(new SearchTagsQuery(Search: "Updated", Type: TagType.Technology, TenantId: tenantId, IncludeInactive: true))).Should().ContainSingle();

        await service.Invoking(s => s.CreateRelationshipAsync(new CreateTagRelationshipRequest(created.Id, created.Id, TagRelationshipType.Related)))
            .Should().ThrowAsync<InvalidOperationException>().WithMessage("A tag cannot relate to itself.");

        var second = await service.CreateTagAsync(new CreateTagRequest("Basics", TagType.Topic));
        var relationship = await service.CreateRelationshipAsync(new CreateTagRelationshipRequest(created.Id, second.Id, TagRelationshipType.Parent, .5m, "metadata"));
        relationship.Weight.Should().Be(.5m);
        (await service.GetRelationshipsAsync(created.Id)).Should().ContainSingle();

        var proficiency = await service.CreateProficiencyAsync(new CreateTagProficiencyRequest("Expert C#", TagType.Skill, SkillProficiencyLevel.Expert, "Expert", "#000000", "star"));
        proficiency.ProficiencyLevel.Should().Be(SkillProficiencyLevel.Expert);
        (await service.SearchProficienciesAsync(new SearchTagProficienciesQuery(TagType.Skill, SkillProficiencyLevel.Expert, IncludeInactive: false))).Should().ContainSingle();
    }

    [Fact]
    public async Task Handlers_Controllers_Di_And_ModelConfiguration_Should_Cover_Public_Module_Surface()
    {
        var tag = new TagDto(Guid.NewGuid(), "Tag", "Desc", TagType.Skill, "#fff", "icon", true, Guid.NewGuid());
        var relationship = new TagRelationshipDto(Guid.NewGuid(), tag.Id, Guid.NewGuid(), TagRelationshipType.Related, null, null);
        var proficiency = new TagProficiencyDto(Guid.NewGuid(), "Expert", null, TagType.Skill, SkillProficiencyLevel.Expert, null, null, true);
        var service = new Mock<ITagsService>();
        service.Setup(s => s.CreateTagAsync(It.IsAny<CreateTagRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(tag);
        service.Setup(s => s.UpdateTagAsync(tag.Id, It.IsAny<UpdateTagRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(tag);
        service.Setup(s => s.UpdateTagAsync(Guid.Empty, It.IsAny<UpdateTagRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync((TagDto?)null);
        service.Setup(s => s.GetTagAsync(tag.Id, It.IsAny<CancellationToken>())).ReturnsAsync(tag);
        service.Setup(s => s.GetTagAsync(Guid.Empty, It.IsAny<CancellationToken>())).ReturnsAsync((TagDto?)null);
        service.Setup(s => s.SearchTagsAsync(It.IsAny<SearchTagsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([tag]);
        service.Setup(s => s.CreateRelationshipAsync(It.IsAny<CreateTagRelationshipRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(relationship);
        service.Setup(s => s.GetRelationshipsAsync(tag.Id, It.IsAny<CancellationToken>())).ReturnsAsync([relationship]);
        service.Setup(s => s.CreateProficiencyAsync(It.IsAny<CreateTagProficiencyRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(proficiency);
        service.Setup(s => s.SearchProficienciesAsync(It.IsAny<SearchTagProficienciesQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([proficiency]);

        (await new CreateTagCommandHandler(service.Object).Handle(new CreateTagCommand(new CreateTagRequest("Tag", TagType.Skill)), CancellationToken.None)).Should().Be(tag);
        (await new UpdateTagCommandHandler(service.Object).Handle(new UpdateTagCommand(tag.Id, new UpdateTagRequest()), CancellationToken.None)).Should().Be(tag);
        (await new GetTagQueryHandler(service.Object).Handle(new GetTagQuery(tag.Id), CancellationToken.None)).Should().Be(tag);
        (await new SearchTagsQueryHandler(service.Object).Handle(new SearchTagsQuery(), CancellationToken.None)).Should().ContainSingle();
        (await new CreateTagRelationshipCommandHandler(service.Object).Handle(new CreateTagRelationshipCommand(new CreateTagRelationshipRequest(tag.Id, relationship.TargetId, TagRelationshipType.Related)), CancellationToken.None)).Should().Be(relationship);
        (await new GetTagRelationshipsQueryHandler(service.Object).Handle(new GetTagRelationshipsQuery(tag.Id), CancellationToken.None)).Should().ContainSingle();
        (await new CreateTagProficiencyCommandHandler(service.Object).Handle(new CreateTagProficiencyCommand(new CreateTagProficiencyRequest("Expert", TagType.Skill, SkillProficiencyLevel.Expert)), CancellationToken.None)).Should().Be(proficiency);
        (await new SearchTagProficienciesQueryHandler(service.Object).Handle(new SearchTagProficienciesQuery(), CancellationToken.None)).Should().ContainSingle();

        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<SearchTagsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([tag]);
        sender.Setup(s => s.Send(It.Is<GetTagQuery>(q => q.Id == tag.Id), It.IsAny<CancellationToken>())).ReturnsAsync(tag);
        sender.Setup(s => s.Send(It.Is<GetTagQuery>(q => q.Id == Guid.Empty), It.IsAny<CancellationToken>())).ReturnsAsync((TagDto?)null);
        sender.Setup(s => s.Send(It.IsAny<CreateTagCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(tag);
        sender.Setup(s => s.Send(It.Is<UpdateTagCommand>(c => c.Id == tag.Id), It.IsAny<CancellationToken>())).ReturnsAsync(tag);
        sender.Setup(s => s.Send(It.Is<UpdateTagCommand>(c => c.Id == Guid.Empty), It.IsAny<CancellationToken>())).ReturnsAsync((TagDto?)null);
        sender.Setup(s => s.Send(It.IsAny<GetTagRelationshipsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([relationship]);
        sender.Setup(s => s.Send(It.IsAny<CreateTagRelationshipCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(relationship);
        sender.Setup(s => s.Send(It.IsAny<SearchTagProficienciesQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([proficiency]);
        sender.Setup(s => s.Send(It.IsAny<CreateTagProficiencyCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(proficiency);
        var controller = new TagsController(sender.Object);

        (await controller.Search(null, null, null, false, 0, 0, CancellationToken.None)).Should().ContainSingle();
        (await controller.Search("Tag", TagType.Skill, tag.TenantId, true, 1, 10, CancellationToken.None)).Should().ContainSingle();
        (await controller.Get(tag.Id, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.Get(Guid.Empty, CancellationToken.None)).Should().BeOfType<NotFoundResult>();
        (await controller.Create(new CreateTagRequest("Tag", TagType.Skill), CancellationToken.None)).Should().Be(tag);
        (await controller.Update(tag.Id, new UpdateTagRequest(), CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.Update(Guid.Empty, new UpdateTagRequest(), CancellationToken.None)).Should().BeOfType<NotFoundResult>();
        (await controller.GetRelationships(tag.Id, CancellationToken.None)).Should().ContainSingle();
        (await controller.CreateRelationship(new CreateTagRelationshipRequest(tag.Id, relationship.TargetId, TagRelationshipType.Related), CancellationToken.None)).Should().Be(relationship);
        (await controller.SearchProficiencies(null, null, false, CancellationToken.None)).Should().ContainSingle();
        (await controller.SearchProficiencies(TagType.Skill, SkillProficiencyLevel.Expert, true, CancellationToken.None)).Should().ContainSingle();
        (await controller.CreateProficiency(new CreateTagProficiencyRequest("Expert", TagType.Skill, SkillProficiencyLevel.Expert), CancellationToken.None)).Should().Be(proficiency);

        var modelBuilder = new ModelBuilder();
        new TagsModelConfiguration().Configure(modelBuilder);
        modelBuilder.Model.FindEntityType(typeof(CertificateTag))!.FindProperty(nameof(CertificateTag.Source))!.GetMaxLength().Should().Be(100);
        modelBuilder.Model.FindEntityType(typeof(TagRelationship))!.GetCheckConstraints().Should().Contain(constraint => constraint.Name == "CK_TagRelationships_NoSelfReference");

        var services = new ServiceCollection();
        services.AddTagsModule();
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(ITagsService));

        var module = new TagsModule();
        module.Name.Should().Be("Tags");
        module.Order.Should().Be(80);
        module.ConfigureServices(new ServiceCollection(), new ConfigurationBuilder().Build()).Should().NotBeNull();
        var endpoints = new Mock<IEndpointRouteBuilder>();
        module.MapEndpoints(endpoints.Object).Should().BeSameAs(endpoints.Object);
    }

    private static TestTagsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestTagsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestTagsDbContext(options);
    }

    private sealed class TestTagsDbContext(DbContextOptions<TestTagsDbContext> options) : DbContext(options), IApplicationDbContext
    {
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Database.BeginTransactionAsync(cancellationToken);

        protected override void OnModelCreating(ModelBuilder modelBuilder) => new TagsModelConfiguration().Configure(modelBuilder);
    }
}
