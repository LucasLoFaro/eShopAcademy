using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Configuration;

public sealed class PaymentSecurityOptions
{
    public const string SectionName = "Payment";

    [Required]
    [MinLength(32)]
    public string SignatureSecret { get; set; } = string.Empty;
}
