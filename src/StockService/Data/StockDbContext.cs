using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;

namespace Data
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

            modelBuilder.Entity<Stock>().ToCollection("Stocks");
        }
    }
}
