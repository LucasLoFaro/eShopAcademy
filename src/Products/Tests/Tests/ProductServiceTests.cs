using Core.Application.Interfaces.Data;
using Core.Application.Interfaces.Services;
using Core.Application.Services;
using Domain.Products.Entities;
using Moq;
using NUnit.Framework;

namespace Tests;

[TestFixture]
public class ProductServiceTests
{
    private Mock<IProductsRepository> _repository = null!;
    private Mock<IProductMessagingService> _messaging = null!;
    private ProductService _sut = null!;

    [SetUp]
    public void Setup()
    {
        _repository = new Mock<IProductsRepository>();
        _messaging = new Mock<IProductMessagingService>();
        _sut = new ProductService(_repository.Object, _messaging.Object);
    }

    [Test]
    public async Task GetAllAsync_DelegatesToRepository()
    {
        // Arrange
        var products = new List<Product> { new() { Name = "A" }, new() { Name = "B" } };
        _repository.Setup(r => r.GetAllAsync()).ReturnsAsync(products);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        Assert.That(result, Is.EquivalentTo(products));
    }

    [Test]
    public async Task GetByIdAsync_WhenFound_ReturnsProduct()
    {
        // Arrange
        var product = new Product { Name = "Widget" };
        _repository.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        // Act
        var result = await _sut.GetByIdAsync(product.Id);

        // Assert
        Assert.That(result, Is.EqualTo(product));
    }

    [Test]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Product?)null);

        // Act
        var result = await _sut.GetByIdAsync(id);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetMostExpensive_DelegatesToRepository()
    {
        // Arrange
        var product = new Product { Name = "Expensive", Price = 9999 };
        _repository.Setup(r => r.GetMostExpensive()).ReturnsAsync(product);

        // Act
        var result = await _sut.GetMostExpensive();

        // Assert
        Assert.That(result, Is.EqualTo(product));
    }

    [Test]
    public async Task AddOrUpdateAsync_SavesProductAndSendsUpdateEvent()
    {
        // Arrange
        var product = new Product { Name = "New Product", Price = 10 };
        _repository.Setup(r => r.AddOrUpdateAsync(product)).Returns(Task.CompletedTask);
        _messaging.Setup(m => m.SendProductUpdate(product, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _sut.AddOrUpdateAsync(product);

        // Assert: both repository and messaging are called
        _repository.Verify(r => r.AddOrUpdateAsync(product), Times.Once);
        _messaging.Verify(m => m.SendProductUpdate(product, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_DeletesProductAndSendsDeleteEvent()
    {
        // Arrange
        var product = new Product { Name = "Old Product" };
        _repository.Setup(r => r.DeleteAsync(product)).Returns(Task.CompletedTask);
        _messaging.Setup(m => m.SendProductDelete(product, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _sut.DeleteAsync(product);

        // Assert: both repository and messaging are called
        _repository.Verify(r => r.DeleteAsync(product), Times.Once);
        _messaging.Verify(m => m.SendProductDelete(product, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AddOrUpdateAsync_WhenMessagingFails_PropagatesException()
    {
        // Arrange
        var product = new Product { Name = "Product" };
        _repository.Setup(r => r.AddOrUpdateAsync(product)).Returns(Task.CompletedTask);
        _messaging.Setup(m => m.SendProductUpdate(product, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Messaging unavailable"));

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AddOrUpdateAsync(product));
    }
}
