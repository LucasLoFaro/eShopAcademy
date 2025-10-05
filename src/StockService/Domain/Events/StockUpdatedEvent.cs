using Core.Domain.Contracts;

namespace Core.Domain.Events
{
    public class StockUpdatedEvent
    {
        public Guid EventId = new Guid();
        public AlterStockRequest Stock { get; set; }
        public DateTime TriggeredAt = DateTime.Now;
    }
}
