using Application.Interfaces.Data;
using Domain.DTOs;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IProductService
    {
        Task<Product> GetMostExpensiveProduct();
        Task Create(ProductWithImage product);
    }
}
