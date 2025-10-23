using Domain.Stock.Entities;
using MongoDB.Driver;


namespace Infrastructure.Data;

public class StockRepository : IStockRepository
{
    private readonly IMongoCollection<Stock> _stocks;

    public StockRepository(StockDbContext context)
    {
        _stocks = context.Stocks;
    }

    public async Task<IReadOnlyList<Stock>> GetAllAsync(CancellationToken ct = default)
    {
        return await _stocks.Find(Builders<Stock>.Filter.Empty).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Stock>> GetByProductGuidAsync(Guid productGuid, CancellationToken ct = default)
    {
        var filter = Builders<Stock>.Filter.And(
            Builders<Stock>.Filter.Eq(s => s.ProductID, productGuid),
            Builders<Stock>.Filter.Ne(s => s.Quantity, 0)
        );
        return await _stocks.Find(filter).ToListAsync();
    }

    // TODO: Filter by warehouse
    public async Task<Stock?> GetByProductIdAsync(Guid productGuid, CancellationToken ct = default)
    {
        var filter = Builders<Stock>.Filter.And(
            Builders<Stock>.Filter.Eq(s => s.ProductID, productGuid),
            Builders<Stock>.Filter.Ne(s => s.Quantity, 0)
        );
        return await _stocks.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<Stock> AddOrUpdateAsync(Stock stock, CancellationToken ct = default)
    {
        var filter = Builders<Stock>.Filter.Eq(s => s.ProductID, stock.ProductID);

        var options = new ReplaceOptions { IsUpsert = true };

        await _stocks.ReplaceOneAsync(filter, stock, options, ct);

        return stock;
    }

}