using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data
{
    internal interface IStockRepository<T> where T : class
    {
        Task<T> GetByGuidAsync(int id);
        Task<IReadOnlyList<T>> GetAllAsync();
        Task<T> AddAsync(T entity);
        Task<bool> RemoveAsync(int id);
        Task<bool> UpdateAsync(T entity);
    }
}
