using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Commerce.Products.UnitTests.Commands;

public sealed class ProductStatusCommandHandlerTests
{
    private readonly Mock<IProductRepository> _repository = new();

    [Fact]
    public async Task ActivateProduct_SetsPublishedTrue_AndPersistsProduct()
    {
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId);
        product.IsPublished = false;

        _repository
            .Setup(repository => repository.GetByIdAsync(productId, It.IsAny<CancellationToken>(), false, false, null))
            .ReturnsAsync(product);

        var handler = new ActivateProductHandler(_repository.Object);

        var result = await handler.Handle(new ActivateProductCommand(productId), CancellationToken.None);

        product.IsPublished.Should().BeTrue();
        result.IsPublished.Should().BeTrue();
        _repository.Verify(repository => repository.UpdateAsync(product, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateProduct_SetsPublishedFalse_AndPersistsProduct()
    {
        var productId = Guid.NewGuid();
        var product = CreateProduct(productId);
        product.IsPublished = true;

        _repository
            .Setup(repository => repository.GetByIdAsync(productId, It.IsAny<CancellationToken>(), false, false, null))
            .ReturnsAsync(product);

        var handler = new DeactivateProductHandler(_repository.Object);

        var result = await handler.Handle(new DeactivateProductCommand(productId), CancellationToken.None);

        product.IsPublished.Should().BeFalse();
        result.IsPublished.Should().BeFalse();
        _repository.Verify(repository => repository.UpdateAsync(product, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActivateProduct_WhenProductDoesNotExist_ThrowsProductNotFoundException()
    {
        var productId = Guid.NewGuid();
        _repository
            .Setup(repository => repository.GetByIdAsync(productId, It.IsAny<CancellationToken>(), false, false, null))
            .ReturnsAsync((Product?)null);

        var handler = new ActivateProductHandler(_repository.Object);

        var act = async () => await handler.Handle(new ActivateProductCommand(productId), CancellationToken.None);

        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    [Fact]
    public async Task DeactivateProduct_WhenProductDoesNotExist_ThrowsProductNotFoundException()
    {
        var productId = Guid.NewGuid();
        _repository
            .Setup(repository => repository.GetByIdAsync(productId, It.IsAny<CancellationToken>(), false, false, null))
            .ReturnsAsync((Product?)null);

        var handler = new DeactivateProductHandler(_repository.Object);

        var act = async () => await handler.Handle(new DeactivateProductCommand(productId), CancellationToken.None);

        await act.Should().ThrowAsync<ProductNotFoundException>();
    }

    private static Product CreateProduct(Guid id)
        => new()
        {
            Id = id,
            Name = "Module product",
            Type = ProductType.Program
        };
}
