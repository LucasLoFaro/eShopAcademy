using Core.Domain.Entities;
using MongoDB.Driver;


namespace Infrastructure.Data;

public class StockRepository : IStockRepository
{
    private readonly IMongoCollection<Stock> _stocks;

    public StockRepository(StockDbContext context)
    {
        _stocks = context.Stocks;
    }

    public async Task<IReadOnlyList<Stock>> GetAllAsync()
    {
        return await _stocks.Find(Builders<Stock>.Filter.Empty).ToListAsync();
    }

    public async Task<IReadOnlyList<Stock>> GetByProductGuidAsync(Guid productGuid)
    {
        var filter = Builders<Stock>.Filter.And(
            Builders<Stock>.Filter.Eq(s => s.ProductID, productGuid),
            Builders<Stock>.Filter.Ne(s => s.Quantity, 0)
        );
        return await _stocks.Find(filter).ToListAsync();
    }

    public async Task<Stock?> GetByProductGuidAndWarehouseAsync(Guid productGuid, string warehouse)
    {
        var filter = Builders<Stock>.Filter.And(
            Builders<Stock>.Filter.Eq(s => s.ProductID, productGuid),
            Builders<Stock>.Filter.Eq(s => s.Warehouse, warehouse),
            Builders<Stock>.Filter.Ne(s => s.Quantity, 0)
        );
        return await _stocks.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<Stock> AddAsync(Stock stock)
    {
        await _stocks.InsertOneAsync(stock);
        return stock;
    }

    public async Task<bool> UpdateAsync(Stock stock)
    {
        var filter = Builders<Stock>.Filter.Eq(s => s._id, stock._id);
        var result = await _stocks.ReplaceOneAsync(filter, stock);
        return result.ModifiedCount > 0;
    }
}