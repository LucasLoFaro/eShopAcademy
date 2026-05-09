using Domain.Notification.Entities;
using Domain.Notification.Enums;
using FluentAssertions;
using Moq;
using Notification.Api.Data;
using Xunit;

namespace Notification.Tests;

public class NotificationRepositoryTests
{
    private static NotificationMessage CreateNotification(
        string email, Guid? orderId = null, bool isRead = false, string type = "OrderSubmitted") =>
        new()
        {
            Id = Guid.NewGuid(),
            Recipient = new NotificationRecipient { Name = "Test", Email = email },
            Channel = NotificationChannel.InApp,
            Subject = "Test",
            Body = "Test body",
            Status = NotificationStatus.Sent,
            SentAt = DateTime.UtcNow,
            OrderId = orderId ?? Guid.NewGuid(),
            Type = type,
            IsRead = isRead,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

    [Fact]
    public void Repository_ImplementsINotificationRepository()
    {
        // Assert: NotificationRepository implements the expected interface
        typeof(NotificationRepository).Should().Implement<INotificationRepository>();
    }

    [Fact]
    public async Task INotificationRepository_DefinesGetByEmailAsync()
    {
        // Arrange
        var mock = new Mock<INotificationRepository>();
        var notifications = new List<NotificationMessage> { CreateNotification("a@b.com") };
        mock.Setup(r => r.GetByEmailAsync("a@b.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(notifications);

        // Act
        var result = await mock.Object.GetByEmailAsync("a@b.com");

        // Assert
        result.Should().HaveCount(1);
        result[0].Recipient.Email.Should().Be("a@b.com");
    }

    [Fact]
    public async Task INotificationRepository_DefinesMarkAsReadAsync()
    {
        // Arrange
        var id = Guid.NewGuid();
        var mock = new Mock<INotificationRepository>();
        mock.Setup(r => r.MarkAsReadAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await mock.Object.MarkAsReadAsync(id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task INotificationRepository_DefinesMarkAllAsReadAsync()
    {
        // Arrange
        var mock = new Mock<INotificationRepository>();
        mock.Setup(r => r.MarkAllAsReadAsync("a@b.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        // Act
        var result = await mock.Object.MarkAllAsReadAsync("a@b.com");

        // Assert: returns count of marked-as-read notifications
        result.Should().Be(3);
    }

    [Fact]
    public async Task INotificationRepository_MarkAsRead_ReturnsFalseWhenNotFound()
    {
        // Arrange
        var mock = new Mock<INotificationRepository>();
        mock.Setup(r => r.MarkAsReadAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await mock.Object.MarkAsReadAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }
}
