using Domain.Common.Events.Sellers;
using Domain.Notifications.Entities;
using Domain.Notifications.Enums;
using MassTransit;
using NotificationService.Data;

namespace NotificationService;

public class SellerNotificationConsumer : IConsumer<SellerVerificationCompletedEvent>
{
    private readonly NotificationDbContext _dbContext;
    private readonly ILogger<SellerNotificationConsumer> _logger;

    public SellerNotificationConsumer(
        NotificationDbContext dbContext,
        ILogger<SellerNotificationConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SellerVerificationCompletedEvent> context)
    {
        var message = context.Message;

        var (subject, body) = message.Approved
            ? ("Seller Account Approved", $"Congratulations {message.Name}! Your seller account has been verified and is now active. You can start listing products on eShop Academy.")
            : ("Seller Account Rejected", $"Hello {message.Name}, unfortunately your seller verification was not approved. Reason: {message.Reason ?? "Verification failed."}");

        var notification = new NotificationMessage
        {
            Recipient = new NotificationRecipient
            {
                Name = message.Name,
                Email = message.Email
            },
            Channel = NotificationChannel.InApp,
            Subject = subject,
            Body = body,
            Status = NotificationStatus.Sent,
            SentAt = DateTime.UtcNow,
            Type = message.Approved ? "SellerApproved" : "SellerRejected",
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };

        await _dbContext.Notifications.InsertOneAsync(notification, cancellationToken: context.CancellationToken);

        _logger.LogInformation(
            "Persisted seller verification notification; approved: {Approved}",
            message.Approved);
    }
}
