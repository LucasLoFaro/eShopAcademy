using DomainContracts = Core.Domain.Contracts;
using GrpcContracts = Protos;
using Infrastructure.Services;
using Core.Domain.Entities;
using Infrastructure.Data;
using Grpc.Core;
using Protos;


namespace gRPC.Services;

public class StockService : StockProtoService.StockProtoServiceBase
{
    private readonly IStockRepository _stockRepository;
    private readonly IStockReservationRepository _reservationRepository;
    private readonly StockMessagingClient _messagingClient;

    public StockService(IStockRepository stockRepository,
        IStockReservationRepository reservationRepository,
        StockMessagingClient messagingClient)
    {
        _stockRepository = stockRepository ?? throw new ArgumentNullException(nameof(stockRepository));
        _reservationRepository = reservationRepository ?? throw new ArgumentNullException(nameof(reservationRepository));
        _messagingClient = messagingClient ?? throw new ArgumentNullException(nameof(messagingClient));
    }


    public override async Task GetAll(GetStockByProdGuidRequest request, IServerStreamWriter<StockModel> responseStream, ServerCallContext context)
    {
        IReadOnlyList<Stock> stockList = await _stockRepository.GetAllAsync(context.CancellationToken);

        foreach (var stock in stockList)
        {
            var response = new StockModel()
            {
                ProductGuid = stock.ProductID.ToString(),
                Quantity = stock.Quantity,
                Warehouse = stock.Warehouse
                //Warehouse = new WarehouseModel(){
                //    Name = stock.Warehouse.Name,
                //    Location = new LocationModel()
                //    {
                //        Address = stock.Warehouse.Location.Address,
                //        Latitude = stock.Warehouse.Location.Latitude,
                //        Longitude = stock.Warehouse.Location.Longitude
                //    }
                //}
            };
            //var productModel = _mapper.Map<ProductModel>(product);
            await responseStream.WriteAsync(response);
        }
    }


    public override async Task<StockModel> GetStockByProductGuidAndWarehouse(GetStockByProdAndWarehouseRequest request, ServerCallContext context)
    {
        var dummyWarehouse = new Warehouse()
        {
            Name = "Dummy",
            Location = new Location()
            {
                Address = "",
                Latitude = 1,
                Longitude = 1
            }
        };

        //Stock stock = await _stockRepository.GetByProductGuidAndWarehouseAsync(new Guid(request.ProductGuid), dummyWarehouse);
        Stock stock = await _stockRepository.GetByProductIdAsync(new Guid(request.ProductGuid), context.CancellationToken);

        if (stock == null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Stock for Product with Guid={request.ProductGuid} was not found"));

        var response = new StockModel()
        {
            ProductGuid = stock.ProductID.ToString(),
            Quantity = stock.Quantity,
            Warehouse = stock.Warehouse
            //Warehouse = new WarehouseModel() // ToDo: AutoMapper
        };

        return response;
    }
    public override async Task<GrpcContracts.ReserveStockResponse> ReserveStock(
    GrpcContracts.ReserveStockRequest request,
    ServerCallContext context)
    {
        var reservation = new StockReservation(request.OrderId);
        var outOfStock = new List<string>();

        foreach (var reservationItem in request.Stock)
        {
            var items = new List<StockItem>();

            foreach (var stockItem in reservationItem.Items)
            {
                var productId = Guid.Parse(stockItem.ProductGuid);
                var stock = await _stockRepository.GetByProductIdAsync(productId, context.CancellationToken);

                if (stock == null || stock.Quantity < stockItem.Quantity)
                {
                    outOfStock.Add(stockItem.ProductGuid);
                    continue;
                }

                stock.Quantity -= stockItem.Quantity;
                await _stockRepository.AddOrUpdateAsync(stock, context.CancellationToken);

                items.Add(new StockItem
                {
                    ProductID = productId,
                    Quantity = stockItem.Quantity
                });
            }

            reservation.Items.Add(new ReservationItem
            {
                Warehouse = reservationItem.Warehouse,
                Items = items
            });
        }

        await _reservationRepository.CreateAsync(reservation, context.CancellationToken);

        var reserveRequest = new DomainContracts.ReserveStockRequest
        {
            OrderId = Guid.Parse(request.OrderId),
            Stock = reservation.Items
                .SelectMany(i => i.Items.Select(si => new Stock
                {
                    ProductID = si.ProductID,
                    Quantity = si.Quantity,
                    Warehouse = i.Warehouse
                }))
                .ToList()
        };

        await _messagingClient.SendStockReserved(reserveRequest, reservation.Id, context.CancellationToken);

        var response = new GrpcContracts.ReserveStockResponse
        {
            ReservationId = reservation.Id.ToString(),
            OutOfStockProducts = { outOfStock },
            Success = outOfStock.Count == 0,
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(reservation.CreatedAt.ToUniversalTime()),
            ValidUntil = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(reservation.ValidUntil.ToUniversalTime())
        };

        return response;
    }

    public override async Task<GrpcContracts.CommitReservationResponse> CommitReservation(
    GrpcContracts.CommitReservationRequest request,
    ServerCallContext context)
    {
        var reservationId = Guid.Parse(request.ReservationId);
        var reservation = await _reservationRepository.GetByIdAsync(reservationId, context.CancellationToken);

        if (reservation == null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Reservation {reservationId} not found."));

        if (reservation.IsCommitted)
            throw new RpcException(new Status(StatusCode.AlreadyExists, $"Reservation {reservationId} is already committed."));

        if (reservation.ValidUntil < DateTime.UtcNow)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, $"Reservation {reservationId} has expired."));

        reservation.IsCommitted = true;
        reservation.CommittedAt = DateTime.UtcNow;
        await _reservationRepository.UpdateAsync(reservation, context.CancellationToken);

        var reserveRequest = new DomainContracts.ReserveStockRequest
        {
            OrderId = Guid.Parse(request.OrderId),
            Stock = reservation.Items
                .SelectMany(i => i.Items.Select(si => new Stock
                {
                    ProductID = si.ProductID,
                    Quantity = si.Quantity,
                    Warehouse = i.Warehouse
                }))
                .ToList()
        };

        await _messagingClient.SendStockReservationCommitted(reserveRequest, reservationId, context.CancellationToken);

        var response = new GrpcContracts.CommitReservationResponse
        {
            OrderId = request.OrderId,
            ReservationId = request.ReservationId,
            Success = true
        };

        return response;
    }

    public override async Task<GrpcContracts.CancelReservationResponse> CancelReservation(
    GrpcContracts.CancelReservationRequest request,
    ServerCallContext context)
    {
        var reservationId = Guid.Parse(request.ReservationId);

        var reservation = await _reservationRepository.GetByIdAsync(reservationId, context.CancellationToken);

        if (reservation == null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Reservation {reservationId} not found."));

        if (reservation.IsCommitted)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, $"Reservation {reservationId} is already committed."));

        if (reservation.ValidUntil < DateTime.UtcNow)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, $"Reservation {reservationId} has already expired."));

        foreach (var resItem in reservation.Items)
        {
            foreach (var item in resItem.Items)
            {
                var stock = await _stockRepository.GetByProductIdAsync(item.ProductID, context.CancellationToken);
                if (stock != null)
                {
                    stock.Quantity += item.Quantity;
                    await _stockRepository.AddOrUpdateAsync(stock, context.CancellationToken);
                }
            }
        }

        await _messagingClient.SendStockReservationCancelled(Guid.Parse(request.OrderId), reservationId, request.Reason, context.CancellationToken);

        reservation.IsCommitted = false;
        await _reservationRepository.UpdateAsync(reservation, context.CancellationToken);

        var response = new GrpcContracts.CancelReservationResponse
        {
            OrderId = request.OrderId,
            ReservationId = request.ReservationId,
            Success = true,
            Reason = request.Reason
        };

        return response;
    }
}