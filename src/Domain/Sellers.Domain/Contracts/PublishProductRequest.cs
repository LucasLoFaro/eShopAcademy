using System.ComponentModel.DataAnnotations;

namespace Domain.Sellers.Contracts;

public record PublishProductRequest(
    [Required] string Name,
    double Price,
    string Description,
    [Required, Url] string ImageUrl,
    string CategoryId,
    List<string> AdditionalImages,
    string AboutHtml,
    List<ProductSpecRequest> Specs,
    List<ProductFaqRequest> Faqs,
    int InitialStock = 0);

public record ProductSpecRequest(string Label, string Value);

public record ProductFaqRequest(string Question, string Answer);
