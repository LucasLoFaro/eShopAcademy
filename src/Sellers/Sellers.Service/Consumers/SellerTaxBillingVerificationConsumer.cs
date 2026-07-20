using Domain.Common.Events.Sellers;
using Domain.Sellers.Enums;
using MassTransit;
using Microsoft.Extensions.Logging;
using Sellers.Application.Repositories;

namespace Sellers.Service.Consumers;

public class SellerTaxBillingVerificationConsumer : IConsumer<SellerTaxVerificationRequestedEvent>
{
    private readonly ISellerRepository _repository;
    private readonly ILogger<SellerTaxBillingVerificationConsumer> _logger;

    public SellerTaxBillingVerificationConsumer(
        ISellerRepository repository,
        ILogger<SellerTaxBillingVerificationConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SellerTaxVerificationRequestedEvent> context)
    {
        var message = context.Message;

        if (message.SellerId == Guid.Empty || string.IsNullOrWhiteSpace(message.TaxId))
        {
            throw new ArgumentException("Seller identifier and tax identifier are required.");
        }

        _logger.LogInformation(
            "[Sellers] Tax billing verification requested for seller {SellerId}",
            message.SellerId);

        var seller = await _repository.GetByIdAsync(message.SellerId, context.CancellationToken);
        if (seller is null)
        {
            _logger.LogWarning("[Sellers] Seller {SellerId} not found, skipping tax billing verification", message.SellerId);
            return;
        }

        if (seller.VerificationNotes?.StartsWith("Tax billing verification", StringComparison.Ordinal) == true)
        {
            _logger.LogInformation("[Sellers] Tax billing verification already completed for seller {SellerId}", message.SellerId);
            return;
        }

        // Simulate generating a small bill against the local tax authority
        var billingResult = await GenerateTaxAuthorityBillAsync(message, context.CancellationToken);

        if (billingResult.Success)
        {
            seller.Status = SellerStatus.Active;
            seller.VerificationNotes = $"Tax billing verification passed. Transaction ID: {billingResult.TransactionId}";
            _logger.LogInformation(
                "[Sellers] Seller {SellerId} tax billing verified successfully. Transaction: {TransactionId}",
                message.SellerId, billingResult.TransactionId);
        }
        else
        {
            seller.Status = SellerStatus.Rejected;
            seller.VerificationNotes = $"Tax billing verification failed: {billingResult.Reason}";
            _logger.LogWarning(
                "[Sellers] Seller {SellerId} tax billing verification failed: {Reason}",
                message.SellerId, billingResult.Reason);
        }

        await _repository.UpdateAsync(seller, context.CancellationToken);

        await context.Publish(new SellerVerificationCompletedEvent
        {
            SellerId = message.SellerId,
            Name = message.Name,
            Email = message.Email,
            Approved = billingResult.Success,
            Reason = billingResult.Success ? null : billingResult.Reason
        }, context.CancellationToken);
    }

    private Task<TaxBillingResult> GenerateTaxAuthorityBillAsync(
        SellerTaxVerificationRequestedEvent message,
        CancellationToken cancellationToken)
    {
        // TODO: Integrate with real tax authority billing API
        // For now, hardcoded to always succeed with a simulated transaction
        _logger.LogInformation(
            "[Sellers] Generating a seller tax verification bill (development stub)");

        var result = new TaxBillingResult
        {
            Success = true,
            TransactionId = $"TAX-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            Amount = 0.01m,
            Reason = null
        };

        return Task.FromResult(result);
    }

    private sealed class TaxBillingResult
    {
        public bool Success { get; init; }
        public string TransactionId { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public string? Reason { get; init; }
    }
}
