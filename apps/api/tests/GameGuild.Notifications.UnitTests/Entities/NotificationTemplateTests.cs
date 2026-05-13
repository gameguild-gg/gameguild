namespace GameGuild.Notifications.UnitTests.Entities;

public class NotificationTemplateTests
{
    [Fact]
    public void Create_Should_Initialize_Template()
    {
        var tenantId = Guid.NewGuid();

        var template = NotificationTemplate.Create(
            "welcome",
            "Welcome template",
            NotificationType.Onboarding,
            NotificationChannel.Email,
            "Welcome {{name}}",
            "Hello {{name}}",
            "Used during onboarding",
            "https://example.test/welcome",
            "https://example.test/icon.svg",
            NotificationPriority.High,
            tenantId,
            "Onboarding",
            "[\"name\"]");

        template.Code.Should().Be("welcome");
        template.Name.Should().Be("Welcome template");
        template.Type.Should().Be(NotificationType.Onboarding);
        template.Channel.Should().Be(NotificationChannel.Email);
        template.TitleTemplate.Should().Be("Welcome {{name}}");
        template.MessageTemplate.Should().Be("Hello {{name}}");
        template.Description.Should().Be("Used during onboarding");
        template.ActionUrlTemplate.Should().Be("https://example.test/welcome");
        template.DefaultIconUrl.Should().Be("https://example.test/icon.svg");
        template.DefaultPriority.Should().Be(NotificationPriority.High);
        template.TemplateTenantId.Should().NotBeNull();
        template.TemplateTenantId!.Value.Value.Should().Be(tenantId);
        template.Category.Should().Be("Onboarding");
        template.SupportedPlaceholders.Should().Be("[\"name\"]");
        template.IsActive.Should().BeTrue();
    }

    [Fact]
    public void UpdateMethods_Should_Modify_Template_State()
    {
        var template = NotificationTemplate.Create(
            "welcome",
            "Welcome template",
            NotificationType.Onboarding,
            NotificationChannel.Email,
            "Welcome {{name}}",
            "Hello {{name}}");

        template.UpdateContent("Hi {{name}}", "Updated body", "https://example.test/updated", "https://example.test/new-icon.svg");
        template.UpdateMetadata("Renamed", "Updated description", "General", NotificationPriority.Urgent);
        template.Deactivate();
        template.Activate();

        template.TitleTemplate.Should().Be("Hi {{name}}");
        template.MessageTemplate.Should().Be("Updated body");
        template.ActionUrlTemplate.Should().Be("https://example.test/updated");
        template.DefaultIconUrl.Should().Be("https://example.test/new-icon.svg");
        template.Name.Should().Be("Renamed");
        template.Description.Should().Be("Updated description");
        template.Category.Should().Be("General");
        template.DefaultPriority.Should().Be(NotificationPriority.Urgent);
        template.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Delete_Should_SoftDelete_Persisted_Template()
    {
        var template = NotificationTemplate.Create(
            "welcome",
            "Welcome template",
            NotificationType.Onboarding,
            NotificationChannel.Email,
            "Welcome {{name}}",
            "Hello {{name}}");

        template.Version = 1;

        template.Delete();

        template.IsDeleted.Should().BeTrue();
        template.DeletedAt.Should().NotBeNull();
    }
}
