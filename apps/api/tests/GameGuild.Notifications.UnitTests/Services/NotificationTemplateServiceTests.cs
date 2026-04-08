using FluentAssertions;
using GameGuild.Notifications;
using GameGuild.Notifications.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace GameGuild.Notifications.UnitTests.Services;

public class NotificationTemplateServiceTests
{
    private readonly Mock<IApplicationDbContext> _contextMock = new();
    private readonly NotificationTemplateService _sut;

    public NotificationTemplateServiceTests()
    {
        _sut = new NotificationTemplateService(
            _contextMock.Object,
            NullLogger<NotificationTemplateService>.Instance);
    }

    private void SetupTemplateDbSet(List<NotificationTemplate> data)
    {
        var mock = data.AsQueryable().BuildMockDbSet();
        _contextMock.Setup(c => c.Set<NotificationTemplate>()).Returns(mock.Object);
        _contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    #region GetTemplateByCodeAsync

    [Fact]
    public async Task GetTemplateByCodeAsync_WhenTemplateExists_ReturnsSuccess()
    {
        // Arrange
        var template = NotificationTemplate.Create(
            "welcome_email", "Welcome Email", NotificationType.Onboarding,
            NotificationChannel.Email, "Welcome {{name}}!", "Hello {{name}}!");
        SetupTemplateDbSet([template]);

        // Act
        var result = await _sut.GetTemplateByCodeAsync("welcome_email");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("welcome_email");
        result.Value.Name.Should().Be("Welcome Email");
    }

    [Fact]
    public async Task GetTemplateByCodeAsync_WhenTemplateNotFound_ReturnsFailure()
    {
        // Arrange
        SetupTemplateDbSet([]);

        // Act
        var result = await _sut.GetTemplateByCodeAsync("nonexistent");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Template.NotFound");
    }

    #endregion

    #region GetTemplatesAsync

    [Fact]
    public async Task GetTemplatesAsync_ReturnsAllTemplates()
    {
        // Arrange
        var templates = new List<NotificationTemplate>
        {
            NotificationTemplate.Create("code1", "Template 1", NotificationType.System, NotificationChannel.InApp, "T1", "M1"),
            NotificationTemplate.Create("code2", "Template 2", NotificationType.Security, NotificationChannel.Email, "T2", "M2"),
            NotificationTemplate.Create("code3", "Template 3", NotificationType.Marketing, NotificationChannel.Push, "T3", "M3")
        };
        SetupTemplateDbSet(templates);

        // Act
        var result = await _sut.GetTemplatesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetTemplatesAsync_WithCategoryFilter_ReturnsFilteredTemplates()
    {
        // Arrange
        var templates = new List<NotificationTemplate>
        {
            NotificationTemplate.Create("code1", "Template 1", NotificationType.System, NotificationChannel.InApp, "T1", "M1", category: "onboarding"),
            NotificationTemplate.Create("code2", "Template 2", NotificationType.System, NotificationChannel.InApp, "T2", "M2", category: "security"),
            NotificationTemplate.Create("code3", "Template 3", NotificationType.System, NotificationChannel.InApp, "T3", "M3", category: "onboarding")
        };
        SetupTemplateDbSet(templates);

        // Act
        var result = await _sut.GetTemplatesAsync(category: "onboarding");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(t => t.Category == "onboarding");
    }

    [Fact]
    public async Task GetTemplatesAsync_WithActiveFilter_ReturnsFilteredTemplates()
    {
        // Arrange
        var activeTemplate = NotificationTemplate.Create("active", "Active", NotificationType.System, NotificationChannel.InApp, "T", "M");
        var inactiveTemplate = NotificationTemplate.Create("inactive", "Inactive", NotificationType.System, NotificationChannel.InApp, "T", "M");
        inactiveTemplate.Deactivate();
        SetupTemplateDbSet([activeTemplate, inactiveTemplate]);

        // Act
        var result = await _sut.GetTemplatesAsync(isActive: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().Code.Should().Be("active");
    }

    [Fact]
    public async Task GetTemplatesAsync_WithBothFilters_ReturnsCorrectTemplates()
    {
        // Arrange
        var templates = new List<NotificationTemplate>
        {
            NotificationTemplate.Create("code1", "A", NotificationType.System, NotificationChannel.InApp, "T", "M", category: "cat1"),
            NotificationTemplate.Create("code2", "B", NotificationType.System, NotificationChannel.InApp, "T", "M", category: "cat1"),
            NotificationTemplate.Create("code3", "C", NotificationType.System, NotificationChannel.InApp, "T", "M", category: "cat2")
        };
        templates[1].Deactivate(); // code2 is inactive
        SetupTemplateDbSet(templates);

        // Act
        var result = await _sut.GetTemplatesAsync(category: "cat1", isActive: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().Code.Should().Be("code1");
    }

    [Fact]
    public async Task GetTemplatesAsync_ReturnsOrderedByName()
    {
        // Arrange
        var templates = new List<NotificationTemplate>
        {
            NotificationTemplate.Create("code1", "Zebra", NotificationType.System, NotificationChannel.InApp, "T", "M"),
            NotificationTemplate.Create("code2", "Apple", NotificationType.System, NotificationChannel.InApp, "T", "M"),
            NotificationTemplate.Create("code3", "Mango", NotificationType.System, NotificationChannel.InApp, "T", "M")
        };
        SetupTemplateDbSet(templates);

        // Act
        var result = await _sut.GetTemplatesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        var names = result.Value.Select(t => t.Name).ToList();
        names.Should().BeInAscendingOrder();
    }

    #endregion

    #region CreateTemplateAsync

    [Fact]
    public async Task CreateTemplateAsync_WithValidData_CreatesTemplate()
    {
        // Arrange
        SetupTemplateDbSet([]);

        // Act
        var result = await _sut.CreateTemplateAsync(
            "new_template", "New Template", NotificationType.Onboarding,
            NotificationChannel.Email, "Welcome {{name}}!", "Hello {{name}}!",
            description: "Onboarding email", actionUrlTemplate: "/welcome",
            category: "onboarding");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("new_template");
        result.Value.Name.Should().Be("New Template");
        result.Value.Type.Should().Be(NotificationType.Onboarding);
        result.Value.Channel.Should().Be(NotificationChannel.Email);
        result.Value.Category.Should().Be("onboarding");
        result.Value.IsActive.Should().BeTrue();
        _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTemplateAsync_WhenCodeExists_ReturnsConflict()
    {
        // Arrange
        var existingTemplate = NotificationTemplate.Create(
            "existing_code", "Existing", NotificationType.System,
            NotificationChannel.InApp, "T", "M");
        SetupTemplateDbSet([existingTemplate]);

        // Act
        var result = await _sut.CreateTemplateAsync(
            "existing_code", "New Name", NotificationType.System,
            NotificationChannel.InApp, "T", "M");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Template.DuplicateCode");
    }

    [Fact]
    public async Task CreateTemplateAsync_WithMinimalParams_UsesDefaults()
    {
        // Arrange
        SetupTemplateDbSet([]);

        // Act
        var result = await _sut.CreateTemplateAsync(
            "minimal", "Minimal", NotificationType.System,
            NotificationChannel.InApp, "Title", "Message");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DefaultPriority.Should().Be(NotificationPriority.Normal);
        result.Value.Description.Should().BeNull();
        result.Value.ActionUrlTemplate.Should().BeNull();
        result.Value.Category.Should().BeNull();
    }

    #endregion

    #region UpdateTemplateAsync

    [Fact]
    public async Task UpdateTemplateAsync_WithNewContent_UpdatesTemplate()
    {
        // Arrange
        var template = NotificationTemplate.Create(
            "code", "Name", NotificationType.System,
            NotificationChannel.InApp, "Old Title", "Old Message");
        SetupTemplateDbSet([template]);

        // Act
        var result = await _sut.UpdateTemplateAsync(
            template.Id,
            titleTemplate: "New Title",
            messageTemplate: "New Message",
            actionUrlTemplate: "/new-action");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TitleTemplate.Should().Be("New Title");
        result.Value.MessageTemplate.Should().Be("New Message");
        result.Value.ActionUrlTemplate.Should().Be("/new-action");
    }

    [Fact]
    public async Task UpdateTemplateAsync_WhenTemplateNotFound_ReturnsFailure()
    {
        // Arrange
        SetupTemplateDbSet([]);

        // Act
        var result = await _sut.UpdateTemplateAsync(Guid.NewGuid(), titleTemplate: "New Title");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Template.NotFound");
    }

    [Fact]
    public async Task UpdateTemplateAsync_WithIsActiveTrue_ActivatesTemplate()
    {
        // Arrange
        var template = NotificationTemplate.Create(
            "code", "Name", NotificationType.System,
            NotificationChannel.InApp, "Title", "Message");
        template.Deactivate();
        SetupTemplateDbSet([template]);

        // Act
        var result = await _sut.UpdateTemplateAsync(template.Id, isActive: true);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTemplateAsync_WithIsActiveFalse_DeactivatesTemplate()
    {
        // Arrange
        var template = NotificationTemplate.Create(
            "code", "Name", NotificationType.System,
            NotificationChannel.InApp, "Title", "Message");
        SetupTemplateDbSet([template]);

        // Act
        var result = await _sut.UpdateTemplateAsync(template.Id, isActive: false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateTemplateAsync_WithOnlyIsActive_DoesNotChangeContent()
    {
        // Arrange
        var template = NotificationTemplate.Create(
            "code", "Name", NotificationType.System,
            NotificationChannel.InApp, "Original Title", "Original Message");
        SetupTemplateDbSet([template]);

        // Act
        var result = await _sut.UpdateTemplateAsync(template.Id, isActive: false);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TitleTemplate.Should().Be("Original Title");
        result.Value.MessageTemplate.Should().Be("Original Message");
    }

    [Fact]
    public async Task UpdateTemplateAsync_PartialContentUpdate_PreservesUnchangedFields()
    {
        // Arrange
        var template = NotificationTemplate.Create(
            "code", "Name", NotificationType.System,
            NotificationChannel.InApp, "Original Title", "Original Message", 
            actionUrlTemplate: "/original-action");
        SetupTemplateDbSet([template]);

        // Act
        var result = await _sut.UpdateTemplateAsync(template.Id, titleTemplate: "New Title");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TitleTemplate.Should().Be("New Title");
        result.Value.MessageTemplate.Should().Be("Original Message");
        result.Value.ActionUrlTemplate.Should().Be("/original-action");
    }

    #endregion

    #region ReplacePlaceholders

    [Fact]
    public void ReplacePlaceholders_WithValidPlaceholders_ReplacesAll()
    {
        // Arrange
        var template = "Hello {{name}}, your course {{course}} starts on {{date}}.";
        var placeholders = new Dictionary<string, string>
        {
            { "name", "Alice" },
            { "course", "Game Design 101" },
            { "date", "2025-01-15" }
        };

        // Act
        var result = _sut.ReplacePlaceholders(template, placeholders);

        // Assert
        result.Should().Be("Hello Alice, your course Game Design 101 starts on 2025-01-15.");
    }

    [Fact]
    public void ReplacePlaceholders_WithEmptyPlaceholders_ReturnsOriginal()
    {
        // Arrange
        var template = "No placeholders here.";
        var placeholders = new Dictionary<string, string>();

        // Act
        var result = _sut.ReplacePlaceholders(template, placeholders);

        // Assert
        result.Should().Be("No placeholders here.");
    }

    [Fact]
    public void ReplacePlaceholders_WithUnusedPlaceholders_LeavesThemUnchanged()
    {
        // Arrange
        var template = "Hello {{name}}!";
        var placeholders = new Dictionary<string, string> { { "unused", "value" } };

        // Act
        var result = _sut.ReplacePlaceholders(template, placeholders);

        // Assert
        result.Should().Be("Hello {{name}}!");
    }

    [Fact]
    public void ReplacePlaceholders_WithMultipleOccurrences_ReplacesAll()
    {
        // Arrange
        var template = "{{greeting}} {{name}}! How are you, {{name}}?";
        var placeholders = new Dictionary<string, string>
        {
            { "greeting", "Hi" },
            { "name", "Bob" }
        };

        // Act
        var result = _sut.ReplacePlaceholders(template, placeholders);

        // Assert
        result.Should().Be("Hi Bob! How are you, Bob?");
    }

    [Fact]
    public void ReplacePlaceholders_WithSpecialCharactersInValues_HandlesCorrectly()
    {
        // Arrange
        var template = "Amount: {{amount}}";
        var placeholders = new Dictionary<string, string> { { "amount", "$1,000.00" } };

        // Act
        var result = _sut.ReplacePlaceholders(template, placeholders);

        // Assert
        result.Should().Be("Amount: $1,000.00");
    }

    [Fact]
    public void ReplacePlaceholders_WithEmptyString_ReturnsEmptyString()
    {
        // Arrange
        var template = "";
        var placeholders = new Dictionary<string, string> { { "name", "Alice" } };

        // Act
        var result = _sut.ReplacePlaceholders(template, placeholders);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion
}
