using System.Reflection;

namespace NotificationService.Templates;

public interface IEmailTemplateRenderer
{
    string Render(string templateName, Dictionary<string, string> placeholders);
}

public class EmailTemplateRenderer : IEmailTemplateRenderer
{
    private readonly Dictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _templateDir;

    public EmailTemplateRenderer()
    {
        _templateDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Templates");
    }

    // Constructor for testing — allows injecting a custom template directory
    public EmailTemplateRenderer(string templateDir)
    {
        _templateDir = templateDir;
    }

    public string Render(string templateName, Dictionary<string, string> placeholders)
    {
        var html = LoadTemplate(templateName);

        foreach (var (key, value) in placeholders)
        {
            html = html.Replace($"{{{{{key}}}}}", value);
        }

        return html;
    }

    private string LoadTemplate(string templateName)
    {
        if (_cache.TryGetValue(templateName, out var cached))
            return cached;

        var path = Path.Combine(_templateDir, $"{templateName}.html");
        if (!File.Exists(path))
            throw new FileNotFoundException($"Email template '{templateName}' not found at {path}.");

        var content = File.ReadAllText(path);
        _cache[templateName] = content;
        return content;
    }
}
