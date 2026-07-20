using Domain.Sellers.Entities;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Microsoft.Extensions.Options;

namespace Sellers.Application.Repositories;

public class SellerRepository : ISellerRepository
{
    private readonly IMongoCollection<Seller> _collection;

    static SellerRepository()
    {
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
    }

    public SellerRepository(IOptions<SellerStorageOptions> options)
    {
        var value = options.Value;
        var client = new MongoClient(value.ConnectionString);
        _collection = client.GetDatabase(value.DatabaseName).GetCollection<Seller>(value.CollectionName);
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

    public async Task<Seller?> GetByIdentityAsync(string identityObjectId, CancellationToken cancellationToken)
    {
        return await _collection.Find(s => s.IdentityObjectId == identityObjectId).FirstOrDefaultAsync(cancellationToken);
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

    public async Task<(Seller? Seller, bool Created)> TryRegisterSaleAsync(
        Guid sellerId,
        SellerLedgerEntry entry,
        CancellationToken cancellationToken)
    {
        var filter = Builders<Seller>.Filter.And(
            Builders<Seller>.Filter.Eq(seller => seller.Id, sellerId),
            Builders<Seller>.Filter.Not(Builders<Seller>.Filter.ElemMatch(
                seller => seller.Ledger,
                existing => existing.OrderId == entry.OrderId &&
                            existing.OrderItemId == entry.OrderItemId &&
                            existing.Type == Domain.Sellers.Enums.SellerLedgerEntryType.Sale)));

        var update = Builders<Seller>.Update
            .Inc(seller => seller.AccumulatedSalesAmount, entry.GrossAmount)
            .Inc(seller => seller.AccumulatedCommissionsAmount, entry.CommissionAmount)
            .Push(seller => seller.Ledger, entry)
            .Set(seller => seller.ModifiedAt, DateTime.UtcNow);

        var updated = await _collection.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<Seller> { ReturnDocument = ReturnDocument.After },
            cancellationToken);

        if (updated is not null)
        {
            return (updated, true);
        }

        return (await GetByIdAsync(sellerId, cancellationToken), false);
    }
}
