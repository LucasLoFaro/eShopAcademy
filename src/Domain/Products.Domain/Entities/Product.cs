using Domain.Product.Entities;
using System.ComponentModel.DataAnnotations;

namespace Domain.Products.Entities;

public class Product : BaseEntity
{
    [Required]
    public string Name { get; set; } = "";

    [Required]
    public double Price { get; set; }

    public string Description { get; set; } = "";

    public string ImageUrl { get; set; } = "";

    public string CategoryId { get; set; } = "";
    public Category? Category { get; set; }

    // ── Enhanced fields ──
    public List<string> AdditionalImages { get; set; } = [];
    public string AboutHtml { get; set; } = "";
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsBestSeller { get; set; }
    public bool IsDeal { get; set; }
    public double? DealPrice { get; set; }
    public bool IsNewRelease { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<ProductSpec> Specs { get; set; } = [];
    public List<ProductFaq> Faqs { get; set; } = [];
}

public class ProductSpec
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
}

public class ProductFaq
{
    public string Question { get; set; } = "";
    public string Answer { get; set; } = "";
}
