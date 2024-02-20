using Application.IntegrationEvents.Messages;
using MassTransit;

namespace API.IntegrationEvents.Consumers
{
    public class StockChangedConsumer : IConsumer<StockChanged>
    {
        private readonly ILogger<StockChangedConsumer> _log;
        //private readonly BasketDbContext _context;

        public StockChangedConsumer(ILogger<StockChangedConsumer> log/*, BasketDbContext context*/)
        {
            _log = log;
            //_context = context;
        }

        public async Task Consume(ConsumeContext<StockChanged> context)
        {
            _log.LogInformation("Nuevo evento: Precio actualizado del producto {0}.", context.Message.ProductGuid);
            _log.LogWarning("Nuevo Stock: {0}", context.Message.Quantity);

            await Console.Out.WriteLineAsync($"Nuevo evento: Precio actualizado del producto {context.Message.ProductGuid}.");
            await Console.Out.WriteLineAsync($"Nuevo Stock: {context.Message.Quantity}");


        }
    }
}