namespace Shipping.Application.Options;

public sealed class ShippingProviderOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string SchedulePath { get; set; } = string.Empty;
    public string HistoryPathFormat { get; set; } = string.Empty;
    public string ConfirmPickupPath { get; set; } = string.Empty;
}
