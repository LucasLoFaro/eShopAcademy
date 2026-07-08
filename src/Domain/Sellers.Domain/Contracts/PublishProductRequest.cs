namespace Domain.Sellers.Contracts;

public record PublishProductRequest(
    string Name,
    double Price,
    string Description,
    string ImageUrl,
    string CategoryId,
    List<string> AdditionalImages,
    string AboutHtml,
    List<ProductSpecRequest> Specs,
    List<ProductFaqRequest> Faqs);

public record ProductSpecRequest(string Label, string Value);

public record ProductFaqRequest(string Question, string Answer);
