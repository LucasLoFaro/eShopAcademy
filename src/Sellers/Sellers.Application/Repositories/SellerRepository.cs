using Domain.Sellers.Entities;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace Sellers.Application.Repositories;

public class SellerRepository : ISellerRepository
{
    private readonly IMongoCollection<Seller> _collection;

    public SellerRepository(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("sellers");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Missing connection string for the sellers Mongo database.");
        }

        var databaseName = configuration["Sellers:Database"] ?? "sellers";
        var collectionName = configuration["Sellers:SellersCollection"] ?? "sellers";

        var client = new MongoClient(connectionString);
        _collection = client.GetDatabase(databaseName).GetCollection<Seller>(collectionName);
    }

    public async Task<Seller> CreateAsync(Seller seller, CancellationToken cancellationToken)
    {
        seller.CreatedAt = DateTime.UtcNow;
        seller.ModifiedAt = DateTime.UtcNow;
        await _collection.InsertOneAsync(seller, cancellationToken: cancellationToken);
        return seller;
    }

    public async Task<Seller?> GetByIdAsync(Guid sellerId, CancellationToken cancellationToken)
    {
        return await _collection.Find(s => s.Id == sellerId).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Seller>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _collection.Find(_ => true).ToListAsync(cancellationToken);
    }

    public async Task<Seller> UpdateAsync(Seller seller, CancellationToken cancellationToken)
    {
        seller.ModifiedAt = DateTime.UtcNow;

        await _collection.ReplaceOneAsync(
            s => s.Id == seller.Id,
            seller,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);

        return seller;
    }
}
