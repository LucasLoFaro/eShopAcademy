using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class StockRepository : IStockRepository
    {

        public readonly StockDbContext _context;

        public StockRepository(StockDbContext context)
        {
            _context = context;
        }


        public async Task<IReadOnlyList<Stock>> GetAllAsync()
        {
            return await _context.Stocks.ToListAsync();
        }

        public async Task<IReadOnlyList<Stock>> GetByProductGuidAsync(string productGuid)
        {
            IReadOnlyList<Stock> stock = await _context.Stocks.Where(
                x => x.ProductGuid == productGuid &&
                x.Quantity != 0).ToListAsync();

            return stock;
        }

        public async Task<Stock> GetByProductGuidAndWarehouseAsync(string productGuid, string warehouse)
        {
            Stock stock = await _context.Stocks.Where(
                    x => x.ProductGuid == productGuid &&
                    x.Warehouse == warehouse &&
                    x.Quantity != 0).FirstOrDefaultAsync();
            return stock;
        }

        public async Task<Stock> AddAsync(Stock stock)
        {
            _context.Stocks.Add(stock);
            await _context.SaveChangesAsync();
            return stock;
        }

        public async Task<bool> UpdateAsync(Stock stock)
        {
            _context.Stocks.Update(stock);
            return await _context.SaveChangesAsync() > 0 ? true : false;
        }

    }
}