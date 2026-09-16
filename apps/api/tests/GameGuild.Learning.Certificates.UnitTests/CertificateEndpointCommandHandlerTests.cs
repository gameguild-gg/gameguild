using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Learning.Certificates.Tests;

public class CertificateEndpointCommandHandlerTests
{
    private readonly Mock<ICertificateService> _certificateService = new();
    private readonly Mock<ICertificateTemplateService> _templateService = new();

    [Fact]
    public async Task IssueCertificate_DelegatesEveryActorAndResourceIdentifier()
    {
        var command = new IssueCertificateEndpointCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());
        _certificateService
            .Setup(service => service.IssueCertificateAsync(
                command.TemplateId,
                command.EnrollmentId,
                command.UserId,
                command.CourseId,
                command.TenantId))
            .ReturnsAsync(Result.Failure<Certificate>(Error.Failure("Certificate", "Expected failure")));

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _certificateService.VerifyAll();
    }

    [Fact]
    public async Task RevokeCertificate_DelegatesTheReason()
    {
        var command = new RevokeCertificateEndpointCommand(Guid.NewGuid(), "Duplicate credential");
        _certificateService
            .Setup(service => service.RevokeCertificateAsync(command.CertificateId, command.Reason))
            .ReturnsAsync(Result.Success());

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _certificateService.VerifyAll();
    }

    [Fact]
    public async Task CreateTemplate_BuildsAndPersistsTheRequestedTemplate()
    {
        var command = new CreateCertificateTemplateEndpointCommand(
            Guid.NewGuid(),
            "Course completion",
            "<html>Certificate</html>",
            Guid.NewGuid());
        CertificateTemplate? capturedTemplate = null;
        _templateService
            .Setup(service => service.CreateTemplateAsync(It.IsAny<CertificateTemplate>(), command.TenantId))
            .Callback<CertificateTemplate, Guid?>((template, _) => capturedTemplate = template)
            .ReturnsAsync((CertificateTemplate template, Guid? _) => Result.Success(template));

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedTemplate.Should().NotBeNull();
        capturedTemplate!.CourseId.Should().Be(command.CourseId);
        capturedTemplate.Name.Should().Be(command.Name);
        capturedTemplate.TemplateHtml.Should().Be(command.TemplateHtml);
        capturedTemplate.TenantId.Should().Be(command.TenantId);
        _templateService.VerifyAll();
    }

    [Fact]
    public async Task UpdateTemplate_WhenTemplateDoesNotExist_ReturnsNotFound()
    {
        var command = CreateUpdateCommand(Guid.NewGuid(), isDefault: false);
        _templateService
            .Setup(service => service.GetTemplateByIdAsync(command.TemplateId))
            .ReturnsAsync((CertificateTemplate?)null);

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        _templateService.Verify(
            service => service.UpdateTemplateAsync(It.IsAny<CertificateTemplate>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateTemplate_WhenMadeDefault_UsesTheCourseDefaultOperation()
    {
        var template = CertificateTemplate.Create(Guid.NewGuid(), "Old", "<html>Old</html>");
        var command = CreateUpdateCommand(template.Id, isDefault: true);
        _templateService
            .Setup(service => service.GetTemplateByIdAsync(template.Id))
            .ReturnsAsync(template);
        _templateService
            .Setup(service => service.SetDefaultTemplateAsync(template.CourseId, template.Id))
            .ReturnsAsync(Result.Success(template));

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        template.IsDefault.Should().BeTrue();
        template.Name.Should().Be(command.Name);
        _templateService.VerifyAll();
        _templateService.Verify(
            service => service.UpdateTemplateAsync(It.IsAny<CertificateTemplate>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateTemplate_WhenNotDefault_UsesTheStandardUpdateOperation()
    {
        var template = CertificateTemplate.Create(Guid.NewGuid(), "Old", "<html>Old</html>");
        template.SetDefault(true);
        var command = CreateUpdateCommand(template.Id, isDefault: false);
        _templateService
            .Setup(service => service.GetTemplateByIdAsync(template.Id))
            .ReturnsAsync(template);
        _templateService
            .Setup(service => service.UpdateTemplateAsync(template))
            .ReturnsAsync(Result.Success(template));

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        template.IsDefault.Should().BeFalse();
        template.Name.Should().Be(command.Name);
        _templateService.VerifyAll();
        _templateService.Verify(
            service => service.SetDefaultTemplateAsync(It.IsAny<Guid>(), It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteTemplate_DelegatesTheTemplateIdentifier()
    {
        var command = new DeleteCertificateTemplateEndpointCommand(Guid.NewGuid());
        _templateService
            .Setup(service => service.DeleteTemplateAsync(command.TemplateId))
            .ReturnsAsync(Result.Success());

        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _templateService.VerifyAll();
    }

    private CertificateEndpointCommandHandler CreateHandler() =>
        new(_certificateService.Object, _templateService.Object);

    private static UpdateCertificateTemplateEndpointCommand CreateUpdateCommand(
        Guid templateId,
        bool isDefault) =>
        new(
            templateId,
            "Updated certificate",
            "Updated description",
            "<html>Updated</html>",
            ".certificate { color: blue; }",
            isDefault,
            true);
}
