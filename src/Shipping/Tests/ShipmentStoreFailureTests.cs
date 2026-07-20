using FluentAssertions;
using Moq;
using Shipping.Simulator.Storage;
using StackExchange.Redis;
using Xunit;

namespace Shipping.Tests;

public sealed class ShipmentStoreFailureTests
{
    [Fact]
    public async Task GetAllAsync_WhenRedisFails_PropagatesStorageFailure()
    {
        var database = new Mock<IDatabase>();
        database.Setup(db => db.SetMembersAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new TimeoutException("redis unavailable"));
        var connection = new Mock<IConnectionMultiplexer>();
        connection.Setup(multiplexer => multiplexer.GetDatabase(1, It.IsAny<object>()))
            .Returns(database.Object);
        var store = new ShipmentStore(connection.Object, 1);

        var action = () => store.GetAllAsync();

        await action.Should().ThrowAsync<TimeoutException>();
    }
}
