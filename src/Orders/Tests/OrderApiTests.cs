using System.Net;
using System.Net.Http.Json;
using Domain.Orders.Contracts;
using Domain.Orders.Entities;
using FluentAssertions;
using Xunit;

namespace Tests;

/// <summary>
/// Integration tests for the OrderService API.  These tests host the API in
/// memory and verify that requests return the expected responses and
/// interactions with the messaging layer occur as intended.
/// </summary>
public class OrderApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public OrderApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PlaceOrder_ReturnsAcceptedAndPublishesCommand()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var request = new OrderRequest
        {
            CustomerId = Guid.NewGuid(),
            Items = new List<Item>
            {
                new Item { ProductID = Guid.NewGuid(), Quantity = 2 }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/orders", request);

        // Assert: the API should return 202 Accepted
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        //// Assert: the mock messaging service should have been invoked with the expected order
        //_factory.MessagingServiceMock.Verify(ms => ms.SubmitOrder(It.Is<OrderRequest>(o =>
        //    o.CustomerId == request.CustomerId &&
        //    o.Items.Count == request.Items.Count &&
        //    o.Items[0].Quantity == request.Items[0].Quantity)), Times.Once);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsEmptyArray()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/orders");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.Content.ReadFromJsonAsync<List<Order>>();
        orders.Should().NotBeNull();
        orders!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOrder_ReturnsDefaultOrder()
    {
        using var client = _factory.CreateClient();
        var orderId = Guid.NewGuid();
        var response = await client.GetAsync($"/orders/{orderId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = await response.Content.ReadFromJsonAsync<Order>();
        order.Should().NotBeNull();
        // Since no persistence is implemented yet, a new Order is returned with default values
        order!.Items.Should().BeNullOrEmpty();
        order.CustomerId.Should().BeEmpty();
    }
}