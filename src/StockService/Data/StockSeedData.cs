using Core.Domain.DTOs;
using Core.Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Services;


namespace Data;

public static class StockSeedData
{
    public static async Task InitializeAsync(
        StockDbContext context,
        StockMessagingClient messaging,
        CancellationToken ct = default)
    {
        var stockExists = await context.Stocks.EstimatedDocumentCountAsync() > 0;
        if (!stockExists)
        {
            var stocks = new List<Stock>
            {
                new(){ ProductID = new Guid("8b6e2e1d-6f1d-4b0d-9f6c-1a4c7b9d2e8f"), Quantity = 4, Warehouse = "WH-01" },
                new(){ ProductID = new Guid("c2d3f8a1-9b4d-41ea-b77f-5e20b39a48a6"), Quantity = 7, Warehouse = "WH-01" },
                new(){ ProductID = new Guid("4f2a0b63-8e3d-4f7d-9b5c-1e19f9c3d21b"), Quantity = 2, Warehouse = "WH-02" }
            };
            await context.Stocks.InsertManyAsync(stocks, cancellationToken: ct);

            foreach (var stock in stocks)
                await messaging.SendStockUpdate(new AlterStockDTO(stock), ct);
        }   
    }
}

