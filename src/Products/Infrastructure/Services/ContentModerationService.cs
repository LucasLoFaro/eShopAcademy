using Azure.AI.ContentSafety;
using Azure.Identity;
using Core.Application.Interfaces.Services;
using Domain.Products.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class ContentModerationService : IContentModerationService
{
    private readonly ContentSafetyClient _client;
    private readonly ILogger<ContentModerationService> _logger;

    public ContentModerationService(IOptions<ContentSafetyOptions> options, ILogger<ContentModerationService> logger)
    {
        _logger = logger;
        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ExcludeManagedIdentityCredential = options.Value.ExcludeManagedIdentity
        });
        _client = new ContentSafetyClient(new Uri(options.Value.Endpoint), credential);
    }

    public async Task<ContentModerationResult> ModerateProductAsync(Product product, CancellationToken cancellationToken = default)
    {
        var textToAnalyze = $"{product.Name} {product.Description}";

        // Analyze text content
        var textResult = await AnalyzeTextAsync(textToAnalyze, cancellationToken);
        if (!textResult.IsApproved)
            return textResult;

        // Analyze image content if URL is provided
        if (!string.IsNullOrWhiteSpace(product.ImageUrl))
        {
            var imageResult = await AnalyzeImageAsync(product.ImageUrl, cancellationToken);
            if (!imageResult.IsApproved)
                return imageResult;
        }

        return new ContentModerationResult(true);
    }

    private async Task<ContentModerationResult> AnalyzeTextAsync(string text, CancellationToken cancellationToken)
    {
        try
        {
            var options = new AnalyzeTextOptions(text);
            var response = await _client.AnalyzeTextAsync(options, cancellationToken);

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

    private async Task<ContentModerationResult> AnalyzeImageAsync(string imageUrl, CancellationToken cancellationToken)
    {
        try
        {
            var imageData = new ContentSafetyImageData(new Uri(imageUrl));
            var options = new AnalyzeImageOptions(imageData);
            var response = await _client.AnalyzeImageAsync(options, cancellationToken);

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
            _logger.LogError(ex, "Content Safety image analysis failed");
            throw;
        }
    }
}

public sealed class ContentSafetyOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public bool ExcludeManagedIdentity { get; set; }
}

public static class ContentModerationRegistration
{
    public static IServiceCollection AddProductContentModeration(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOptions<ContentSafetyOptions>()
            .Bind(configuration.GetSection("ContentSafety"))
            .PostConfigure(options =>
            {
                if (environment.IsDevelopment())
                    options.ExcludeManagedIdentity = true;
            })
            .Validate(options => Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint) &&
                                 endpoint.Scheme == Uri.UriSchemeHttps,
                "ContentSafety:Endpoint must be an absolute HTTPS URI.")
            .ValidateOnStart();
        services.AddSingleton<IContentModerationService, ContentModerationService>();
        return services;
    }
}
