namespace Domain.Shipping.Helpers;

public static class ProviderShippingStatus
{
    public const string Shipped = "shipped";
    public const string OutForDelivery = "out_for_delivery";
    public const string Delivered = "delivered";

    public static string Normalize(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return string.Empty;
        }

        return status.Trim().ToLowerInvariant();
    }
}
