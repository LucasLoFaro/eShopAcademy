using Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;
using System.ComponentModel;

namespace Infrastructure.Data
{
    public class StockDbContext: DbContext
    {
        public DbSet<Stock> Stocks { get; init; }

        public static StockDbContext Create(IMongoDatabase database) =>
            new(new DbContextOptionsBuilder<StockDbContext>()
                .UseMongoDB(database.Client, database.DatabaseNamespace.DatabaseName)
                .Options);

        public StockDbContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Stock>().ToCollection("stock");
            modelBuilder.Entity<Warehouse>().ToCollection("warehouses");
        }
    }
}
