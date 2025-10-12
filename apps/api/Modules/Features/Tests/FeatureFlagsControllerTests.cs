using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using GameGuild.Modules.Features.Controllers;
using GameGuild.Modules.Features.Models;
using GameGuild.Modules.Features.Services;

namespace GameGuild.Modules.Features.Tests;

public class FeatureFlagsControllerTests
{
    private readonly Mock<IFeatureFlagService> _mockFeatureFlagService;
    private readonly Mock<ILogger<FeatureFlagsController>> _mockLogger;
    private readonly FeatureFlagsController _controller;

    public FeatureFlagsControllerTests()
    {
        _mockFeatureFlagService = new Mock<IFeatureFlagService>();
        _mockLogger = new Mock<ILogger<FeatureFlagsController>>();
        _controller = new FeatureFlagsController(_mockFeatureFlagService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task BulkEvaluateFeatures_ReturnsOkResult_WithValidRequest()
    {
        // Arrange
        var request = new BulkEvaluationRequest
        {
            FeatureKeys = new List<string> { "feature1", "feature2" },
            UserId = Guid.NewGuid(),
            Environment = "test"
        };

        var mockResults = new List<FeatureEvaluationResult>
        {
            new() { FeatureKey = "feature1", Enabled = true, Reason = "Test" },
            new() { FeatureKey = "feature2", Enabled = false, Reason = "Test" }
        };

        _mockFeatureFlagService
            .Setup(x => x.EvaluateFeatureAsync(It.IsAny<string>(), It.IsAny<FeatureContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, FeatureContext context, CancellationToken ct) => 
                mockResults.First(r => r.FeatureKey == key));

        // Act
        var result = await _controller.BulkEvaluateFeatures(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<BulkEvaluationResponse>(okResult.Value);
        Assert.Equal(2, response.Results.Count);
        Assert.Equal("feature1", response.Results[0].FeatureKey);
        Assert.True(response.Results[0].Enabled);
    }
}
