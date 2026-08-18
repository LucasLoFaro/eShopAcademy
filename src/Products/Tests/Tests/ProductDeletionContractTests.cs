using System.Text.Json;
using Domain.Common.Events.Products;

namespace Products.Tests;

public class ProductDeletionContractTests
{
    [Test]
    public void ProductDeletedEvent_UsesStableVersionedIdentity()
    {
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var eventId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var message = new ProductDeletedEvent
        {
            ProductId = productId,
            EventId = eventId
        };

        Assert.Multiple(() =>
        {
            Assert.That(message.ContractVersion, Is.EqualTo(ProductDeletedEvent.CurrentContractVersion));
            Assert.That(message.EventType, Is.EqualTo("Deleted"));
            Assert.That(message.ProductId, Is.EqualTo(productId));
            Assert.That(message.CorrelationId, Is.EqualTo(productId));
            Assert.That(message.EventId, Is.EqualTo(eventId));
        });
    }

    [Test]
    public void ProductDeletedEvent_VersionOnePayloadRoundTrips()
    {
        const string payload =
            """
            {
              "ContractVersion": 1,
              "ProductId": "11111111-1111-1111-1111-111111111111",
              "EventType": "Deleted",
              "EventId": "22222222-2222-2222-2222-222222222222",
              "CorrelationId": "11111111-1111-1111-1111-111111111111",
              "TriggeredAt": "2026-08-18T12:00:00Z",
              "CreatedBy": "contract-test",
              "Source": "products",
              "Version": "test-build"
            }
            """;

        var message = JsonSerializer.Deserialize<ProductDeletedEvent>(payload);
        var serialized = JsonSerializer.SerializeToDocument(message);

        Assert.That(message, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(message!.ContractVersion, Is.EqualTo(1));
            Assert.That(message.ProductId, Is.EqualTo(Guid.Parse("11111111-1111-1111-1111-111111111111")));
            Assert.That(message.CorrelationId, Is.EqualTo(message.ProductId));
            Assert.That(message.EventId, Is.EqualTo(Guid.Parse("22222222-2222-2222-2222-222222222222")));
            Assert.That(serialized.RootElement.GetProperty("ContractVersion").GetInt32(), Is.EqualTo(1));
            Assert.That(serialized.RootElement.GetProperty("EventType").GetString(), Is.EqualTo("Deleted"));
        });
    }

    [Test]
    public void ProductUpdatedEvent_RemainsDistinctFromDeletion()
    {
        var update = new ProductUpdatedEvent
        {
            ProductId = Guid.NewGuid(),
            Name = "Existing product",
            Price = 42
        };
        var deletion = new ProductDeletedEvent { ProductId = update.ProductId };

        var updateJson = JsonSerializer.SerializeToDocument(update);
        var deletionJson = JsonSerializer.SerializeToDocument(deletion);

        Assert.Multiple(() =>
        {
            Assert.That(update.EventType, Is.EqualTo("Updated"));
            Assert.That(deletion.EventType, Is.EqualTo("Deleted"));
            Assert.That(updateJson.RootElement.TryGetProperty("ContractVersion", out _), Is.False);
            Assert.That(deletionJson.RootElement.TryGetProperty("Name", out _), Is.False);
            Assert.That(deletionJson.RootElement.TryGetProperty("Price", out _), Is.False);
        });
    }
}
