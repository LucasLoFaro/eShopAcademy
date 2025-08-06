using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using Core.Domain.Contracts;
using Core.Domain.Entities;
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

        // Assert: the stub messaging service should have received the request
        _factory.StubMessaging.LastRequest.Should().NotBeNull();
        _factory.StubMessaging.LastRequest!.CustomerId.Should().Be(request.CustomerId);
        _factory.StubMessaging.LastRequest.Items.Should().HaveCount(request.Items.Count);
        _factory.StubMessaging.LastRequest.Items[0].Quantity.Should().Be(request.Items[0].Quantity);
    }
}