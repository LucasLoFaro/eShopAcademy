using MassTransit;

namespace Application.IntegrationEvents.Messages
{

    //Mensaje: solo un PoCo, Evneto, mas detalles como Id, timestamp
    //ithub
    public class StockChangedEvent : IConsumer<StockChangedEvent>
    {
        public Guid Guid { get; set; }
        public DateTime Date { get; set; }
        public string ProductGuid { get; set; }
        public int Quantity { get; set; }
        public string Warehouse { get; set; }


        public StockChangedEvent(string productGuid, int quantity, string warehouse)
        {
            Guid = Guid.NewGuid();
            Date = DateTime.Now;
            ProductGuid = productGuid;
            Quantity = quantity;
            Warehouse = warehouse;
        }
        

        public async Task Consume(ConsumeContext<StockChangedEvent> context)
        {
            await Console.Out.WriteLineAsync($"Nuevo evento: Stock actualizado del producto {context.Message.ProductGuid}.");
            await Console.Out.WriteLineAsync($"Nuevo Stock: {context.Message.Quantity}");
        }
    }
   
}
