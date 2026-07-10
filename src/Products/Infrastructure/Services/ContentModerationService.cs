using Azure.AI.ContentSafety;
using Azure.Identity;
using Core.Application.Interfaces.Services;
using Domain.Products.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class ContentModerationService : IContentModerationService
{
    private readonly ContentSafetyClient _client;
    private readonly ILogger<ContentModerationService> _logger;

    public ContentModerationService(IConfiguration configuration, ILogger<ContentModerationService> logger)
    {
        _logger = logger;
        var endpoint = configuration["ContentSafety:Endpoint"]
            ?? throw new InvalidOperationException("ContentSafety:Endpoint configuration is missing.");

        _client = new ContentSafetyClient(new Uri(endpoint), new DefaultAzureCredential());
    }

    public async Task<ContentModerationResult> ModerateProductAsync(Product product)
    {
        var textToAnalyze = $"{product.Name} {product.Description}";

        // Analyze text content
        var textResult = await AnalyzeTextAsync(textToAnalyze);
        if (!textResult.IsApproved)
            return textResult;

        // Analyze image content if URL is provided
        if (!string.IsNullOrWhiteSpace(product.ImageUrl))
        {
            var imageResult = await AnalyzeImageAsync(product.ImageUrl);
            if (!imageResult.IsApproved)
                return imageResult;
        }

        return new ContentModerationResult(true);
    }

    private async Task<ContentModerationResult> AnalyzeTextAsync(string text)
    {
        try
        {
            var options = new AnalyzeTextOptions(text);
            var response = await _client.AnalyzeTextAsync(options);

            foreach (var category in response.Value.CategoriesAnalysis)
            {
                if (category.Severity >= 2)
                {
                    _logger.LogWarning("Text moderation rejected: Category={Category}, Severity={Severity}",
                        category.Category, category.Severity);
                    return new ContentModerationResult(false,
                        $"Text content flagged for {category.Category} (severity: {category.Severity}).");
                }
            }

            return new ContentModerationResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Content Safety text analysis failed");
            throw;
        }
    }

    private async Task<ContentModerationResult> AnalyzeImageAsync(string imageUrl)
    {
        try
        {
            var imageData = new ContentSafetyImageData(new Uri(imageUrl));
            var options = new AnalyzeImageOptions(imageData);
            var response = await _client.AnalyzeImageAsync(options);

            foreach (var category in response.Value.CategoriesAnalysis)
            {
                if (category.Severity >= 2)
                {
                    _logger.LogWarning("Image moderation rejected: Category={Category}, Severity={Severity}",
                        category.Category, category.Severity);
                    return new ContentModerationResult(false,
                        $"Image content flagged for {category.Category} (severity: {category.Severity}).");
                }
            }

            return new ContentModerationResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Content Safety image analysis failed for URL: {ImageUrl}", imageUrl);
            throw;
        }
    }
}
