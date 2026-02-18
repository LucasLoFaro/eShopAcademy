using FluentAssertions;
using NotificationService.Templates;
using Xunit;

namespace Notification.Tests;

/// <summary>
/// Tests EmailTemplateRenderer by writing real template files to a temp directory
/// and pointing the renderer's Templates folder there via a thin factory helper.
/// </summary>
public class EmailTemplateRendererTests : IDisposable
{
    private readonly string _templatesDir;

    public EmailTemplateRendererTests()
    {
        _templatesDir = Path.Combine(Path.GetTempPath(), $"notification-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_templatesDir);
    }

    public void Dispose() => Directory.Delete(_templatesDir, recursive: true);

    private EmailTemplateRenderer CreateSutWithTemplate(string name, string html)
    {
        File.WriteAllText(Path.Combine(_templatesDir, $"{name}.html"), html);
        return new EmailTemplateRenderer(_templatesDir);
    }

    [Fact]
    public void Render_ReplacesAllPlaceholders()
    {
        // Arrange
        var renderer = CreateSutWithTemplate(
            "Test",
            "Hello {{CustomerName}}, your order {{OrderNumber}} is ready on {{Date}}.");

        var placeholders = new Dictionary<string, string>
        {
            ["CustomerName"] = "Alice",
            ["OrderNumber"] = "ORD-42",
            ["Date"] = "January 1, 2025"
        };

        // Act
        var result = renderer.Render("Test", placeholders);

        // Assert
        result.Should().Be("Hello Alice, your order ORD-42 is ready on January 1, 2025.");
    }

    [Fact]
    public void Render_WithNoMatchingPlaceholders_ReturnsTemplateUnchanged()
    {
        // Arrange
        const string template = "<p>Static content</p>";
        var renderer = CreateSutWithTemplate("Static", template);

        // Act
        var result = renderer.Render("Static", new Dictionary<string, string>());

        // Assert
        result.Should().Be(template);
    }

    [Fact]
    public void Render_UnknownTemplate_ThrowsFileNotFoundException()
    {
        // Arrange
        var renderer = new EmailTemplateRenderer(_templatesDir);

        // Act
        var act = () => renderer.Render("DoesNotExist", new Dictionary<string, string>());

        // Assert
        act.Should().Throw<FileNotFoundException>();
    }
}

