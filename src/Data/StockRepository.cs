using Domain.Entities;

namespace Data
{
    public class StockRepository : IStockRepository<Stock>
    {
        public Task<Stock> AddAsync(Stock entity)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<Stock>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task<Stock> GetByGuidAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateAsync(Stock entity)
        {
            throw new NotImplementedException();
        }
    }
}