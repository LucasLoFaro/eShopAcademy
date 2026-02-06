using Core.Application.Interfaces;
using Domain.Orders.Entities;
using System.Net.Http.Json;


namespace Infrastructure.Clients;

public class ProductServiceClient : IProductServiceClient
{
    private readonly HttpClient _httpClient;

    public ProductServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Product> GetProductByIdAsync(Guid productId)
    {
        var response = await _httpClient.GetFromJsonAsync<Product>($"/api/product/{productId}");

        if (response == null)
            throw new InvalidOperationException("Product service returned no data.");

        return response;
    }
}
