using FluentAssertions;
using GameGuild.Commerce.Products;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Products.UnitTests.Commands;

/// <summary>
/// Unit tests for DeleteProductCommandHandler
/// </summary>
public class DeleteProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _mockRepository;
    private readonly DeleteProductCommandHandler _handler;

    public DeleteProductCommandHandlerTests()
    {
        _mockRepository = new Mock<IProductRepository>();
        _handler = new DeleteProductCommandHandler(_mockRepository.Object);
    }

    private static Product CreateTestProduct(Guid? id = null)
    {
        return new Product
        {
            Id = id ?? Guid.NewGuid(),
            Name = "Test Product",
            Version = 1
        };
    }

    #region Soft Delete Tests

    [Fact]
    public async Task Handle_WithSoftDeleteTrue_PerformsSoftDelete()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct(productId);
        var command = new DeleteProductCommand(ProductId: productId, SoftDelete: true);

        _mockRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>(), false, false))
            .ReturnsAsync(existingProduct);
        _mockRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        existingProduct.IsDeleted.Should().BeTrue();
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithSoftDelete_SetsDeletedAt()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct(productId);
        var command = new DeleteProductCommand(ProductId: productId, SoftDelete: true);
        var before = DateTime.UtcNow;

        _mockRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>(), false, false))
            .ReturnsAsync(existingProduct);
        _mockRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);
        var after = DateTime.UtcNow;

        // Assert
        existingProduct.DeletedAt.Should().NotBeNull();
        existingProduct.DeletedAt.Should().BeAfter(before.AddSeconds(-1));
        existingProduct.DeletedAt.Should().BeBefore(after.AddSeconds(1));
    }

    #endregion

    #region Hard Delete Tests

    [Fact]
    public async Task Handle_WithSoftDeleteFalse_PerformsHardDelete()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct(productId);
        var command = new DeleteProductCommand(ProductId: productId, SoftDelete: false);

        _mockRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>(), false, false))
            .ReturnsAsync(existingProduct);
        _mockRepository.Setup(r => r.DeleteAsync(existingProduct, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        _mockRepository.Verify(r => r.DeleteAsync(existingProduct, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDefaultSoftDelete_PerformsSoftDelete()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct(productId);
        var command = new DeleteProductCommand(ProductId: productId);

        _mockRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>(), false, false))
            .ReturnsAsync(existingProduct);
        _mockRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task Handle_WithNullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        DeleteProductCommand command = null!;

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_WithNonExistentProduct_ThrowsProductNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var command = new DeleteProductCommand(ProductId: productId);

        _mockRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>(), false, false))
            .ReturnsAsync((Product?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [Fact]
    public async Task Handle_WithVersionMismatch_ThrowsConcurrencyException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct(productId);
        existingProduct.Version = 3;
        
        var command = new DeleteProductCommand(
            ProductId: productId,
            ExpectedVersion: 1);

        _mockRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>(), false, false))
            .ReturnsAsync(existingProduct);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConcurrencyException>();
    }

    [Fact]
    public async Task Handle_WithCorrectVersion_Succeeds()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct(productId);
        existingProduct.Version = 5;
        
        var command = new DeleteProductCommand(
            ProductId: productId,
            ExpectedVersion: 5);

        _mockRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>(), false, false))
            .ReturnsAsync(existingProduct);
        _mockRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
    }

    #endregion

    #region Repository Interaction Tests

    [Fact]
    public async Task Handle_CallsGetByIdAsync()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct(productId);
        var command = new DeleteProductCommand(ProductId: productId);

        _mockRepository.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>(), false, false))
            .ReturnsAsync(existingProduct);
        _mockRepository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>(), false, false), Times.Once);
    }

    [Fact]
    public async Task Handle_PassesCancellationToken()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingProduct = CreateTestProduct(productId);
        var command = new DeleteProductCommand(ProductId: productId, SoftDelete: false);
        var cts = new CancellationTokenSource();

        _mockRepository.Setup(r => r.GetByIdAsync(productId, cts.Token, false, false))
            .ReturnsAsync(existingProduct);
        _mockRepository.Setup(r => r.DeleteAsync(existingProduct, cts.Token))
            .Returns(Task.CompletedTask);
        _mockRepository.Setup(r => r.SaveChangesAsync(cts.Token))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, cts.Token);

        // Assert
        _mockRepository.Verify(r => r.GetByIdAsync(productId, cts.Token, false, false), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(existingProduct, cts.Token), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(cts.Token), Times.Once);
    }

    #endregion
}

/// <summary>
/// Unit tests for DeleteProductCommand record
/// </summary>
public class DeleteProductCommandTests
{
    [Fact]
    public void DeleteProductCommand_WithProductId_SetsProductIdCorrectly()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        var command = new DeleteProductCommand(ProductId: productId);

        // Assert
        command.ProductId.Should().Be(productId);
    }

    [Fact]
    public void DeleteProductCommand_WithDefaults_SoftDeleteIsTrue()
    {
        // Arrange & Act
        var command = new DeleteProductCommand(ProductId: Guid.NewGuid());

        // Assert
        command.SoftDelete.Should().BeTrue();
        command.ExpectedVersion.Should().BeNull();
    }

    [Fact]
    public void DeleteProductCommand_WithSoftDeleteFalse_SetsCorrectly()
    {
        // Arrange & Act
        var command = new DeleteProductCommand(
            ProductId: Guid.NewGuid(),
            SoftDelete: false);

        // Assert
        command.SoftDelete.Should().BeFalse();
    }

    [Fact]
    public void DeleteProductCommand_WithExpectedVersion_SetsCorrectly()
    {
        // Arrange & Act
        var command = new DeleteProductCommand(
            ProductId: Guid.NewGuid(),
            ExpectedVersion: 42);

        // Assert
        command.ExpectedVersion.Should().Be(42);
    }
}
