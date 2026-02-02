using AutoFixture.Xunit2;
using Common.Domain.Commands.Stock;
using Common.Domain.Events.Stock;
using Domain.Stock.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Services;
using MassTransit;
using Moq;
using Stock.Messaging.Processor.Consumers;
using Xunit;

namespace Stock.Tests.Messaging;

public class CommitStockReservationConsumerTests
{
    [Theory]
    [AutoData]
    public async Task Consume_CommitsReservationAndPublishesEvent(CommitStockReservationCommand command)
    {
        // Arrange
        var repository = new InMemoryStockReservationRepository();
        var publishEndpoint = new Mock<IPublishEndpoint>();
        publishEndpoint
            .Setup(endpoint => endpoint.Publish(It.IsAny<StockReservationCommittedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var messagingClient = new StockMessagingClient(publishEndpoint.Object);
        var consumer = new CommitStockReservationConsumer(repository, messagingClient);
        var context = CreateContext(command);
        var reservation = CreateReservation(command, DateTime.UtcNow.AddMinutes(5));

        await repository.CreateAsync(reservation, CancellationToken.None);

        // Act
        await consumer.Consume(context.Object);

        // Assert
        var updated = await repository.GetByIdAsync(command.ReservationId, CancellationToken.None);
        updated.Should().NotBeNull();
        updated!.IsCommitted.Should().BeTrue();
        updated.CommittedAt.Should().NotBeNull();

        publishEndpoint.Verify(endpoint => endpoint.Publish(
                It.Is<StockReservationCommittedEvent>(evt =>
                    evt.OrderId == command.OrderId && evt.ReservationId == command.ReservationId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task Consume_WhenReservationMissing_PublishesFailure(CommitStockReservationCommand command)
    {
        // Arrange
        var repository = new InMemoryStockReservationRepository();
        var publishEndpoint = new Mock<IPublishEndpoint>();

        var messagingClient = new StockMessagingClient(publishEndpoint.Object);
        var consumer = new CommitStockReservationConsumer(repository, messagingClient);
        var context = CreateContext(command);
        context.Setup(ctx => ctx.Publish(It.IsAny<StockReservationCommitFailedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await consumer.Consume(context.Object);

        // Assert
        context.Verify(ctx => ctx.Publish(
                It.Is<StockReservationCommitFailedEvent>(evt =>
                    evt.OrderId == command.OrderId && evt.ReservationId == command.ReservationId),
                It.IsAny<CancellationToken>()),
            Times.Once);
        publishEndpoint.Verify(endpoint => endpoint.Publish(
                It.IsAny<StockReservationCommittedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [AutoData]
    public async Task Consume_WhenReservationAlreadyCommitted_PublishesFailure(CommitStockReservationCommand command)
    {
        // Arrange
        var repository = new InMemoryStockReservationRepository();
        var publishEndpoint = new Mock<IPublishEndpoint>();

        var messagingClient = new StockMessagingClient(publishEndpoint.Object);
        var consumer = new CommitStockReservationConsumer(repository, messagingClient);
        var context = CreateContext(command);
        context.Setup(ctx => ctx.Publish(It.IsAny<StockReservationCommitFailedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var committedAt = DateTime.UtcNow.AddMinutes(-1);
        var reservation = CreateReservation(command, DateTime.UtcNow.AddMinutes(5), true, committedAt);

        await repository.CreateAsync(reservation, CancellationToken.None);

        // Act
        await consumer.Consume(context.Object);

        // Assert
        var updated = await repository.GetByIdAsync(command.ReservationId, CancellationToken.None);
        updated.Should().NotBeNull();
        updated!.IsCommitted.Should().BeTrue();
        updated.CommittedAt.Should().Be(committedAt);

        context.Verify(ctx => ctx.Publish(
                It.Is<StockReservationCommitFailedEvent>(evt =>
                    evt.OrderId == command.OrderId && evt.ReservationId == command.ReservationId),
                It.IsAny<CancellationToken>()),
            Times.Once);
        publishEndpoint.Verify(endpoint => endpoint.Publish(
                It.IsAny<StockReservationCommittedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [AutoData]
    public async Task Consume_WhenReservationExpired_PublishesFailure(CommitStockReservationCommand command)
    {
        // Arrange
        var repository = new InMemoryStockReservationRepository();
        var publishEndpoint = new Mock<IPublishEndpoint>();

        var messagingClient = new StockMessagingClient(publishEndpoint.Object);
        var consumer = new CommitStockReservationConsumer(repository, messagingClient);
        var context = CreateContext(command);
        context.Setup(ctx => ctx.Publish(It.IsAny<StockReservationCommitFailedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var reservation = CreateReservation(command, DateTime.UtcNow.AddMinutes(-1));

        await repository.CreateAsync(reservation, CancellationToken.None);

        // Act
        await consumer.Consume(context.Object);

        // Assert
        var updated = await repository.GetByIdAsync(command.ReservationId, CancellationToken.None);
        updated.Should().NotBeNull();
        updated!.IsCommitted.Should().BeFalse();
        updated.CommittedAt.Should().BeNull();

        context.Verify(ctx => ctx.Publish(
                It.Is<StockReservationCommitFailedEvent>(evt =>
                    evt.OrderId == command.OrderId && evt.ReservationId == command.ReservationId),
                It.IsAny<CancellationToken>()),
            Times.Once);
        publishEndpoint.Verify(endpoint => endpoint.Publish(
                It.IsAny<StockReservationCommittedEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static StockReservation CreateReservation(
        CommitStockReservationCommand command,
        DateTime validUntil,
        bool isCommitted = false,
        DateTime? committedAt = null)
    {
        return new StockReservation
        {
            Id = command.ReservationId,
            OrderId = command.OrderId,
            IsCommitted = isCommitted,
            CommittedAt = committedAt,
            ValidUntil = validUntil,
            Items =
            [
                new ReservationItem
                {
                    Warehouse = "primary",
                    Items =
                    [
                        new StockItem
                        {
                            ProductID = Guid.NewGuid(),
                            Quantity = 1
                        }
                    ]
                }
            ]
        };
    }

    private static Mock<ConsumeContext<CommitStockReservationCommand>> CreateContext(
        CommitStockReservationCommand command)
    {
        var context = new Mock<ConsumeContext<CommitStockReservationCommand>>();
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
}
