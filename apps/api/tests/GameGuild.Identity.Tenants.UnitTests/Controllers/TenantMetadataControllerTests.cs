using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Controllers;

public class TenantMetadataControllerTests
{
    [Fact]
    public async Task Metadata_Endpoints_Should_Return_Expected_Results()
    {
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send<TenantMetadataDto?>(It.IsAny<GetTenantMetadataQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantMetadataDto?)null);
        sender.Setup(s => s.Send<Dictionary<string, object?>?>(It.IsAny<GetTenantCustomFieldsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, object?>());
        sender.Setup(s => s.Send<List<string>?>(It.IsAny<GetTenantTagsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());
        sender.Setup(s => s.Send<UpdateTenantMetadataCommand>(It.IsAny<UpdateTenantMetadataCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sender.Setup(s => s.Send<ReplaceTenantMetadataCommand>(It.IsAny<ReplaceTenantMetadataCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sender.Setup(s => s.Send<UpdateTenantCustomFieldsCommand>(It.IsAny<UpdateTenantCustomFieldsCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sender.Setup(s => s.Send<UpdateTenantTagsCommand>(It.IsAny<UpdateTenantTagsCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sender.Setup(s => s.Send<ReplaceTenantTagsCommand>(It.IsAny<ReplaceTenantTagsCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new TenantMetadataController(sender.Object);
        var tenantId = Guid.NewGuid();
        var customFields = new Dictionary<string, object?> { ["segment"] = "brokerage" };
        var replacementTags = new List<string> { "enterprise", "brokerage" };

        var updateRequest = new UpdateTenantMetadataRequest(
            CustomFields: null,
            Tags: null,
            ExternalReferences: null,
            BusinessInfo: null,
            ContactInfo: null,
            AdminNotes: null
        );

        var replaceRequest = new ReplaceTenantMetadataRequest(
            new Dictionary<string, object?>(),
            new List<string>(),
            new Dictionary<string, string>(),
            new UpdateTenantBusinessInfoRequest(null, null, null, null, null),
            new UpdateTenantContactInfoRequest(null, null, null, null, null, null),
            null
        );

        (await controller.GetMetadata(tenantId, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.UpdateMetadata(tenantId, updateRequest, CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.ReplaceMetadata(tenantId, replaceRequest, CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.GetCustomFields(tenantId, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.UpdateCustomFields(tenantId, customFields, CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.GetTags(tenantId, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.UpdateTags(tenantId, new UpdateTenantTagsRequest(new List<string>()), CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.ReplaceTags(tenantId, replacementTags, CancellationToken.None)).Should().BeOfType<NoContentResult>();

        sender.Verify(
            s => s.Send<UpdateTenantCustomFieldsCommand>(
                It.Is<UpdateTenantCustomFieldsCommand>(command => (string?)command.Request.CustomFields["segment"] == "brokerage"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        sender.Verify(
            s => s.Send<ReplaceTenantTagsCommand>(
                It.Is<ReplaceTenantTagsCommand>(command => command.Request.Tags.SequenceEqual(replacementTags)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
