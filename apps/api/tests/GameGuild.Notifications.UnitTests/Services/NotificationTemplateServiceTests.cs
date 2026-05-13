using GameGuild.Notifications.UnitTests.Infrastructure;

namespace GameGuild.Notifications.UnitTests.Services;

public class NotificationTemplateServiceTests
{
    [Fact]
    public async Task GetTemplateByCodeAsync_Should_Return_NotFound_When_Template_Does_Not_Exist()
    {
        using var context = CreateContext();
        var subject = new NotificationTemplateService(new ApplicationDbContextAdapter(context), NullLogger<NotificationTemplateService>.Instance);

        var result = await subject.GetTemplateByCodeAsync("missing");

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Template.NotFound");
    }

    [Fact]
    public async Task GetTemplateByCodeAsync_Should_Return_Template_When_It_Exists()
    {
        using var context = CreateContext();
        var template = NotificationTemplate.Create("welcome", "Welcome", NotificationType.Onboarding, NotificationChannel.Email, "Welcome", "Hello");
        context.NotificationTemplates.Add(template);
        await context.SaveChangesAsync();
        var subject = new NotificationTemplateService(new ApplicationDbContextAdapter(context), NullLogger<NotificationTemplateService>.Instance);

        var result = await subject.GetTemplateByCodeAsync("welcome");

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(template.Id);
    }

    [Fact]
    public async Task GetTemplatesAsync_Should_Filter_By_Category_And_Active_State()
    {
        using var context = CreateContext();
        var active = NotificationTemplate.Create("welcome", "Welcome", NotificationType.Onboarding, NotificationChannel.Email, "Welcome", "Hello", category: "Onboarding");
        var inactive = NotificationTemplate.Create("billing", "Billing", NotificationType.Billing, NotificationChannel.Email, "Billing", "Body", category: "Billing");
        inactive.Deactivate();
        context.NotificationTemplates.AddRange(active, inactive);
        await context.SaveChangesAsync();
        var subject = new NotificationTemplateService(new ApplicationDbContextAdapter(context), NullLogger<NotificationTemplateService>.Instance);

        var result = await subject.GetTemplatesAsync("Onboarding", true);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(template => template.Code == "welcome");
    }

    [Fact]
    public async Task CreateTemplateAsync_Should_Return_Conflict_When_Code_Already_Exists()
    {
        using var context = CreateContext();
        context.NotificationTemplates.Add(NotificationTemplate.Create("welcome", "Welcome", NotificationType.Onboarding, NotificationChannel.Email, "Welcome", "Hello"));
        await context.SaveChangesAsync();
        var subject = new NotificationTemplateService(new ApplicationDbContextAdapter(context), NullLogger<NotificationTemplateService>.Instance);

        var result = await subject.CreateTemplateAsync("welcome", "Duplicate", NotificationType.System, NotificationChannel.InApp, "Title", "Body");

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Template.DuplicateCode");
    }

    [Fact]
    public async Task CreateTemplateAsync_Should_Create_Template_When_Code_Is_New()
    {
        using var context = CreateContext();
        var subject = new NotificationTemplateService(new ApplicationDbContextAdapter(context), NullLogger<NotificationTemplateService>.Instance);

        var result = await subject.CreateTemplateAsync("welcome", "Welcome", NotificationType.Onboarding, NotificationChannel.Email, "Welcome {{name}}", "Hello {{name}}", "desc", "https://example.test", "Onboarding");

        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("welcome");
        context.NotificationTemplates.Should().ContainSingle();
    }

    [Fact]
    public async Task UpdateTemplateAsync_Should_Return_NotFound_When_Template_Does_Not_Exist()
    {
        using var context = CreateContext();
        var subject = new NotificationTemplateService(new ApplicationDbContextAdapter(context), NullLogger<NotificationTemplateService>.Instance);

        var result = await subject.UpdateTemplateAsync(Guid.NewGuid(), "Updated", "Body", "https://example.test", false);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Template.NotFound");
    }

    [Fact]
    public async Task UpdateTemplateAsync_Should_Update_Content_And_Activation_State()
    {
        using var context = CreateContext();
        var template = NotificationTemplate.Create("welcome", "Welcome", NotificationType.Onboarding, NotificationChannel.Email, "Welcome", "Hello");
        context.NotificationTemplates.Add(template);
        await context.SaveChangesAsync();
        var subject = new NotificationTemplateService(new ApplicationDbContextAdapter(context), NullLogger<NotificationTemplateService>.Instance);

        var deactivate = await subject.UpdateTemplateAsync(template.Id, "Hi", "Updated body", "https://example.test/updated", false);
        var activate = await subject.UpdateTemplateAsync(template.Id, isActive: true);

        deactivate.IsSuccess.Should().BeTrue();
        activate.IsSuccess.Should().BeTrue();
        template.TitleTemplate.Should().Be("Hi");
        template.MessageTemplate.Should().Be("Updated body");
        template.ActionUrlTemplate.Should().Be("https://example.test/updated");
        template.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ReplacePlaceholders_Should_Replace_Known_Placeholders_And_Leave_Others()
    {
        using var context = CreateContext();
        var subject = new NotificationTemplateService(new ApplicationDbContextAdapter(context), NullLogger<NotificationTemplateService>.Instance);

        var result = subject.ReplacePlaceholders(
            "Welcome {{name}} to {{product}} and {{unknown}}",
            new Dictionary<string, string>
            {
                ["name"] = "Ada",
                ["product"] = "GameGuild"
            });

        result.Should().Be("Welcome Ada to GameGuild and {{unknown}}");
    }

    private static NotificationsTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NotificationsTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new NotificationsTestDbContext(options);
    }

    private sealed class ApplicationDbContextAdapter(NotificationsTestDbContext inner) : IApplicationDbContext
    {
        public DbSet<T> Set<T>() where T : class => inner.Set<T>();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => inner.SaveChangesAsync(cancellationToken);

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Mock.Of<IDbContextTransaction>());
    }
}
