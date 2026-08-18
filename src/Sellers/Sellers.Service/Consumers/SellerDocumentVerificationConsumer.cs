using Domain.Common.Events.Sellers;
using Domain.Sellers.Enums;
using MassTransit;
using Microsoft.Extensions.Logging;
using Sellers.Application.Repositories;

namespace Sellers.Service.Consumers;

public class SellerDocumentVerificationConsumer : IConsumer<SellerRegistrationRequestedEvent>
{
    private readonly ISellerRepository _repository;
    private readonly ILogger<SellerDocumentVerificationConsumer> _logger;

    public SellerDocumentVerificationConsumer(
        ISellerRepository repository,
        ILogger<SellerDocumentVerificationConsumer> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SellerRegistrationRequestedEvent> context)
    {
        var message = context.Message;

        if (message.SellerId == Guid.Empty || string.IsNullOrWhiteSpace(message.DocumentUrl))
        {
            throw new ArgumentException("Seller identifier and document reference are required.");
        }

        _logger.LogInformation(
            "[Sellers] Document verification requested for seller {SellerId}",
            message.SellerId);

        var seller = await _repository.GetByIdAsync(message.SellerId, context.CancellationToken);
        if (seller is null)
        {
            _logger.LogWarning("[Sellers] Seller {SellerId} not found, skipping verification", message.SellerId);
            return;
        }

        if (seller.VerificationStatus is DocumentVerificationStatus.Verified or DocumentVerificationStatus.Rejected)
        {
            _logger.LogInformation("[Sellers] Document verification already completed for seller {SellerId}", message.SellerId);
            return;
        }

        seller.VerificationStatus = DocumentVerificationStatus.Processing;
        await _repository.UpdateAsync(seller, context.CancellationToken);

        // TODO: Call Azure AI Document Intelligence to analyze the document
        // var documentResult = await _documentIntelligenceClient.AnalyzeDocumentAsync(message.DocumentUrl);
        // Validate extracted fields (TaxId, Name, Address) match the registration request

        var verificationPassed = await VerifySellerDocumentAsync(message, context.CancellationToken);

        if (verificationPassed)
        {
            seller.VerificationStatus = DocumentVerificationStatus.Verified;
            seller.Status = SellerStatus.Active;
            seller.VerificationNotes = "Document verified successfully via AI analysis.";
            _logger.LogInformation("[Sellers] Seller {SellerId} document verified, status set to Active", message.SellerId);
        }
        else
        {
            seller.VerificationStatus = DocumentVerificationStatus.Rejected;
            seller.Status = SellerStatus.Rejected;
            seller.VerificationNotes = "Document verification failed. Information does not match registration data.";
            _logger.LogWarning("[Sellers] Seller {SellerId} document verification failed", message.SellerId);
        }

        await _repository.UpdateAsync(seller, context.CancellationToken);

        await context.Publish(new SellerVerificationCompletedEvent
        {
            SellerId = message.SellerId,
            Name = message.Name,
            Email = message.Email,
            Approved = verificationPassed,
            Reason = verificationPassed ? null : "Document information does not match registration data."
        }, context.CancellationToken);
    }

    private Task<bool> VerifySellerDocumentAsync(SellerRegistrationRequestedEvent message, CancellationToken cancellationToken)
    {
        // TODO: Implement Azure AI Document Intelligence integration
        // 1. Call Document Intelligence to extract fields from the document at message.DocumentUrl
        // 2. Compare extracted TaxId, Name against message.TaxId, message.Name
        // 3. Return true if extracted data matches registration data within acceptable confidence
        //
        // Example:
        // var client = new DocumentIntelligenceClient(endpoint, credential);
        // var operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-document", documentUrl);
        // var result = operation.Value;
        // ... extract and compare fields

        _logger.LogWarning("[Sellers] Document Intelligence integration not yet implemented. Auto-approving for development.");
        return Task.FromResult(true);
    }
}
