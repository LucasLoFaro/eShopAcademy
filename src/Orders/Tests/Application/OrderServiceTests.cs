using Application;
using AutoFixture.Xunit2;
using Core.Application.Interfaces;
using Domain.Orders.Contracts;
using Domain.Orders.Entities;
using Domain.Orders.Enums;
using FluentAssertions;
using Infrastructure.Data;
using Moq;
using Xunit;

namespace Orders.Tests.Application;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _repository = new();
    private readonly Mock<IStockServiceClient> _stockClient = new();
    private readonly Mock<IProductServiceClient> _productClient = new();
    private readonly Mock<IPaymentServiceClient> _paymentClient = new();
    private readonly Mock<ICustomerServiceClient> _customerClient = new();
    private readonly Mock<IOrderMessagingClient> _messagingClient = new();

    private OrderService CreateSut() => new(
        _repository.Object,
        _stockClient.Object,
        _productClient.Object,
        _paymentClient.Object,
        _customerClient.Object,
        _messagingClient.Object);

    [Theory]
    [AutoData]
    public async Task GetAllOrders_ReturnsList(List<Order> orders)
    {
        // Arrange
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(orders);

        // Act
        var result = await CreateSut().GetAllOrders();

        // Assert
        result.Should().BeEquivalentTo(orders);
    }

    [Theory]
    [AutoData]
    public async Task GetOrderById_WhenFound_ReturnsOrder(Order order)
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        // Act
        var result = await CreateSut().GetOrderById(order.Id);

        // Assert
        result.Should().BeEquivalentTo(order);
    }

    [Theory]
    [AutoData]
    public async Task GetOrderById_WhenNotFound_ReturnsNull(Guid id)
    {
        // Arrange
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        // Act
        var result = await CreateSut().GetOrderById(id);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [AutoData]
    public async Task RemoveOrder_DelegatesToRepository(Guid id)
    {
        // Arrange
        _repository.Setup(r => r.RemoveByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        await CreateSut().RemoveOrder(id);

        // Assert
        _repository.Verify(r => r.RemoveByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task PlaceOrderAsync_HappyPath_ReturnsResponseAndPublishesEvents(
        Guid customerId,
        Guid productId,
        Guid reservationId,
        Guid paymentId)
    {
        // Arrange
        var price = 10f;
        var request = new OrderRequest
        {
            CustomerId = customerId,
            Items = [new Item { ProductID = productId, Quantity = 2, Price = price }],
            ShippingAddress = new OrderAddressInfo { Street = "Main St", City = "Springfield" }
        };

        var customer = new OrderCustomerInfo { Name = "Jane Doe", Email = "jane@example.com" };
        var product = new Product { ID = productId, Name = "Widget", Price = price };
        var paymentResponse = new InitPaymentResponse
        {
            Id = paymentId,
            Amount = (double)(2 * price),
            ProviderTransactionId = "prov-123",
            PaymentUrl = "http://pay.example.com/checkout"
        };
        var stockResponse = new ReserveStockResponse { ReservationId = reservationId };

        _customerClient.Setup(c => c.GetCustomerByIdAsync(customerId)).ReturnsAsync(customer);
        _productClient.Setup(p => p.GetProductByIdAsync(productId)).ReturnsAsync(product);
        _paymentClient.Setup(p => p.InitPaymentAsync(It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paymentResponse);
        _stockClient.Setup(s => s.ReserveStockAsync(It.IsAny<Guid>(), It.IsAny<List<Item>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stockResponse);
        _repository.Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _messagingClient.Setup(m => m.PublishOrderSubmitted(It.IsAny<Order>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _messagingClient.Setup(m => m.PublishCustomerAddressUpdated(It.IsAny<Guid>(), It.IsAny<OrderAddressInfo>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        var result = await CreateSut().PlaceOrderAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(OrderStatus.Created);
        result.PaymentUrl.Should().Be(new Uri(paymentResponse.PaymentUrl));

        _repository.Verify(r => r.AddAsync(It.Is<Order>(o => o.CustomerId == customerId), It.IsAny<CancellationToken>()), Times.Once);
        _messagingClient.Verify(m => m.PublishOrderSubmitted(It.IsAny<Order>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        _messagingClient.Verify(m => m.PublishCustomerAddressUpdated(customerId, It.IsAny<OrderAddressInfo>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task PlaceOrderAsync_WhenCustomerNotFound_Throws(OrderRequest request)
    {
        // Arrange
        _customerClient.Setup(c => c.GetCustomerByIdAsync(request.CustomerId)).ReturnsAsync((OrderCustomerInfo?)null!);

        // Act
        var act = () => CreateSut().PlaceOrderAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Customer not found*");
    }

    [Theory]
    [AutoData]
    public async Task PlaceOrderAsync_WhenPriceChanged_Throws(Guid customerId, Guid productId)
    {
        // Arrange
        var request = new OrderRequest
        {
            CustomerId = customerId,
            Items = [new Item { ProductID = productId, Quantity = 1, Price = 10f }]
        };
        var customer = new OrderCustomerInfo { Name = "John", Email = "john@example.com" };
        // Product has a different price than what was in the request
        var product = new Product { ID = productId, Name = "Widget", Price = 99f };

        _customerClient.Setup(c => c.GetCustomerByIdAsync(customerId)).ReturnsAsync(customer);
        _productClient.Setup(p => p.GetProductByIdAsync(productId)).ReturnsAsync(product);

        // Act
        var act = () => CreateSut().PlaceOrderAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Price changed*");
    }

    [Theory]
    [AutoData]
    public async Task PlaceOrderAsync_WhenPaymentFails_Throws(Guid customerId, Guid productId)
    {
        // Arrange
        var price = 10f;
        var request = new OrderRequest
        {
            CustomerId = customerId,
            Items = [new Item { ProductID = productId, Quantity = 1, Price = price }]
        };
        var customer = new OrderCustomerInfo { Name = "John", Email = "john@example.com" };
        var product = new Product { ID = productId, Name = "Widget", Price = price };

        _customerClient.Setup(c => c.GetCustomerByIdAsync(customerId)).ReturnsAsync(customer);
        _productClient.Setup(p => p.GetProductByIdAsync(productId)).ReturnsAsync(product);
        _paymentClient.Setup(p => p.InitPaymentAsync(It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InitPaymentResponse?)null!);

        // Act
        var act = () => CreateSut().PlaceOrderAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*error creating the payment*");
    }

    [Theory]
    [AutoData]
    public async Task PlaceOrderAsync_WhenStockOutOfStock_Throws(Guid customerId, Guid productId)
    {
        // Arrange
        var price = 10f;
        var request = new OrderRequest
        {
            CustomerId = customerId,
            Items = [new Item { ProductID = productId, Quantity = 1, Price = price }]
        };
        var customer = new OrderCustomerInfo { Name = "John", Email = "john@example.com" };
        var product = new Product { ID = productId, Name = "Widget", Price = price };
        var paymentResponse = new InitPaymentResponse
        {
            Id = Guid.NewGuid(),
            Amount = price,
            ProviderTransactionId = "prov-456",
            PaymentUrl = "http://pay.example.com/checkout"
        };
        // Stock response with out-of-stock products
        var stockResponse = new ReserveStockResponse { OutOfStockProducts = ["Widget"] };

        _customerClient.Setup(c => c.GetCustomerByIdAsync(customerId)).ReturnsAsync(customer);
        _productClient.Setup(p => p.GetProductByIdAsync(productId)).ReturnsAsync(product);
        _paymentClient.Setup(p => p.InitPaymentAsync(It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paymentResponse);
        _stockClient.Setup(s => s.ReserveStockAsync(It.IsAny<Guid>(), It.IsAny<List<Item>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stockResponse);

        // Act
        var act = () => CreateSut().PlaceOrderAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*run out of stock*");
    }
}
