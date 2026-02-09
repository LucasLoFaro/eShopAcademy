using Domain.Operations.Entities;
using Domain.Operations.Enums;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace Operations.Application.Repositories;

public class PackageRepository : IPackageRepository
{
    private readonly IMongoCollection<Package> _collection;

    public PackageRepository(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("operations");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Missing connection string for the operations Mongo database.");
        }

        var databaseName = configuration["Operations:Database"] ?? "operations";
        var collectionName = configuration["Operations:PackagesCollection"] ?? "packages";

        var client = new MongoClient(connectionString);
        _collection = client.GetDatabase(databaseName).GetCollection<Package>(collectionName);
    }

    public async Task<IReadOnlyList<Package>> GetPendingAsync(CancellationToken cancellationToken)
    {
        return await _collection.Find(p => p.Status == PackageStatus.Pending).ToListAsync(cancellationToken);
    }

    public async Task<Package?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return await _collection.Find(p => p.OrderId == orderId).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Package> CreateOrUpdateAsync(Package package, CancellationToken cancellationToken)
    {
        package.UpdatedAt = DateTime.UtcNow;
        await _collection.ReplaceOneAsync(
            p => p.OrderId == package.OrderId,
            package,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);

        return package;
    }
}
