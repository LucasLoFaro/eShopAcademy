using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Data
{
    public interface IProductsRepository
    {
        public Task<IEnumerable<Product>> GetAllAsync();
        public Task<Product> GetByIdAsync(Guid id);
        public Task AddAsync(Product product);
        public Task DeleteAsync(Product product);
        public Task UpdateAsync(Product product);
        public Task<Product> GetMostExpensive();

    }
}
