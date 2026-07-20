using Domain.Products.Entities;

namespace Core.Application.Interfaces.Services;

public interface IContentModerationService
{
    Task<ContentModerationResult> ModerateProductAsync(Product product, CancellationToken cancellationToken = default);
}

public record ContentModerationResult(bool IsApproved, string? RejectionReason = null);
