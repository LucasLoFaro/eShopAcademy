using MongoDB.Bson;
using MongoDB.Driver;

const string connectionVariable = "DOCUMENTDB_CONNECTION_STRING";
var connectionString = Environment.GetEnvironmentVariable(connectionVariable);

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine($"{connectionVariable} is required.");
    return 2;
}

var settings = MongoClientSettings.FromConnectionString(connectionString);
settings.ConnectTimeout = TimeSpan.FromSeconds(15);
settings.ServerSelectionTimeout = TimeSpan.FromSeconds(15);

var client = new MongoClient(settings);

if (args.Contains("--bootstrap", StringComparer.OrdinalIgnoreCase))
{
    var databaseNames = new[]
    {
        "stock",
        "customers",
        "shipping",
        "operations",
        "notifications",
        "sellers",
        "products"
    };

    foreach (var databaseName in databaseNames)
    {
        var database = client.GetDatabase(databaseName);
        await database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));

        var metadata = database.GetCollection<BsonDocument>("__qa_metadata");
        await metadata.Indexes.CreateOneAsync(
            new CreateIndexModel<BsonDocument>(
                Builders<BsonDocument>.IndexKeys.Ascending("name"),
                new CreateIndexOptions { Unique = true, Name = "ux_name" }));

        await metadata.ReplaceOneAsync(
            Builders<BsonDocument>.Filter.Eq("name", "environment"),
            new BsonDocument
            {
                ["name"] = "environment",
                ["value"] = "qa",
                ["updatedAt"] = DateTime.UtcNow
            },
            new ReplaceOptions { IsUpsert = true });

        Console.WriteLine($"BOOTSTRAPPED {databaseName}");
    }

    return 0;
}

const string compatibilityDatabaseName = "documentdb_compatibility";
var compatibilityDatabase = client.GetDatabase(compatibilityDatabaseName);
var collection = compatibilityDatabase.GetCollection<BsonDocument>("driver_checks");

try
{
    await compatibilityDatabase.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));

    await collection.Indexes.CreateOneAsync(
        new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("externalId"),
            new CreateIndexOptions { Unique = true, Name = "ux_external_id" }));

    var externalId = Guid.NewGuid();
    await collection.InsertOneAsync(new BsonDocument
    {
        ["externalId"] = new BsonBinaryData(externalId, GuidRepresentation.Standard),
        ["createdAt"] = DateTime.UtcNow,
        ["kind"] = "compatibility-smoke"
    });

    var stored = await collection
        .Find(Builders<BsonDocument>.Filter.Eq(
            "externalId",
            new BsonBinaryData(externalId, GuidRepresentation.Standard)))
        .SingleAsync();

    if (stored["kind"] != "compatibility-smoke")
    {
        throw new InvalidOperationException("Document round-trip returned an unexpected payload.");
    }

    using var session = await client.StartSessionAsync();
    session.StartTransaction();
    await collection.InsertOneAsync(
        session,
        new BsonDocument
        {
            ["externalId"] = new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard),
            ["kind"] = "transaction-smoke"
        });
    await session.AbortTransactionAsync();

    Console.WriteLine("PASS ping,index,uuid-roundtrip,transaction-abort");
    return 0;
}
finally
{
    await client.DropDatabaseAsync(compatibilityDatabaseName);
}
