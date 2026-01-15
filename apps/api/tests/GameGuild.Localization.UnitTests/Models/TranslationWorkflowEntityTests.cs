using FluentAssertions;
using Xunit;

namespace GameGuild.Tests.Localization.Unit.Models;

/// <summary>
/// Tests for TranslationWorkflowEntity and TranslationTaskEntity
/// </summary>
public class TranslationWorkflowEntityTests
{
    [Fact]
    public void TranslationWorkflowEntity_TargetLanguagesJson_SerializesCorrectly()
    {
        // Arrange
        var entity = new GameGuild.Localization.TranslationWorkflowEntity
        {
            Id = Guid.NewGuid(),
            ResourceKey = "Course.123.Title",
            SourceLanguage = "en-US",
            SourceText = "Test Course"
        };

        // Act
        entity.TargetLanguages = ["es-ES", "fr-FR", "de-DE"];

        // Assert
        entity.TargetLanguagesJson.Should().Contain("es-ES");
        entity.TargetLanguagesJson.Should().Contain("fr-FR");
        entity.TargetLanguagesJson.Should().Contain("de-DE");
    }

    [Fact]
    public void TranslationWorkflowEntity_TargetLanguagesJson_DeserializesCorrectly()
    {
        // Arrange
        var entity = new GameGuild.Localization.TranslationWorkflowEntity
        {
            Id = Guid.NewGuid(),
            ResourceKey = "Course.123.Title",
            SourceLanguage = "en-US",
            SourceText = "Test Course",
            TargetLanguagesJson = "[\"es-ES\",\"fr-FR\"]"
        };

        // Act
        var languages = entity.TargetLanguages;

        // Assert
        languages.Should().HaveCount(2);
        languages.Should().Contain("es-ES");
        languages.Should().Contain("fr-FR");
    }

    [Fact]
    public void TranslationWorkflowEntity_DefaultStatus_IsPendingAssignment()
    {
        // Arrange & Act
        var entity = new GameGuild.Localization.TranslationWorkflowEntity
        {
            Id = Guid.NewGuid(),
            ResourceKey = "Test",
            SourceLanguage = "en-US",
            SourceText = "Test"
        };

        // Assert
        entity.Status.Should().Be(GameGuild.Localization.TranslationWorkflowStatus.PendingAssignment);
    }

    [Fact]
    public void TranslationWorkflowEntity_DefaultPriority_IsNormal()
    {
        // Arrange & Act
        var entity = new GameGuild.Localization.TranslationWorkflowEntity
        {
            Id = Guid.NewGuid(),
            ResourceKey = "Test",
            SourceLanguage = "en-US",
            SourceText = "Test"
        };

        // Assert
        entity.Priority.Should().Be(GameGuild.Localization.TranslationPriority.Normal);
    }

    [Fact]
    public void TranslationWorkflowEntity_HasEmptyTasksCollection()
    {
        // Arrange & Act
        var entity = new GameGuild.Localization.TranslationWorkflowEntity
        {
            Id = Guid.NewGuid(),
            ResourceKey = "Test",
            SourceLanguage = "en-US",
            SourceText = "Test"
        };

        // Assert
        entity.Tasks.Should().BeEmpty();
    }

    [Fact]
    public void TranslationTaskEntity_Metadata_SerializesCorrectly()
    {
        // Arrange
        var entity = new GameGuild.Localization.TranslationTaskEntity
        {
            Id = Guid.NewGuid(),
            WorkflowId = Guid.NewGuid(),
            TargetLanguage = "es-ES",
            TranslatorId = Guid.NewGuid(),
            AssignedAt = DateTime.UtcNow
        };

        // Act
        entity.Metadata = new Dictionary<string, string>
        {
            ["source"] = "manual",
            ["tool"] = "google-translate"
        };

        // Assert
        entity.MetadataJson.Should().Contain("source");
        entity.MetadataJson.Should().Contain("manual");
    }

    [Fact]
    public void TranslationTaskEntity_Metadata_DeserializesCorrectly()
    {
        // Arrange
        var entity = new GameGuild.Localization.TranslationTaskEntity
        {
            Id = Guid.NewGuid(),
            WorkflowId = Guid.NewGuid(),
            TargetLanguage = "es-ES",
            TranslatorId = Guid.NewGuid(),
            AssignedAt = DateTime.UtcNow,
            MetadataJson = "{\"source\":\"manual\",\"tool\":\"google\"}"
        };

        // Act
        var metadata = entity.Metadata;

        // Assert
        metadata.Should().NotBeNull();
        metadata.Should().ContainKey("source");
        metadata!["source"].Should().Be("manual");
        metadata["tool"].Should().Be("google");
    }

    [Fact]
    public void TranslationTaskEntity_Metadata_ReturnsNull_WhenEmpty()
    {
        // Arrange
        var entity = new GameGuild.Localization.TranslationTaskEntity
        {
            Id = Guid.NewGuid(),
            WorkflowId = Guid.NewGuid(),
            TargetLanguage = "es-ES",
            TranslatorId = Guid.NewGuid(),
            AssignedAt = DateTime.UtcNow,
            MetadataJson = null
        };

        // Act
        var metadata = entity.Metadata;

        // Assert
        metadata.Should().BeNull();
    }

    [Fact]
    public void TranslationTaskEntity_DefaultStatus_IsAssigned()
    {
        // Arrange & Act
        var entity = new GameGuild.Localization.TranslationTaskEntity
        {
            Id = Guid.NewGuid(),
            WorkflowId = Guid.NewGuid(),
            TargetLanguage = "es-ES",
            TranslatorId = Guid.NewGuid(),
            AssignedAt = DateTime.UtcNow
        };

        // Assert
        entity.Status.Should().Be(GameGuild.Localization.TranslationTaskStatus.Assigned);
    }

    [Fact]
    public void TranslationWorkflowStatus_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<GameGuild.Localization.TranslationWorkflowStatus>()
            .Should().Contain(GameGuild.Localization.TranslationWorkflowStatus.PendingAssignment);
        Enum.GetValues<GameGuild.Localization.TranslationWorkflowStatus>()
            .Should().Contain(GameGuild.Localization.TranslationWorkflowStatus.InProgress);
        Enum.GetValues<GameGuild.Localization.TranslationWorkflowStatus>()
            .Should().Contain(GameGuild.Localization.TranslationWorkflowStatus.Completed);
    }

    [Fact]
    public void TranslationTaskStatus_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<GameGuild.Localization.TranslationTaskStatus>()
            .Should().Contain(GameGuild.Localization.TranslationTaskStatus.Assigned);
        Enum.GetValues<GameGuild.Localization.TranslationTaskStatus>()
            .Should().Contain(GameGuild.Localization.TranslationTaskStatus.PendingReview);
        Enum.GetValues<GameGuild.Localization.TranslationTaskStatus>()
            .Should().Contain(GameGuild.Localization.TranslationTaskStatus.Approved);
    }

    [Fact]
    public void TranslationPriority_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<GameGuild.Localization.TranslationPriority>()
            .Should().Contain(GameGuild.Localization.TranslationPriority.Low);
        Enum.GetValues<GameGuild.Localization.TranslationPriority>()
            .Should().Contain(GameGuild.Localization.TranslationPriority.Normal);
        Enum.GetValues<GameGuild.Localization.TranslationPriority>()
            .Should().Contain(GameGuild.Localization.TranslationPriority.High);
        Enum.GetValues<GameGuild.Localization.TranslationPriority>()
            .Should().Contain(GameGuild.Localization.TranslationPriority.Critical);
    }
}
