using Core.Domain.Contracts;
using Domain.DTOs;

namespace Core.Domain.Events
{
    public class StockUpdatedEvent
    {
        public Guid EventId = new Guid();
        public AlterStockDTO Stock { get; set; }
        public DateTime TriggeredAt = DateTime.Now;
    }
}
