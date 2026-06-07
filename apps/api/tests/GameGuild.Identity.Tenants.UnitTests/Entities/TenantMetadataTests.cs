using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Entities;

public class TenantMetadataTests
{
    [Fact]
    public void TenantMetadata_Partial_Constructor_Should_Map_Properties()
    {
        var metadata = new TenantMetadata(new { Industry = "Games", Type = "Studio" });

        metadata.Industry.Should().Be("Games");
        metadata.Type.Should().Be("Studio");
    }

    [Fact]
    public void SetCustomFields_Should_Serialize_And_Roundtrip()
    {
        var metadata = new TenantMetadata();
        var fields = new Dictionary<string, object?>
        {
            ["plan"] = "pro",
            ["seats"] = 10
        };

        metadata.SetCustomFields(fields);
        var roundtrip = metadata.GetCustomFields(NullLogger<TenantMetadata>.Instance);

        roundtrip["plan"]?.ToString().Should().Be("pro");
        roundtrip["seats"]?.ToString().Should().Be("10");
    }

    [Fact]
    public void GetCustomFields_Should_Return_Empty_On_Invalid_Json()
    {
        var metadata = new TenantMetadata { CustomFields = "{invalid json" };

        var result = metadata.GetCustomFields(NullLogger<TenantMetadata>.Instance);

        result.Should().BeEmpty();
    }

    [Fact]
    public void SetTags_Should_Serialize_And_Roundtrip()
    {
        var metadata = new TenantMetadata();
        metadata.SetTags(["alpha", "beta"]);

        var tags = metadata.GetTags(NullLogger<TenantMetadata>.Instance);

        tags.Should().ContainInOrder("alpha", "beta");
    }

    [Fact]
    public void GetTags_Should_Return_Empty_On_Invalid_Json()
    {
        var metadata = new TenantMetadata { Tags = "[invalid" };

        var tags = metadata.GetTags(NullLogger<TenantMetadata>.Instance);

        tags.Should().BeEmpty();
    }

    [Fact]
    public void SetExternalReferences_Should_Serialize_And_Roundtrip()
    {
        var metadata = new TenantMetadata();
        metadata.SetExternalReferences(new Dictionary<string, string> { ["crm"] = "abc" });

        var result = metadata.GetExternalReferences(NullLogger<TenantMetadata>.Instance);

        result["crm"].Should().Be("abc");
    }

    [Fact]
    public void GetExternalReferences_Should_Return_Empty_On_Invalid_Json()
    {
        var metadata = new TenantMetadata { ExternalReferences = "{invalid" };

        var result = metadata.GetExternalReferences(NullLogger<TenantMetadata>.Instance);

        result.Should().BeEmpty();
    }

    [Fact]
    public void SetBusinessInfo_Should_Serialize_And_Roundtrip()
    {
        var metadata = new TenantMetadata();
        metadata.SetBusinessInfo(new Dictionary<string, object?> { ["legalName"] = "Acme" });

        var result = metadata.GetBusinessInfo(NullLogger<TenantMetadata>.Instance);

        result["legalName"]?.ToString().Should().Be("Acme");
    }

    [Fact]
    public void GetBusinessInfo_Should_Return_Empty_On_Invalid_Json()
    {
        var metadata = new TenantMetadata { BusinessInfo = "{invalid" };

        var result = metadata.GetBusinessInfo(NullLogger<TenantMetadata>.Instance);

        result.Should().BeEmpty();
    }

    [Fact]
    public void SetContactInfo_Should_Serialize_And_Roundtrip()
    {
        var metadata = new TenantMetadata();
        metadata.SetContactInfo(new Dictionary<string, object?> { ["email"] = "support@example.com" });

        var result = metadata.GetContactInfo(NullLogger<TenantMetadata>.Instance);

        result["email"]?.ToString().Should().Be("support@example.com");
    }

    [Fact]
    public void GetContactInfo_Should_Return_Empty_On_Invalid_Json()
    {
        var metadata = new TenantMetadata { ContactInfo = "{invalid" };

        var result = metadata.GetContactInfo(NullLogger<TenantMetadata>.Instance);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Getters_Should_Return_Empty_When_Serialized_Value_Is_Null()
    {
        var metadata = new TenantMetadata
        {
            CustomFields = "null",
            Tags = "null",
            ExternalReferences = "null",
            BusinessInfo = "null",
            ContactInfo = "null"
        };

        metadata.GetCustomFields().Should().BeEmpty();
        metadata.GetTags().Should().BeEmpty();
        metadata.GetExternalReferences().Should().BeEmpty();
        metadata.GetBusinessInfo().Should().BeEmpty();
        metadata.GetContactInfo().Should().BeEmpty();
    }

    [Fact]
    public void UpdateCategorization_Should_Update_Provided_Values()
    {
        var metadata = new TenantMetadata();

        metadata.UpdateCategorization(industry: "Gaming", size: TenantSize.Medium, type: "Studio");

        metadata.Industry.Should().Be("Gaming");
        metadata.Size.Should().Be(TenantSize.Medium);
        metadata.Type.Should().Be("Studio");
    }

    [Fact]
    public void UpdateNotes_Should_Set_Notes()
    {
        var metadata = new TenantMetadata();

        metadata.UpdateNotes("Important tenant");

        metadata.Notes.Should().Be("Important tenant");
    }

    [Fact]
    public void Create_Should_Set_TenantId_And_Optional_Fields()
    {
        var tenantId = Guid.NewGuid();
        var custom = new Dictionary<string, object?> { ["tier"] = "pro" };
        var tags = new List<string> { "pro", "priority" };

        var metadata = TenantMetadata.Create(tenantId, custom, tags);

        metadata.TenantId.Should().Be(tenantId);
        metadata.GetCustomFields().Should().ContainKey("tier");
        metadata.GetTags().Should().Contain(tags);
    }
}
