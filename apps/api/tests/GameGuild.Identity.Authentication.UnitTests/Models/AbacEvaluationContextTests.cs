using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Models;

public class AbacEvaluationContextTests
{
    [Fact]
    public void DefaultValues_ShouldBeEmpty()
    {
        var ctx = new AbacEvaluationContext();

        ctx.UserAttributes.Should().BeEmpty();
        ctx.ResourceAttributes.Should().BeEmpty();
        ctx.EnvironmentalAttributes.Should().BeEmpty();
        ctx.ActionAttributes.Should().BeEmpty();
        ctx.UserId.Should().BeNull();
        ctx.TenantId.Should().BeNull();
        ctx.ResourceId.Should().BeNull();
        ctx.ResourceType.Should().BeNull();
        ctx.ContentType.Should().BeNull();
        ctx.SessionId.Should().BeNull();
        ctx.ClientIpAddress.Should().BeNull();
        ctx.UserAgent.Should().BeNull();
    }

    [Fact]
    public void AddUserAttribute_ShouldAddToUserAttributes()
    {
        var ctx = new AbacEvaluationContext();

        ctx.AddUserAttribute("department", "Engineering");

        ctx.UserAttributes.Should().ContainKey("department");
        ctx.UserAttributes["department"].Should().Be("Engineering");
    }

    [Fact]
    public void AddUserAttribute_ShouldOverwriteExisting()
    {
        var ctx = new AbacEvaluationContext();
        ctx.AddUserAttribute("department", "Sales");

        ctx.AddUserAttribute("department", "Engineering");

        ctx.UserAttributes["department"].Should().Be("Engineering");
    }

    [Fact]
    public void AddResourceAttribute_ShouldAddToResourceAttributes()
    {
        var ctx = new AbacEvaluationContext();

        ctx.AddResourceAttribute("classification", "Confidential");

        ctx.ResourceAttributes.Should().ContainKey("classification");
        ctx.ResourceAttributes["classification"].Should().Be("Confidential");
    }

    [Fact]
    public void AddEnvironmentalAttribute_ShouldAddToEnvironmentalAttributes()
    {
        var ctx = new AbacEvaluationContext();

        ctx.AddEnvironmentalAttribute("ipAddress", "192.168.1.1");

        ctx.EnvironmentalAttributes.Should().ContainKey("ipAddress");
        ctx.EnvironmentalAttributes["ipAddress"].Should().Be("192.168.1.1");
    }

    [Fact]
    public void AddActionAttribute_ShouldAddToActionAttributes()
    {
        var ctx = new AbacEvaluationContext();

        ctx.AddActionAttribute("operation", "delete");

        ctx.ActionAttributes.Should().ContainKey("operation");
        ctx.ActionAttributes["operation"].Should().Be("delete");
    }

    [Fact]
    public void GetAttribute_UserCategory_ShouldReturnValue()
    {
        var ctx = new AbacEvaluationContext();
        ctx.AddUserAttribute("level", 5);

        var result = ctx.GetAttribute<int>("user", "level");

        result.Should().Be(5);
    }

    [Fact]
    public void GetAttribute_ResourceCategory_ShouldReturnValue()
    {
        var ctx = new AbacEvaluationContext();
        ctx.AddResourceAttribute("owner", "user-123");

        var result = ctx.GetAttribute<string>("resource", "owner");

        result.Should().Be("user-123");
    }

    [Fact]
    public void GetAttribute_EnvironmentCategory_ShouldReturnValue()
    {
        var ctx = new AbacEvaluationContext();
        ctx.AddEnvironmentalAttribute("time", "morning");

        var result = ctx.GetAttribute<string>("environment", "time");

        result.Should().Be("morning");
    }

    [Fact]
    public void GetAttribute_ActionCategory_ShouldReturnValue()
    {
        var ctx = new AbacEvaluationContext();
        ctx.AddActionAttribute("method", "POST");

        var result = ctx.GetAttribute<string>("action", "method");

        result.Should().Be("POST");
    }

    [Fact]
    public void GetAttribute_UnknownCategory_ShouldReturnDefault()
    {
        var ctx = new AbacEvaluationContext();

        var result = ctx.GetAttribute<string>("unknown", "key");

        result.Should().BeNull();
    }

    [Fact]
    public void GetAttribute_MissingKey_ShouldReturnDefault()
    {
        var ctx = new AbacEvaluationContext();

        var result = ctx.GetAttribute<int>("user", "nonexistent");

        result.Should().Be(0);
    }

    [Fact]
    public void GetAttribute_WrongType_ShouldReturnDefault()
    {
        var ctx = new AbacEvaluationContext();
        ctx.AddUserAttribute("level", "not-an-int");

        var result = ctx.GetAttribute<int>("user", "level");

        result.Should().Be(0);
    }

    [Fact]
    public void GetAttribute_CaseInsensitiveCategory()
    {
        var ctx = new AbacEvaluationContext();
        ctx.AddUserAttribute("dept", "IT");

        ctx.GetAttribute<string>("USER", "dept").Should().Be("IT");
        ctx.GetAttribute<string>("User", "dept").Should().Be("IT");
    }

    [Fact]
    public void HasAttribute_WhenExists_ShouldReturnTrue()
    {
        var ctx = new AbacEvaluationContext();
        ctx.AddUserAttribute("department", "IT");

        ctx.HasAttribute("user", "department").Should().BeTrue();
    }

    [Fact]
    public void HasAttribute_WhenNotExists_ShouldReturnFalse()
    {
        var ctx = new AbacEvaluationContext();

        ctx.HasAttribute("user", "department").Should().BeFalse();
    }

    [Fact]
    public void HasAttribute_ResourceCategory_ShouldWork()
    {
        var ctx = new AbacEvaluationContext();
        ctx.AddResourceAttribute("type", "course");

        ctx.HasAttribute("resource", "type").Should().BeTrue();
        ctx.HasAttribute("resource", "missing").Should().BeFalse();
    }

    [Fact]
    public void HasAttribute_EnvironmentCategory_ShouldWork()
    {
        var ctx = new AbacEvaluationContext();
        ctx.AddEnvironmentalAttribute("ip", "1.2.3.4");

        ctx.HasAttribute("environment", "ip").Should().BeTrue();
    }

    [Fact]
    public void HasAttribute_ActionCategory_ShouldWork()
    {
        var ctx = new AbacEvaluationContext();
        ctx.AddActionAttribute("method", "GET");

        ctx.HasAttribute("action", "method").Should().BeTrue();
    }

    [Fact]
    public void HasAttribute_UnknownCategory_ShouldReturnFalse()
    {
        var ctx = new AbacEvaluationContext();

        ctx.HasAttribute("unknown", "key").Should().BeFalse();
    }
}
