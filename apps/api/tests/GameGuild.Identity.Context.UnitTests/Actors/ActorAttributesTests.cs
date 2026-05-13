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
    public void ToDictionary_Should_Include_All_Supported_Attributes_When_Populated()
    {
        var authenticatedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(55);
        var joinedAt = DateTimeOffset.UtcNow.AddDays(-30);
        var managerId = Guid.NewGuid();
        var attrs = new ActorAttributes
        {
            Email = "user@example.com",
            EmailVerified = true,
            DisplayName = "User Name",
            FirstName = "User",
            LastName = "Name",
            Username = "username",
            PictureUrl = "https://example.com/picture.png",
            MfaVerified = true,
            MfaMethod = "totp",
            IpAddress = "127.0.0.1",
            UserAgent = "agent",
            DeviceFingerprint = "device-1",
            TrustedDevice = true,
            SessionId = "session-1",
            TokenId = "token-1",
            AuthenticatedAt = authenticatedAt,
            TokenExpiresAt = expiresAt,
            Department = "Finance",
            JobTitle = "Manager",
            ManagerId = managerId,
            OrganizationUnit = "/Finance",
            EmployeeId = "emp-1",
            CostCenter = "cc-1",
            TenantRole = "Owner",
            TenantJoinedAt = joinedAt,
            TenantMembershipStatus = "Active",
            IdentityProvider = "google",
            ExternalSubjectId = "external-1",
            Locale = "en-US",
            Timezone = "America/Sao_Paulo",
            Custom = new Dictionary<string, string> { ["custom"] = "value" }
        };

        var dict = attrs.ToDictionary();

        dict.Should().Contain
        (
            new KeyValuePair<string, string>("email", "user@example.com")
        ).And.Contain(new KeyValuePair<string, string>("email_verified", "true"))
         .And.Contain(new KeyValuePair<string, string>("name", "User Name"))
         .And.Contain(new KeyValuePair<string, string>("given_name", "User"))
         .And.Contain(new KeyValuePair<string, string>("family_name", "Name"))
         .And.Contain(new KeyValuePair<string, string>("preferred_username", "username"))
         .And.Contain(new KeyValuePair<string, string>("picture", "https://example.com/picture.png"))
         .And.Contain(new KeyValuePair<string, string>("mfa_verified", "true"))
         .And.Contain(new KeyValuePair<string, string>("mfa_method", "totp"))
         .And.Contain(new KeyValuePair<string, string>("ip_address", "127.0.0.1"))
         .And.Contain(new KeyValuePair<string, string>("user_agent", "agent"))
         .And.Contain(new KeyValuePair<string, string>("device_fingerprint", "device-1"))
         .And.Contain(new KeyValuePair<string, string>("trusted_device", "true"))
         .And.Contain(new KeyValuePair<string, string>("session_id", "session-1"))
         .And.Contain(new KeyValuePair<string, string>("jti", "token-1"))
         .And.Contain(new KeyValuePair<string, string>("auth_time", authenticatedAt.ToUnixTimeSeconds().ToString()))
         .And.Contain(new KeyValuePair<string, string>("exp", expiresAt.ToUnixTimeSeconds().ToString()))
         .And.Contain(new KeyValuePair<string, string>("department", "Finance"))
         .And.Contain(new KeyValuePair<string, string>("job_title", "Manager"))
         .And.Contain(new KeyValuePair<string, string>("manager_id", managerId.ToString()))
         .And.Contain(new KeyValuePair<string, string>("org_unit", "/Finance"))
         .And.Contain(new KeyValuePair<string, string>("employee_id", "emp-1"))
         .And.Contain(new KeyValuePair<string, string>("cost_center", "cc-1"))
         .And.Contain(new KeyValuePair<string, string>("tenant_role", "Owner"))
         .And.Contain(new KeyValuePair<string, string>("tenant_joined_at", joinedAt.ToString("O")))
         .And.Contain(new KeyValuePair<string, string>("tenant_membership_status", "Active"))
         .And.Contain(new KeyValuePair<string, string>("idp", "google"))
         .And.Contain(new KeyValuePair<string, string>("external_sub", "external-1"))
         .And.Contain(new KeyValuePair<string, string>("locale", "en-US"))
         .And.Contain(new KeyValuePair<string, string>("zoneinfo", "America/Sao_Paulo"))
         .And.Contain(new KeyValuePair<string, string>("custom", "value"));
    }

    [Fact]
    public void ToDictionary_Should_Exclude_Unset_Values_When_Default()
    {
        var attrs = new ActorAttributes();

        var dict = attrs.ToDictionary();

        dict.Should().BeEmpty();
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
    public void FromDictionary_Should_Map_All_Parsed_Fields_When_Values_Are_Valid()
    {
        var authenticatedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(55);
        var joinedAt = DateTimeOffset.UtcNow.AddDays(-30);
        var managerId = Guid.NewGuid();
        var expectedAuthenticatedAt = DateTimeOffset.FromUnixTimeSeconds(authenticatedAt.ToUnixTimeSeconds());
        var expectedExpiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresAt.ToUnixTimeSeconds());
        var data = new Dictionary<string, string>
        {
            ["email"] = "user@example.com",
            ["email_verified"] = "true",
            ["name"] = "User Name",
            ["given_name"] = "User",
            ["family_name"] = "Name",
            ["preferred_username"] = "username",
            ["picture"] = "https://example.com/picture.png",
            ["mfa_verified"] = "true",
            ["mfa_method"] = "totp",
            ["ip_address"] = "127.0.0.1",
            ["user_agent"] = "agent",
            ["device_fingerprint"] = "device-1",
            ["trusted_device"] = "true",
            ["session_id"] = "session-1",
            ["jti"] = "token-1",
            ["auth_time"] = authenticatedAt.ToUnixTimeSeconds().ToString(),
            ["exp"] = expiresAt.ToUnixTimeSeconds().ToString(),
            ["department"] = "Finance",
            ["job_title"] = "Manager",
            ["manager_id"] = managerId.ToString(),
            ["org_unit"] = "/Finance",
            ["employee_id"] = "emp-1",
            ["cost_center"] = "cc-1",
            ["tenant_role"] = "Owner",
            ["tenant_joined_at"] = joinedAt.ToString("O"),
            ["tenant_membership_status"] = "Active",
            ["idp"] = "google",
            ["external_sub"] = "external-1",
            ["locale"] = "en-US",
            ["zoneinfo"] = "America/Sao_Paulo",
            ["custom_key"] = "custom_value"
        };

        var attrs = ActorAttributes.FromDictionary(data);

        attrs.Email.Should().Be("user@example.com");
        attrs.EmailVerified.Should().BeTrue();
        attrs.DisplayName.Should().Be("User Name");
        attrs.FirstName.Should().Be("User");
        attrs.LastName.Should().Be("Name");
        attrs.Username.Should().Be("username");
        attrs.PictureUrl.Should().Be("https://example.com/picture.png");
        attrs.MfaVerified.Should().BeTrue();
        attrs.MfaMethod.Should().Be("totp");
        attrs.IpAddress.Should().Be("127.0.0.1");
        attrs.UserAgent.Should().Be("agent");
        attrs.DeviceFingerprint.Should().Be("device-1");
        attrs.TrustedDevice.Should().BeTrue();
        attrs.SessionId.Should().Be("session-1");
        attrs.TokenId.Should().Be("token-1");
        attrs.AuthenticatedAt.Should().Be(expectedAuthenticatedAt);
        attrs.TokenExpiresAt.Should().Be(expectedExpiresAt);
        attrs.Department.Should().Be("Finance");
        attrs.JobTitle.Should().Be("Manager");
        attrs.ManagerId.Should().Be(managerId);
        attrs.OrganizationUnit.Should().Be("/Finance");
        attrs.EmployeeId.Should().Be("emp-1");
        attrs.CostCenter.Should().Be("cc-1");
        attrs.TenantRole.Should().Be("Owner");
        attrs.TenantJoinedAt.Should().Be(joinedAt);
        attrs.TenantMembershipStatus.Should().Be("Active");
        attrs.IdentityProvider.Should().Be("google");
        attrs.ExternalSubjectId.Should().Be("external-1");
        attrs.Locale.Should().Be("en-US");
        attrs.Timezone.Should().Be("America/Sao_Paulo");
        attrs.Custom.Should().ContainKey("custom_key").WhoseValue.Should().Be("custom_value");
    }

    [Fact]
    public void FromDictionary_Should_Default_To_False_Or_Null_When_Parsed_Values_Are_Invalid()
    {
        var data = new Dictionary<string, string>
        {
            ["email_verified"] = "false",
            ["mfa_verified"] = "not-a-bool",
            ["trusted_device"] = "false",
            ["auth_time"] = "not-a-number",
            ["exp"] = "not-a-number",
            ["manager_id"] = "not-a-guid",
            ["tenant_joined_at"] = "not-a-date"
        };

        var attrs = ActorAttributes.FromDictionary(data);

        attrs.EmailVerified.Should().BeFalse();
        attrs.MfaVerified.Should().BeFalse();
        attrs.TrustedDevice.Should().BeFalse();
        attrs.AuthenticatedAt.Should().BeNull();
        attrs.TokenExpiresAt.Should().BeNull();
        attrs.ManagerId.Should().BeNull();
        attrs.TenantJoinedAt.Should().BeNull();
    }

    [Fact]
    public void FromDictionary_Should_Return_Empty_When_Null()
    {
        var attrs = ActorAttributes.FromDictionary(null);

        attrs.Should().BeSameAs(ActorAttributes.Empty);
    }

    [Fact]
    public void FromDictionary_Should_Return_Empty_When_Empty()
    {
        var attrs = ActorAttributes.FromDictionary(new Dictionary<string, string>());

        attrs.Should().BeSameAs(ActorAttributes.Empty);
    }
}
