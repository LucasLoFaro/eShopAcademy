namespace Domain.Products.Contracts;

public class ProductSearchFilter
{
    public string? SearchText { get; set; }
    public string? Category { get; set; }
    public double? MinPrice { get; set; }
    public double? MaxPrice { get; set; }
    public bool? Deals { get; set; }
    public bool? InStock { get; set; }
    public double? MinRating { get; set; }
    public string? Sort { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}