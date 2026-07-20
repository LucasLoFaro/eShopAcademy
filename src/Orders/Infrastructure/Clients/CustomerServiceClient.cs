using Core.Application.Interfaces;
using Domain.Orders.Entities;
using System.Net.Http.Json;


namespace Infrastructure.Clients;

public class CustomerServiceClient : ICustomerServiceClient
{
    private readonly HttpClient _httpClient;

    public CustomerServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<OrderCustomerInfo> GetCustomerByIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        // Calls Customers.Api minimal endpoint which currently returns a hardcoded customer
        var response = await _httpClient.GetFromJsonAsync<OrderCustomerInfo>(
            $"/customers/{customerId}",
            cancellationToken);

        if (response == null)
            throw new InvalidOperationException("Customer service returned no data.");

        return response;
    }
}
