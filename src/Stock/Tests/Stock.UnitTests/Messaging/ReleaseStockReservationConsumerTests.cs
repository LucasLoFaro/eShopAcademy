using AutoFixture.Xunit2;
using Domain.Common.Commands.Stock;
using Domain.Common.Events.Stock;
using Domain.Stock.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Services;
using MassTransit;
using Moq;
using Stock.Messaging.Processor.Consumers;
using Xunit;

namespace Stock.Tests.Messaging;

public class ReleaseStockReservationConsumerTests
{
    [Theory]
    [AutoData]
    public async Task Consume_ReleasesReservationAndPublishesEvent(ReleaseStockReservationCommand command)
    {
        // Arrange
        var repository = new InMemoryStockReservationRepository();
        var stockRepository = new InMemoryStockRepository();
        var publishEndpoint = new Mock<IPublishEndpoint>();
        publishEndpoint
            .Setup(endpoint => endpoint.Publish(It.IsAny<StockReleasedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var commandWithReason = command with { Reason = "Payment cancelled" };
        var messagingClient = new StockMessagingClient(publishEndpoint.Object);
        var consumer = new ReleaseStockReservationConsumer(repository, stockRepository, messagingClient);
        var context = CreateContext(commandWithReason);

        var stockItem = new StockItem
        {
            ProductID = Guid.NewGuid(),
            Quantity = 2
        };
        var reservation = CreateReservation(commandWithReason, DateTime.UtcNow.AddMinutes(5), stockItem);

        // Seed inventory with a baseline quantity so release adds the reservation quantity.
        var existingStock = CreateStock(stockItem.ProductID, 5);

        await repository.CreateAsync(reservation, CancellationToken.None);
        await stockRepository.AddOrUpdateAsync(existingStock, CancellationToken.None);

        // Act
        await consumer.Consume(context.Object);

        // Assert
        var updatedStock = await stockRepository.GetByProductIdAsync(stockItem.ProductID, CancellationToken.None);
        updatedStock.Should().NotBeNull();
        updatedStock!.Quantity.Should().Be(7);

        var updatedReservation = await repository.GetByIdAsync(commandWithReason.ReservationId, CancellationToken.None);
        updatedReservation.Should().NotBeNull();
        updatedReservation!.IsCommitted.Should().BeFalse();

        publishEndpoint.Verify(endpoint => endpoint.Publish(
                It.Is<StockReleasedEvent>(evt =>
                    evt.OrderId == commandWithReason.OrderId &&
                    evt.ReservationId == commandWithReason.ReservationId &&
                    evt.Reason == commandWithReason.Reason),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task Consume_WhenReservationMissing_DoesNothing(ReleaseStockReservationCommand command)
    {
        // Arrange
        var repository = new InMemoryStockReservationRepository();
        var stockRepository = new InMemoryStockRepository();
        var publishEndpoint = new Mock<IPublishEndpoint>();

        var messagingClient = new StockMessagingClient(publishEndpoint.Object);
        var consumer = new ReleaseStockReservationConsumer(repository, stockRepository, messagingClient);
        var context = CreateContext(command);

        var existingStock = CreateStock(Guid.NewGuid(), 4);
        await stockRepository.AddOrUpdateAsync(existingStock, CancellationToken.None);

        // Act
        await consumer.Consume(context.Object);

        // Assert
        stockRepository.AddOrUpdateCalls.Should().Be(1);
        publishEndpoint.Verify(endpoint => endpoint.Publish(
                It.IsAny<StockReleasedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [AutoData]
    public async Task Consume_WhenReservationCommitted_DoesNothing(ReleaseStockReservationCommand command)
    {
        // Arrange
        var repository = new InMemoryStockReservationRepository();
        var stockRepository = new InMemoryStockRepository();
        var publishEndpoint = new Mock<IPublishEndpoint>();

        var messagingClient = new StockMessagingClient(publishEndpoint.Object);
        var consumer = new ReleaseStockReservationConsumer(repository, stockRepository, messagingClient);
        var context = CreateContext(command);

        var stockItem = new StockItem
        {
            ProductID = Guid.NewGuid(),
            Quantity = 2
        };
        var reservation = CreateReservation(command, DateTime.UtcNow.AddMinutes(5), stockItem, true);
        var existingStock = CreateStock(stockItem.ProductID, 6);

        await repository.CreateAsync(reservation, CancellationToken.None);
        await stockRepository.AddOrUpdateAsync(existingStock, CancellationToken.None);

        // Act
        await consumer.Consume(context.Object);

        // Assert
        var updatedStock = await stockRepository.GetByProductIdAsync(stockItem.ProductID, CancellationToken.None);
        updatedStock.Should().NotBeNull();
        updatedStock!.Quantity.Should().Be(6);

        publishEndpoint.Verify(endpoint => endpoint.Publish(
                It.IsAny<StockReleasedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [AutoData]
    public async Task Consume_WhenReservationExpired_DoesNothing(ReleaseStockReservationCommand command)
    {
        // Arrange
        var repository = new InMemoryStockReservationRepository();
        var stockRepository = new InMemoryStockRepository();
        var publishEndpoint = new Mock<IPublishEndpoint>();

        var messagingClient = new StockMessagingClient(publishEndpoint.Object);
        var consumer = new ReleaseStockReservationConsumer(repository, stockRepository, messagingClient);
        var context = CreateContext(command);

        var stockItem = new StockItem
        {
            ProductID = Guid.NewGuid(),
            Quantity = 2
        };
        var reservation = CreateReservation(command, DateTime.UtcNow.AddMinutes(-1), stockItem);
        var existingStock = CreateStock(stockItem.ProductID, 6);

        await repository.CreateAsync(reservation, CancellationToken.None);
        await stockRepository.AddOrUpdateAsync(existingStock, CancellationToken.None);

        // Act
        await consumer.Consume(context.Object);

        // Assert
        var updatedStock = await stockRepository.GetByProductIdAsync(stockItem.ProductID, CancellationToken.None);
        updatedStock.Should().NotBeNull();
        updatedStock!.Quantity.Should().Be(6);

        publishEndpoint.Verify(endpoint => endpoint.Publish(
                It.IsAny<StockReleasedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static StockReservation CreateReservation(
        ReleaseStockReservationCommand command,
        DateTime validUntil,
        StockItem stockItem,
        bool isCommitted = false)
    {
        return new StockReservation
        {
            Id = command.ReservationId,
            OrderId = command.OrderId,
            IsCommitted = isCommitted,
            ValidUntil = validUntil,
            Items =
            [
                new ReservationItem
                {
                    Warehouse = "primary",
                    Items = [stockItem]
                }
            ]
        };
    }

    private static Domain.Stock.Entities.Stock CreateStock(Guid productId, int quantity)
    {
        return new Domain.Stock.Entities.Stock
        {
            ProductID = productId,
            Quantity = quantity,
            Warehouse = "primary"
        };
    }

    private static Mock<ConsumeContext<ReleaseStockReservationCommand>> CreateContext(
        ReleaseStockReservationCommand command)
    {
        var context = new Mock<ConsumeContext<ReleaseStockReservationCommand>>();
        context.SetupGet(ctx => ctx.Message).Returns(command);
        context.SetupGet(ctx => ctx.CancellationToken).Returns(CancellationToken.None);
        return context;
    }

    private sealed class InMemoryStockReservationRepository : IStockReservationRepository
    {
        private readonly Dictionary<Guid, StockReservation> _reservations = new();

        public Task CreateAsync(StockReservation reservation, CancellationToken ct)
        {
            _reservations[reservation.Id] = reservation;
            return Task.CompletedTask;
        }

        public Task<StockReservation?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            _reservations.TryGetValue(id, out var reservation);
            return Task.FromResult(reservation);
        }

        public Task UpdateAsync(StockReservation reservation, CancellationToken ct)
        {
            _reservations[reservation.Id] = reservation;
            return Task.CompletedTask;
        }

        public Task<List<StockReservation>> GetExpiredAsync(CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var expired = _reservations.Values
                .Where(reservation => !reservation.IsCommitted && reservation.ValidUntil < now)
                .ToList();
            return Task.FromResult(expired);
        }
    }

    private sealed class InMemoryStockRepository : IStockRepository
    {
        private readonly Dictionary<Guid, Domain.Stock.Entities.Stock> _stocks = new();

        public int AddOrUpdateCalls { get; private set; }

        public Task<IReadOnlyList<Domain.Stock.Entities.Stock>> GetAllAsync(CancellationToken ct = default)
        {
            return Task.FromResult((IReadOnlyList<Domain.Stock.Entities.Stock>)_stocks.Values.ToList());
        }

        public Task<IReadOnlyList<Domain.Stock.Entities.Stock>> GetByProductGuidAsync(Guid productGuid, CancellationToken ct = default)
        {
            var matches = _stocks.Values.Where(stock => stock.ProductID == productGuid).ToList();
            return Task.FromResult((IReadOnlyList<Domain.Stock.Entities.Stock>)matches);
        }

        public Task<Domain.Stock.Entities.Stock> GetByProductIdAsync(Guid productGuid, CancellationToken ct = default)
        {
            _stocks.TryGetValue(productGuid, out var stock);
            return Task.FromResult(stock!);
        }

        public Task<Domain.Stock.Entities.Stock> AddOrUpdateAsync(Domain.Stock.Entities.Stock stock, CancellationToken ct = default)
        {
            _stocks[stock.ProductID] = stock;
            AddOrUpdateCalls++;
            return Task.FromResult(stock);
        }
    }
}
