namespace Shipping.Application.Options;

public sealed class ShippingProviderOptions
{
    public string BaseUrl { get; set; } = string.Empty;

    public static bool IsValid(ShippingProviderOptions options) =>
        Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

