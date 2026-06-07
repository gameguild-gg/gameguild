using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Commands;

public sealed class TenantMetadataOperationsTests
{
    private readonly Mock<ITenantMetadataRepository> _repository = new();
    private TenantMetadata? _storedMetadata;

    public TenantMetadataOperationsTests()
    {
        _repository
            .Setup(repository => repository.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _storedMetadata);
        _repository
            .Setup(repository => repository.AddAsync(It.IsAny<TenantMetadata>(), It.IsAny<CancellationToken>()))
            .Callback<TenantMetadata, CancellationToken>((metadata, _) => _storedMetadata = metadata)
            .Returns(Task.CompletedTask);
        _repository
            .Setup(repository => repository.UpdateAsync(It.IsAny<TenantMetadata>(), It.IsAny<CancellationToken>()))
            .Callback<TenantMetadata, CancellationToken>((metadata, _) => _storedMetadata = metadata)
            .Returns(Task.CompletedTask);
        _repository
            .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task CustomFieldHandlers_Should_CreateAndMergeTenantFields()
    {
        var tenantId = Guid.NewGuid();
        var updateHandler = new UpdateTenantCustomFieldsCommandHandler(_repository.Object);
        var queryHandler = new GetTenantCustomFieldsQueryHandler(_repository.Object);

        await updateHandler.Handle(
            new UpdateTenantCustomFieldsCommand(
                tenantId,
                new UpdateTenantCustomFieldsRequest(new Dictionary<string, object?> { ["market"] = "luxury" })),
            CancellationToken.None);

        var result = await queryHandler.Handle(new GetTenantCustomFieldsQuery(tenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Keys.Should().Contain("market");
        _repository.Verify(repository => repository.AddAsync(It.IsAny<TenantMetadata>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(repository => repository.UpdateAsync(It.IsAny<TenantMetadata>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TagHandlers_Should_MergeThenReplaceNormalizedTags()
    {
        var tenantId = Guid.NewGuid();
        var updateHandler = new UpdateTenantTagsCommandHandler(_repository.Object);
        var replaceHandler = new ReplaceTenantTagsCommandHandler(_repository.Object);
        var queryHandler = new GetTenantTagsQueryHandler(_repository.Object);

        await updateHandler.Handle(
            new UpdateTenantTagsCommand(
                tenantId,
                new UpdateTenantTagsRequest(new List<string> { "owner", "brokerage" })),
            CancellationToken.None);

        var merged = await queryHandler.Handle(new GetTenantTagsQuery(tenantId), CancellationToken.None);
        merged.Should().Equal("brokerage", "owner");

        await replaceHandler.Handle(
            new ReplaceTenantTagsCommand(
                tenantId,
                new ReplaceTenantTagsRequest(new List<string> { "Enterprise", "enterprise", " " })),
            CancellationToken.None);

        var replaced = await queryHandler.Handle(new GetTenantTagsQuery(tenantId), CancellationToken.None);
        replaced.Should().Equal("Enterprise");
    }

    [Fact]
    public async Task MetadataHandlers_Should_UpdateReplaceAndReadCompleteMetadata()
    {
        var tenantId = Guid.NewGuid();
        _storedMetadata = TenantMetadata.Create(
            tenantId,
            new Dictionary<string, object?> { ["existing"] = "kept" },
            new List<string> { "portfolio" });
        _storedMetadata.CreatedAt = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Unspecified);
        _storedMetadata.UpdatedAt = new DateTime(2024, 2, 3, 4, 5, 6, DateTimeKind.Utc);

        var updateHandler = new UpdateTenantMetadataCommandHandler(_repository.Object);
        var replaceHandler = new ReplaceTenantMetadataCommandHandler(_repository.Object);
        var queryHandler = new GetTenantMetadataQueryHandler(_repository.Object);

        await updateHandler.Handle(
            new UpdateTenantMetadataCommand(
                tenantId,
                new UpdateTenantMetadataRequest(
                    new Dictionary<string, object?> { ["market"] = "luxury" },
                    new List<string> { "Brokerage", "brokerage", " " },
                    new Dictionary<string, string> { ["crm"] = "tenant-123" },
                    new UpdateTenantBusinessInfoRequest("Real Estate", "Large", "Brokerage", "US", new List<string> { "SOC2" }),
                    new UpdateTenantContactInfoRequest(
                        "Ada Lovelace",
                        "ada@example.test",
                        "+15551234567",
                        "Ada Properties",
                        new UpdateTenantAddressRequest("1 Main", "Austin", "TX", "78701", "US"),
                        "https://ada.example.test"),
                    "initial note")),
            CancellationToken.None);

        await updateHandler.Handle(
            new UpdateTenantMetadataCommand(
                tenantId,
                new UpdateTenantMetadataRequest(
                    null,
                    null,
                    null,
                    new UpdateTenantBusinessInfoRequest(null, "NotASize", null, null, null),
                    new UpdateTenantContactInfoRequest(null, null, null, null, null, null),
                    null)),
            CancellationToken.None);

        await updateHandler.Handle(
            new UpdateTenantMetadataCommand(
                tenantId,
                new UpdateTenantMetadataRequest(
                    null,
                    null,
                    null,
                    new UpdateTenantBusinessInfoRequest(null, null, null, null, null),
                    new UpdateTenantContactInfoRequest(
                        null,
                        null,
                        null,
                        null,
                        new UpdateTenantAddressRequest(null, null, null, null, null),
                        null),
                    null)),
            CancellationToken.None);

        await replaceHandler.Handle(
            new ReplaceTenantMetadataCommand(
                tenantId,
                new ReplaceTenantMetadataRequest(
                    new Dictionary<string, object?> { ["segment"] = "investor" },
                    new List<string> { "Owner", "owner", "Enterprise" },
                    new Dictionary<string, string> { ["erp"] = "acct-9" },
                    new UpdateTenantBusinessInfoRequest("Finance", "Small", "Owner", "EU", new List<string> { "GDPR" }),
                    new UpdateTenantContactInfoRequest(
                        "Grace Hopper",
                        "grace@example.test",
                        "+15559876543",
                        "Grace Holdings",
                        new UpdateTenantAddressRequest("2 Second", "Boston", "MA", "02110", "US"),
                        "https://grace.example.test"),
                    "replacement note")),
            CancellationToken.None);

        var result = await queryHandler.Handle(new GetTenantMetadataQuery(tenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.CustomFields.Should().ContainKey("segment");
        result.Tags.Should().Equal("Enterprise", "Owner");
        result.ExternalReferences["erp"].Should().Be("acct-9");
        result.BusinessInfo.Industry.Should().Be("Finance");
        result.BusinessInfo.OrganizationSize.Should().Be("Small");
        result.BusinessInfo.TenantType.Should().Be("Owner");
        result.BusinessInfo.GeographicRegion.Should().Be("EU");
        result.BusinessInfo.ComplianceRequirements.Should().Equal("GDPR");
        result.ContactInfo.PrimaryContactName.Should().Be("Grace Hopper");
        result.ContactInfo.Address!.City.Should().Be("Boston");
        result.AdminNotes.Should().Be("replacement note");
        result.CreatedAt.Offset.Should().Be(TimeSpan.Zero);
        result.UpdatedAt.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task MetadataHandlers_Should_ReturnFallbacksForMissingOrInvalidJson()
    {
        var missingTenantId = Guid.NewGuid();
        var queryHandler = new GetTenantMetadataQueryHandler(_repository.Object);

        var missing = await queryHandler.Handle(new GetTenantMetadataQuery(missingTenantId), CancellationToken.None);

        missing.Should().NotBeNull();
        missing!.Id.Should().Be(missingTenantId);
        missing.CustomFields.Should().BeEmpty();
        missing.Tags.Should().BeEmpty();
        missing.BusinessInfo.ComplianceRequirements.Should().BeEmpty();
        missing.ContactInfo.Address.Should().BeNull();

        var invalidTenantId = Guid.NewGuid();
        _storedMetadata = TenantMetadata.Create(invalidTenantId);
        _storedMetadata.BusinessInfo = "{invalid";
        _storedMetadata.ContactInfo = "null";

        var invalid = await queryHandler.Handle(new GetTenantMetadataQuery(invalidTenantId), CancellationToken.None);

        invalid.Should().NotBeNull();
        invalid!.BusinessInfo.Industry.Should().BeNull();
        invalid.ContactInfo.PrimaryContactEmail.Should().BeNull();

        _storedMetadata.BusinessInfo = """
                                       {
                                         "industry": null,
                                         "organizationSize": null,
                                         "tenantType": null,
                                         "geographicRegion": null,
                                         "complianceRequirements": null
                                       }
                                       """;
        _storedMetadata.ContactInfo = " ";
        _storedMetadata.Industry = "Hospitality";
        _storedMetadata.Size = TenantSize.Medium;
        _storedMetadata.Type = "Operator";

        var fallback = await queryHandler.Handle(new GetTenantMetadataQuery(invalidTenantId), CancellationToken.None);

        fallback.Should().NotBeNull();
        fallback!.BusinessInfo.Industry.Should().Be("Hospitality");
        fallback.BusinessInfo.OrganizationSize.Should().Be("Medium");
        fallback.BusinessInfo.TenantType.Should().Be("Operator");
        fallback.BusinessInfo.ComplianceRequirements.Should().BeEmpty();
        fallback.ContactInfo.Address.Should().BeNull();

        var updateHandler = new UpdateTenantMetadataCommandHandler(_repository.Object);
        await updateHandler.Handle(
            new UpdateTenantMetadataCommand(
                invalidTenantId,
                new UpdateTenantMetadataRequest(
                    null,
                    null,
                    null,
                    null,
                    new UpdateTenantContactInfoRequest(
                        null,
                        null,
                        null,
                        null,
                        new UpdateTenantAddressRequest(null, null, null, null, null),
                        null),
                    null)),
            CancellationToken.None);

        var updated = await queryHandler.Handle(new GetTenantMetadataQuery(invalidTenantId), CancellationToken.None);

        updated.Should().NotBeNull();
        updated!.ContactInfo.Address.Should().NotBeNull();
        updated.ContactInfo.Address!.Street.Should().BeNull();
    }
}
