using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using Xunit;

namespace GameGuild.Identity.Context.UnitTests.Actors;

public class ActorAttributesTests
{
    [Fact]
    public void FullName_Should_Return_Null_When_No_Names()
    {
        var attrs = new ActorAttributes();

        attrs.FullName.Should().BeNull();
    }

    [Fact]
    public void FullName_Should_Combine_Names_When_Present()
    {
        var attrs = new ActorAttributes { FirstName = "Ada", LastName = "Lovelace" };

        attrs.FullName.Should().Be("Ada Lovelace");
    }

    [Fact]
    public void FullName_Should_Trim_When_Only_FirstName()
    {
        var attrs = new ActorAttributes { FirstName = "Ada" };

        attrs.FullName.Should().Be("Ada");
    }

    [Fact]
    public void WithCustomAttribute_Should_Add_Custom_Value()
    {
        var attrs = ActorAttributes.Empty;

        var updated = attrs.WithCustomAttribute("region", "us-east");

        updated.Custom.Should().ContainKey("region").WhoseValue.Should().Be("us-east");
        attrs.Custom.Should().BeEmpty();
    }

    [Fact]
    public void ToDictionary_Should_Include_Typed_And_Custom_Attributes()
    {
        var attrs = new ActorAttributes
        {
            Email = "user@example.com",
            EmailVerified = true,
            TenantRole = "Admin",
            Custom = new Dictionary<string, string> { ["custom"] = "value" }
        };

        var dict = attrs.ToDictionary();

        dict["email"].Should().Be("user@example.com");
        dict["email_verified"].Should().Be("true");
        dict["tenant_role"].Should().Be("Admin");
        dict["custom"].Should().Be("value");
    }

    [Fact]
    public void GetCustomAttribute_Should_Return_Null_When_Missing()
    {
        var attrs = new ActorAttributes();

        attrs.GetCustomAttribute("missing").Should().BeNull();
    }

    [Fact]
    public void FromDictionary_Should_Map_Standard_And_Custom_Fields()
    {
        var data = new Dictionary<string, string>
        {
            ["email"] = "user@example.com",
            ["email_verified"] = "true",
            ["given_name"] = "Ada",
            ["family_name"] = "Lovelace",
            ["custom_key"] = "custom_value"
        };

        var attrs = ActorAttributes.FromDictionary(data);

        attrs.Email.Should().Be("user@example.com");
        attrs.EmailVerified.Should().BeTrue();
        attrs.FirstName.Should().Be("Ada");
        attrs.LastName.Should().Be("Lovelace");
        attrs.Custom.Should().ContainKey("custom_key").WhoseValue.Should().Be("custom_value");
    }

    [Fact]
    public void FromDictionary_Should_Return_Empty_When_Null()
    {
        var attrs = ActorAttributes.FromDictionary(null);

        attrs.Should().BeSameAs(ActorAttributes.Empty);
    }
}
