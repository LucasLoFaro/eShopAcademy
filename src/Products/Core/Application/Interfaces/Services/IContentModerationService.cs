using Domain.Products.Entities;

namespace Core.Application.Interfaces.Services;

public interface IContentModerationService
{
    Task<ContentModerationResult> ModerateProductAsync(Product product);
}

public record ContentModerationResult(bool IsApproved, string? RejectionReason = null);
