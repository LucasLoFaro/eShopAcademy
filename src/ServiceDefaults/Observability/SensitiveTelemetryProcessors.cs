using System.Diagnostics;
using System.Text.RegularExpressions;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace ServiceDefaults;

public static partial class TelemetrySanitizer
{
    private static readonly string[] SensitiveKeyFragments =
    [
        "authorization", "api-key", "apikey", "cookie", "connectionstring", "connection_string",
        "password", "secret", "token", "message.body", "messaging.message.payload", "payload",
        "payment", "refund", "card", "cvv", "customer", "email", "address", "tax", "psp", "credential"
    ];

    public static bool IsSensitiveKey(string key) =>
        SensitiveKeyFragments.Any(fragment => key.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    public static string RedactText(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        var sanitized = ConnectionStringPattern().Replace(value, "[REDACTED]");
        sanitized = BearerPattern().Replace(sanitized, "$1[REDACTED]");
        sanitized = EmailPattern().Replace(sanitized, "[REDACTED-EMAIL]");
        sanitized = PaymentCardPattern().Replace(sanitized, "[REDACTED-NUMBER]");
        return StripQueryString(sanitized);
    }

    public static string StripQueryString(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Query))
        {
            return value;
        }

        return uri.GetLeftPart(UriPartial.Path);
    }

    internal static IReadOnlyList<KeyValuePair<string, object?>> SanitizeAttributes(
        IEnumerable<KeyValuePair<string, object?>>? attributes)
    {
        if (attributes is null)
        {
            return [];
        }

        return attributes.Select(attribute => new KeyValuePair<string, object?>(
                attribute.Key,
                IsSensitiveKey(attribute.Key)
                    ? "[REDACTED]"
                    : attribute.Value is string text
                        ? RedactText(text)
                        : attribute.Value))
            .ToArray();
    }

    [GeneratedRegex(@"(?i)(AccountKey|SharedAccessKey|Password|Secret|ApiKey|InstrumentationKey)\s*=\s*[^;\s]+")]
    private static partial Regex ConnectionStringPattern();

    [GeneratedRegex(@"(?i)(Bearer\s+)[A-Za-z0-9._~+/=-]+")]
    private static partial Regex BearerPattern();

    [GeneratedRegex(@"(?i)\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b")]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"(?<!\d)(?:\d[ -]?){13,19}(?!\d)")]
    private static partial Regex PaymentCardPattern();
}

internal sealed class SensitiveActivityProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        foreach (var tag in activity.TagObjects.ToArray())
        {
            if (TelemetrySanitizer.IsSensitiveKey(tag.Key))
            {
                activity.SetTag(tag.Key, "[REDACTED]");
            }
            else if (tag.Value is string value)
            {
                activity.SetTag(tag.Key, TelemetrySanitizer.RedactText(value));
            }
        }
    }
}

internal sealed class SensitiveLogRecordProcessor : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord record)
    {
        record.Body = TelemetrySanitizer.RedactText(record.Body);
        record.FormattedMessage = null;
        record.Attributes = TelemetrySanitizer.SanitizeAttributes(record.Attributes);
        record.Exception = null;
    }
}
