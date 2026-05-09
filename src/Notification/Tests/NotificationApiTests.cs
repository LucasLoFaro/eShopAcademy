extern alias NotificationApi;

using Domain.Notification.Entities;
using Domain.Notification.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Notification.Api.Data;
using System.Net;
using System.Net.Http.Json;
using Xunit;

using ApiProgram = NotificationApi::Program;

namespace Notification.Tests;

public class NotificationApiTests : IClassFixture<WebApplicationFactory<ApiProgram>>
{
    private readonly Mock<INotificationRepository> _repo = new();

    private HttpClient CreateClient(WebApplicationFactory<ApiProgram> factory) =>
        factory.WithWebHostBuilder(builder =>
        {
            // Provide connection string so Program.cs doesn't get null
            builder.UseSetting("ConnectionStrings:notifications", "mongodb://localhost:27017");

            builder.ConfigureServices(services =>
            {
                // Replace the real repository with a mock
                services.AddScoped(_ => _repo.Object);
            });
        }).CreateClient();

    [Fact]
    public async Task GetNotifications_ReturnsOkWithNotifications()
    {
        // Arrange
        var notifications = new List<NotificationMessage>
        {
            CreateNotification("user@test.com", "Order Received", "OrderSubmitted")
        };
        _repo.Setup(r => r.GetByEmailAsync("user@test.com", It.IsAny<CancellationToken>()))
             .ReturnsAsync(notifications);

        using var factory = new WebApplicationFactory<ApiProgram>();
        var client = CreateClient(factory);

        // Act
        var response = await client.GetAsync("/notifications?email=user@test.com");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MarkAsRead_ExistingNotification_ReturnsNoContent()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repo.Setup(r => r.MarkAsReadAsync(id, It.IsAny<CancellationToken>()))
             .ReturnsAsync(true);

        using var factory = new WebApplicationFactory<ApiProgram>();
        var client = CreateClient(factory);

        // Act
        var response = await client.PatchAsync($"/notifications/{id}/read", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task MarkAsRead_NonExistentNotification_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repo.Setup(r => r.MarkAsReadAsync(id, It.IsAny<CancellationToken>()))
             .ReturnsAsync(false);

        using var factory = new WebApplicationFactory<ApiProgram>();
        var client = CreateClient(factory);

        // Act
        var response = await client.PatchAsync($"/notifications/{id}/read", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MarkAllAsRead_ReturnsOkWithCount()
    {
        // Arrange
        _repo.Setup(r => r.MarkAllAsReadAsync("user@test.com", It.IsAny<CancellationToken>()))
             .ReturnsAsync(5);

        using var factory = new WebApplicationFactory<ApiProgram>();
        var client = CreateClient(factory);

        // Act
        var response = await client.PostAsync("/notifications/read-all?email=user@test.com", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MarkAllReadResponse>();
        body!.MarkedRead.Should().Be(5);
    }

    private static NotificationMessage CreateNotification(string email, string subject, string type) =>
        new()
        {
            Id = Guid.NewGuid(),
            Recipient = new NotificationRecipient { Name = "Test", Email = email },
            Channel = NotificationChannel.InApp,
            Subject = subject,
            Body = "Test body",
            Status = NotificationStatus.Sent,
            SentAt = DateTime.UtcNow,
            OrderId = Guid.NewGuid(),
            Type = type,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

    private record MarkAllReadResponse(int MarkedRead);
}
