using MassTransit;

namespace Application.IntegrationEvents.Messages
{
    //public record StockChanged(string ProductGuid, int Quantity, string Warehouse);
    //Mensaje: solo un PoCo, Evneto, mas detalles como Id, timestamp
    //ithub
    public class StockChanged : IConsumer<StockChanged>
    {
        public StockChanged(string productGuid, int quantity, string warehouse)
        {
            ProductGuid = productGuid;
            Quantity = quantity;
            Warehouse = warehouse;
        }

        public string ProductGuid { get; set; }
        public int Quantity { get; set; }
        public string Warehouse { get; set; }

        public async Task Consume(ConsumeContext<StockChanged> context)
        {
            await Console.Out.WriteLineAsync($"Nuevo evento: Stock actualizado del producto {context.Message.ProductGuid}.");
            await Console.Out.WriteLineAsync($"Nuevo Stock: {context.Message.Quantity}");
        }
    }
   
}
